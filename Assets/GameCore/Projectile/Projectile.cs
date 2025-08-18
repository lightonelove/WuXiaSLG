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
        
        // 內部變數
        private Vector3 startPosition;
        private Vector3 targetPosition;
        private Transform targetTransform;
        private float travelTime;
        private float currentTime;
        private Vector3 initialScale;
        private bool hasTarget;
        
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
            targetPosition = target;
            hasTarget = true;
            
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
            transform.rotation = Quaternion.LookRotation(direction.normalized);
            targetPosition = startPosition + direction.normalized * maxRange;
            hasTarget = true;
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
    }
}