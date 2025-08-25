using UnityEngine;
using System.Collections;

namespace Wuxia.GameCore
{
    /// <summary>
    /// 投射物腳本 - 管理投射物的飛行行為
    /// </summary>
    public class Projectile : MonoBehaviour
    {
        [Header("飛行設定")]
        [Tooltip("飛行速度")]
        [SerializeField] private float speed = 10f;
        
        [Tooltip("飛行軌跡類型")]
        [SerializeField] private ProjectileTrajectory trajectoryType = ProjectileTrajectory.Straight;
        
        [Tooltip("拋物線高度（僅當軌跡為拋物線時使用）")]
        [SerializeField] private float arcHeight = 2f;
        
        [Tooltip("最大飛行距離")]
        [SerializeField] private float maxRange = 20f;
        
        [Tooltip("生命週期（秒）")]
        [SerializeField] private float lifeTime = 5f;
        
        [Header("目標設定")]
        [Tooltip("是否追蹤目標")]
        [SerializeField] private bool isHoming = false;
        
        [Tooltip("追蹤轉向速度")]
        [SerializeField] private float homingSpeed = 5f;
        
        [Header("碰撞設定")]
        [Tooltip("碰撞時是否銷毀")]
        [SerializeField] private bool destroyOnImpact = true;
        
        [Tooltip("可碰撞的圖層")]
        [SerializeField] private LayerMask collisionLayers = -1;
        
        [Header("視覺效果")]
        [Tooltip("旋轉速度")]
        [SerializeField] private float rotationSpeed = 0f;
        
        [Tooltip("縮放動畫")]
        [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.Linear(0, 1, 1, 1);
        
        [Header("反彈設定")]
        [Tooltip("最大反彈次數（0 = 無限制）")]
        [SerializeField] private int maxReflections = 3;
        
        // 內部變數
        private Vector3 startPosition;
        private Vector3 targetPosition;
        private Transform targetTransform;
        private float travelTime;
        private float currentTime;
        private Vector3 initialScale;
        private bool hasTarget;
        private CombatEntity shooter; // 發射者
        private Vector3 originalStartPosition; // 原始發射位置（用於反彈計算）
        private Vector3 originalTargetPosition; // 原始目標位置（用於反彈計算）
        private Vector3 originalDirection; // 原始發射方向向量（用於精確反彈）
        private bool hasOriginalDirection = false; // 是否已記錄原始方向
        private int reflectionCount = 0; // 反彈次數
        
        // 事件
        public System.Action<Projectile, Collider> OnHit;
        public System.Action<Projectile> OnDestroy;
        
        /// <summary>
        /// 投射物軌跡類型
        /// </summary>
        public enum ProjectileTrajectory
        {
            Straight,    // 直線
            Arc,         // 拋物線
            Homing       // 追蹤
        }
        
        void Awake()
        {
            initialScale = transform.localScale;
        }
        
        void Start()
        {
            // 設定生命週期
            Destroy(gameObject, lifeTime);
            
            // 記錄起始位置
            startPosition = transform.position;
            
            // 如果沒有設定目標，使用預設方向
            if (!hasTarget)
            {
                targetPosition = startPosition + transform.forward * maxRange;
                // 保存原始位置和方向
                originalStartPosition = startPosition;
                originalTargetPosition = targetPosition;
                originalDirection = (targetPosition - startPosition).normalized;
                hasOriginalDirection = true;
                Debug.Log($"[Projectile] Start 記錄原始方向: {originalDirection}");
            }
            else
            {
            }
            
            // 計算飛行時間
            CalculateTravelTime();
        }
        
        void Update()
        {
            UpdateMovement();
            UpdateVisuals();
        }
        
        /// <summary>
        /// 設定投射物目標
        /// </summary>
        /// <param name="target">目標位置</param>
        public void SetTarget(Vector3 target)
        {
            // 重新設定起始位置為當前位置
            startPosition = transform.position;
            
            targetPosition = target;
            hasTarget = true;
            
            // 保存原始位置和方向（第一次設定時）
            if (!hasOriginalDirection)
            {
                originalStartPosition = startPosition;
                originalTargetPosition = targetPosition;
                originalDirection = (targetPosition - startPosition).normalized;
                hasOriginalDirection = true;
                Debug.Log($"[Projectile] 記錄原始方向: {originalDirection}");
            }
            
            // 如果是追蹤類型，計算初始方向
            if (trajectoryType == ProjectileTrajectory.Homing || isHoming)
            {
                Vector3 direction = (targetPosition - startPosition).normalized;
                transform.rotation = Quaternion.LookRotation(direction);
            }
        }
        
        /// <summary>
        /// 設定投射物追蹤目標
        /// </summary>
        /// <param name="target">追蹤目標Transform</param>
        public void SetTarget(Transform target)
        {
            targetTransform = target;
            if (target != null)
            {
                SetTarget(target.position);
            }
        }
        
        /// <summary>
        /// 設定投射物發射方向
        /// </summary>
        /// <param name="direction">發射方向</param>
        public void SetDirection(Vector3 direction)
        {
            // 重新設定起始位置為當前位置
            startPosition = transform.position;
            
            transform.rotation = Quaternion.LookRotation(direction.normalized);
            targetPosition = startPosition + direction.normalized * maxRange;
            hasTarget = true;
            
            // 保存原始位置和方向（第一次設定時）
            if (!hasOriginalDirection)
            {
                originalStartPosition = startPosition;
                originalTargetPosition = targetPosition;
                originalDirection = direction.normalized;
                hasOriginalDirection = true;
                Debug.Log($"[Projectile] SetDirection 記錄原始方向: {originalDirection}");
            }
        }
        
        /// <summary>
        /// 計算飛行時間
        /// </summary>
        private void CalculateTravelTime()
        {
            float distance = Vector3.Distance(startPosition, targetPosition);
            travelTime = distance / speed;
        }
        
        /// <summary>
        /// 更新移動
        /// </summary>
        private void UpdateMovement()
        {
            currentTime += Time.deltaTime;
            float progress = currentTime / travelTime;
            
            // 如果是追蹤目標且目標還存在，更新目標位置
            if ((isHoming || trajectoryType == ProjectileTrajectory.Homing) && targetTransform != null)
            {
                targetPosition = targetTransform.position;
            }
            
            Vector3 newPosition;
            
            switch (trajectoryType)
            {
                case ProjectileTrajectory.Straight:
                    newPosition = CalculateStraightMovement(progress);
                    break;
                    
                case ProjectileTrajectory.Arc:
                    newPosition = CalculateArcMovement(progress);
                    break;
                    
                case ProjectileTrajectory.Homing:
                    newPosition = CalculateHomingMovement();
                    break;
                    
                default:
                    newPosition = CalculateStraightMovement(progress);
                    break;
            }
            
            // 更新位置和朝向
            Vector3 direction = newPosition - transform.position;
            if (direction != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }
            
            transform.position = newPosition;
            
            // 檢查是否到達目標
            if (progress >= 1f && trajectoryType != ProjectileTrajectory.Homing)
            {
                OnReachTarget();
            }
        }
        
        /// <summary>
        /// 計算直線移動
        /// </summary>
        private Vector3 CalculateStraightMovement(float progress)
        {
            return Vector3.Lerp(startPosition, targetPosition, progress);
        }
        
        /// <summary>
        /// 計算拋物線移動
        /// </summary>
        private Vector3 CalculateArcMovement(float progress)
        {
            Vector3 linearPosition = Vector3.Lerp(startPosition, targetPosition, progress);
            
            // 計算拋物線高度
            float arcProgress = Mathf.Sin(progress * Mathf.PI);
            Vector3 arcOffset = Vector3.up * arcHeight * arcProgress;
            
            return linearPosition + arcOffset;
        }
        
        /// <summary>
        /// 計算追蹤移動
        /// </summary>
        private Vector3 CalculateHomingMovement()
        {
            Vector3 directionToTarget = (targetPosition - transform.position).normalized;
            Vector3 currentDirection = transform.forward;
            
            // 平滑轉向目標
            Vector3 newDirection = Vector3.Slerp(currentDirection, directionToTarget, homingSpeed * Time.deltaTime);
            
            // 移動
            return transform.position + newDirection * speed * Time.deltaTime;
        }
        
        /// <summary>
        /// 更新視覺效果
        /// </summary>
        private void UpdateVisuals()
        {
            // 旋轉效果
            if (rotationSpeed != 0f)
            {
                transform.Rotate(Vector3.forward, rotationSpeed * Time.deltaTime);
            }
            
            // 縮放動畫
            float scaleProgress = currentTime / lifeTime;
            float scaleMultiplier = scaleCurve.Evaluate(scaleProgress);
            transform.localScale = initialScale * scaleMultiplier;
        }
        
        /// <summary>
        /// 到達目標時的處理
        /// </summary>
        private void OnReachTarget()
        {
            OnDestroy?.Invoke(this);
            
            if (destroyOnImpact)
            {
                Destroy(gameObject);
            }
        }
        
        /// <summary>
        /// 碰撞檢測
        /// </summary>
        private void OnTriggerEnter(Collider other)
        {
            // 檢查碰撞圖層
            if (((1 << other.gameObject.layer) & collisionLayers) != 0)
            {
                OnHit?.Invoke(this, other);
                
                if (destroyOnImpact)
                {
                    Destroy(gameObject);
                }
            }
        }
        
        /// <summary>
        /// 設定投射物參數
        /// </summary>
        public void SetParameters(float newSpeed, float newLifeTime, float newRange)
        {
            speed = newSpeed;
            lifeTime = newLifeTime;
            maxRange = newRange;
        }
        
        /// <summary>
        /// 設定發射者
        /// </summary>
        public void SetShooter(CombatEntity shooterEntity)
        {
            shooter = shooterEntity;
        }
        
        /// <summary>
        /// 取得發射者
        /// </summary>
        public CombatEntity GetShooter()
        {
            return shooter;
        }
        
        /// <summary>
        /// 獲取當前飛行進度
        /// </summary>
        public float GetProgress()
        {
            return currentTime / travelTime;
        }
        
        /// <summary>
        /// 強制銷毀投射物
        /// </summary>
        public void DestroyProjectile()
        {
            OnDestroy?.Invoke(this);
            Destroy(gameObject);
        }
        
        /// <summary>
        /// 因達到最大反彈次數而銷毀投射物
        /// </summary>
        private void DestroyProjectileOnMaxReflections()
        {
            Debug.Log($"[Projectile] 投射物達到最大反彈次數限制 ({maxReflections})，自動銷毀");
            OnDestroy?.Invoke(this);
            Destroy(gameObject);
        }
        
        /// <summary>
        /// 投射物反彈 - 朝反方向飛行並更換陣營
        /// </summary>
        /// <param name="newOwner">新的擁有者 CombatEntity</param>
        /// <param name="reflectionDirection">反彈方向（可選，如果為 Vector3.zero 則自動計算反彈方向）</param>
        public void ReflectProjectile(CombatEntity newOwner, Vector3 reflectionDirection = default)
        {
            if (newOwner == null)
            {
                Debug.LogWarning($"[Projectile] ReflectProjectile 的 newOwner 為 null");
                return;
            }
            
            // 檢查反彈次數限制
            if (maxReflections > 0 && reflectionCount >= maxReflections)
            {
                Debug.Log($"[Projectile] 投射物已達到最大反彈次數 ({maxReflections})，直接銷毀");
                DestroyProjectileOnMaxReflections();
                return;
            }
            
            // 增加反彈次數
            reflectionCount++;
            
            // 更換發射者
            shooter = newOwner;
            
            // 更新投射物上所有 DamageDealer 的陣營
            UpdateDamageDealerOwnership(newOwner);
            
            // 計算反彈方向
            Vector3 newDirection;
            if (reflectionDirection != Vector3.zero)
            {
                // 如果外部指定了方向，使用指定的方向
                newDirection = reflectionDirection.normalized;
                Debug.Log($"[Projectile] 第 {reflectionCount} 次反彈，使用指定方向: {newDirection}");
            }
            else
            {
                // 自動計算反彈方向，根據原始方向和反彈次數
                newDirection = CalculateAutoReflectionDirection();
            }
            
            // 重置投射物飛行參數
            startPosition = transform.position;
            targetPosition = startPosition + newDirection * maxRange;
            currentTime = 0f;
            hasTarget = true;
            
            // 更新朝向和重新計算飛行時間
            transform.rotation = Quaternion.LookRotation(newDirection);
            CalculateTravelTime();
            
            Debug.Log($"[Projectile] 投射物反彈！第 {reflectionCount}/{maxReflections} 次反彈，新擁有者: {newOwner.Name}，反彈方向: {newDirection}");
        }
        
        /// <summary>
        /// 更新投射物上所有 DamageDealer 的擁有者
        /// </summary>
        /// <param name="newOwner">新擁有者</param>
        private void UpdateDamageDealerOwnership(CombatEntity newOwner)
        {
            DamageDealer[] damageDealers = GetComponentsInChildren<DamageDealer>();
            
            foreach (DamageDealer dealer in damageDealers)
            {
                if (dealer != null)
                {
                    dealer.sourceCombatEntity = newOwner;
                    Debug.Log($"[Projectile] 更新 DamageDealer {dealer.gameObject.name} 的擁有者為: {newOwner.Name}");
                }
            }
            
            Debug.Log($"[Projectile] 共更新了 {damageDealers.Length} 個 DamageDealer 的擁有者");
        }
        
        /// <summary>
        /// 自動計算反彈方向（內部邏輯）
        /// </summary>
        /// <returns>計算後的反彈方向</returns>
        private Vector3 CalculateAutoReflectionDirection()
        {
            Vector3 reflectionDirection;
            
            // 如果有原始方向資訊
            if (hasOriginalDirection && originalDirection != Vector3.zero)
            {
                // 根據反彈次數決定方向
                // 第1次反彈：負向（返回）
                // 第2次反彈：正向（再次向前）
                // 第3次反彈：負向（再次返回）
                // 以此類推...
                if (reflectionCount % 2 == 1)
                {
                    // 奇數次反彈，使用負向（返回）
                    reflectionDirection = -originalDirection;
                    Debug.Log($"[Projectile] 第 {reflectionCount} 次反彈（奇數），使用負向: {reflectionDirection}");
                }
                else
                {
                    // 偶數次反彈，使用正向（向前）
                    reflectionDirection = originalDirection;
                    Debug.Log($"[Projectile] 第 {reflectionCount} 次反彈（偶數），使用正向: {reflectionDirection}");
                }
            }
            else
            {
                // 備用方案：使用當前方向的反向
                reflectionDirection = -transform.forward;
                Debug.Log($"[Projectile] 無原始方向資訊，使用當前方向反向: {reflectionDirection}");
            }
            
            return reflectionDirection.normalized;
        }
        
        /// <summary>
        /// 檢查投射物是否可以被反彈
        /// </summary>
        /// <returns>是否可以反彈</returns>
        public bool CanBeReflected()
        {
            // 可以添加額外的條件，例如投射物類型、速度等
            return gameObject != null && enabled;
        }
        
        /// <summary>
        /// 取得原始發射位置
        /// </summary>
        /// <returns>原始發射位置</returns>
        public Vector3 GetOriginalStartPosition()
        {
            return originalStartPosition;
        }
        
        /// <summary>
        /// 取得原始目標位置
        /// </summary>
        /// <returns>原始目標位置</returns>
        public Vector3 GetOriginalTargetPosition()
        {
            return originalTargetPosition;
        }
        
        /// <summary>
        /// 取得原始發射方向向量
        /// </summary>
        /// <returns>原始發射方向</returns>
        public Vector3 GetOriginalDirection()
        {
            return originalDirection;
        }
        
        /// <summary>
        /// 取得反彈次數
        /// </summary>
        /// <returns>反彈次數</returns>
        public int GetReflectionCount()
        {
            return reflectionCount;
        }
        
        /// <summary>
        /// 取得最大反彈次數
        /// </summary>
        /// <returns>最大反彈次數</returns>
        public int GetMaxReflections()
        {
            return maxReflections;
        }
        
        /// <summary>
        /// 設定最大反彈次數
        /// </summary>
        /// <param name="max">最大反彈次數（0 = 無限制）</param>
        public void SetMaxReflections(int max)
        {
            maxReflections = Mathf.Max(0, max);
        }
    }
}