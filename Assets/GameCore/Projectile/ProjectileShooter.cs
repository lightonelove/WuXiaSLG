using UnityEngine;
using System.Collections;
using Sirenix.OdinInspector;

namespace Wuxia.GameCore
{
    /// <summary>
    /// 投射物發射器 - 被啟用時自動朝 Forward 方向發射投射物
    /// </summary>
    public class ProjectileShooter : MonoBehaviour
    {
        [Header("投射物設定")]
        [Tooltip("投射物預製物")]
        [SerializeField] private GameObject projectilePrefab;
        
        [Tooltip("發射位置偏移")]
        [SerializeField] private Vector3 shootOffset = Vector3.zero;
        
        [Tooltip("是否使用自己的 Transform 作為發射點")]
        [SerializeField] private bool useThisTransform = true;
        
        [Tooltip("自訂發射點（如果不使用自己的 Transform）")]
        [SerializeField] private Transform customShootPoint;
        
        [Header("發射參數")]
        [Tooltip("發射延遲（秒）")]
        [SerializeField] private float shootDelay = 0f;
        
        [Tooltip("是否覆蓋投射物的預設參數")]
        [SerializeField] private bool overrideProjectileParameters = false;
        
        [Tooltip("投射物初始速度")]
        [ShowIf("overrideProjectileParameters")]
        [SerializeField] private float projectileSpeed = 10f;
        
        [Tooltip("投射物射程")]
        [ShowIf("overrideProjectileParameters")]
        [SerializeField] private float projectileRange = 20f;
        
        [Tooltip("投射物生命週期")]
        [ShowIf("overrideProjectileParameters")]
        [SerializeField] private float projectileLifetime = 5f;
        
        [Header("發射方向")]
        [Tooltip("是否使用自訂方向")]
        [SerializeField] private bool useCustomDirection = false;
        
        [Tooltip("自訂發射方向（如果不使用 Transform.forward）")]
        [SerializeField] private Vector3 customDirection = Vector3.forward;
        
        [Header("多重發射")]
        [Tooltip("是否多重發射")]
        [SerializeField] private bool multipleShots = false;
        
        [Tooltip("發射數量")]
        [SerializeField] private int shotCount = 1;
        
        [Tooltip("發射間隔（秒）")]
        [SerializeField] private float shotInterval = 0.1f;
        
        [Tooltip("散射角度")]
        [SerializeField] private float spreadAngle = 0f;
        
        [Header("目標設定")]
        [Tooltip("是否有特定目標")]
        [SerializeField] private bool hasTarget = false;
        
        [Tooltip("目標 Transform")]
        [SerializeField] private Transform targetTransform;
        
        [Tooltip("目標位置")]
        [SerializeField] private Vector3 targetPosition;
        
        [Header("音效")]
        [Tooltip("發射音效")]
        [SerializeField] private AudioClip shootSound;
        
        [Tooltip("音效播放器")]
        [SerializeField] private AudioSource audioSource;
        
        [Header("調試")]
        [Tooltip("顯示調試資訊")]
        [SerializeField] private bool showDebug = false;
        
        // 內部變數
        private bool hasShot = false;
        
        // 事件
        public System.Action<ProjectileShooter, GameObject> OnProjectileShot;
        public System.Action<ProjectileShooter> OnShootComplete;
        
        void Awake()
        {
            // 如果沒有音效播放器，嘗試獲取
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }
        }
        
        void OnEnable()
        {
            // 重置發射狀態
            hasShot = false;
            
            // 開始發射協程
            StartCoroutine(ShootAfterDelay());
        }
        
        void OnDisable()
        {
            // 停止所有協程
            StopAllCoroutines();
            hasShot = false;
        }
        
        /// <summary>
        /// 延遲發射協程
        /// </summary>
        private IEnumerator ShootAfterDelay()
        {
            if (showDebug)
                Debug.Log($"[ProjectileShooter] {gameObject.name} 準備發射，延遲: {shootDelay}秒");
            
            // 等待延遲時間
            if (shootDelay > 0f)
            {
                yield return new WaitForSeconds(shootDelay);
            }
            
            // 檢查是否還處於啟用狀態
            if (!gameObject.activeInHierarchy || !enabled)
                yield break;
            
            // 執行發射
            if (multipleShots && shotCount > 1)
            {
                yield return StartCoroutine(ShootMultiple());
            }
            else
            {
                ShootSingle();
            }
            
            // 標記已發射
            hasShot = true;
            OnShootComplete?.Invoke(this);
            
            if (showDebug)
                Debug.Log($"[ProjectileShooter] {gameObject.name} 發射完成");
        }
        
        /// <summary>
        /// 單次發射
        /// </summary>
        private void ShootSingle()
        {
            Vector3 shootDirection = GetShootDirection();
            Vector3 shootPosition = GetShootPosition();
            
            GameObject projectile = CreateProjectile(shootPosition, shootDirection);
            
            if (showDebug)
                Debug.Log($"[ProjectileShooter] {gameObject.name} 發射投射物，方向: {shootDirection}");
            
            PlayShootSound();
            OnProjectileShot?.Invoke(this, projectile);
        }
        
        /// <summary>
        /// 多重發射協程
        /// </summary>
        private IEnumerator ShootMultiple()
        {
            for (int i = 0; i < shotCount; i++)
            {
                Vector3 baseDirection = GetShootDirection();
                Vector3 shootDirection = ApplySpread(baseDirection, i);
                Vector3 shootPosition = GetShootPosition();
                
                GameObject projectile = CreateProjectile(shootPosition, shootDirection);
                
                if (showDebug)
                    Debug.Log($"[ProjectileShooter] {gameObject.name} 發射投射物 {i + 1}/{shotCount}");
                
                PlayShootSound();
                OnProjectileShot?.Invoke(this, projectile);
                
                // 如果不是最後一發，等待間隔
                if (i < shotCount - 1 && shotInterval > 0f)
                {
                    yield return new WaitForSeconds(shotInterval);
                }
            }
        }
        
        /// <summary>
        /// 創建投射物
        /// </summary>
        private GameObject CreateProjectile(Vector3 position, Vector3 direction)
        {
            if (projectilePrefab == null)
            {
                Debug.LogError($"[ProjectileShooter] {gameObject.name} 沒有設定投射物預製物！");
                return null;
            }
            
            // 創建投射物
            Quaternion rotation = Quaternion.LookRotation(direction);
            GameObject projectile = Instantiate(projectilePrefab, position, rotation);
            
            // 設定投射物參數
            Projectile projectileScript = projectile.GetComponent<Projectile>();
            if (projectileScript != null)
            {
                // 只有在覆蓋模式下才設定參數
                if (overrideProjectileParameters)
                {
                    projectileScript.SetParameters(projectileSpeed, projectileLifetime, projectileRange);
                }
                
                // 設定目標或方向
                if (hasTarget)
                {
                    if (targetTransform != null)
                    {
                        projectileScript.SetTarget(targetTransform);
                    }
                    else
                    {
                        projectileScript.SetTarget(targetPosition);
                    }
                }
                else
                {
                    projectileScript.SetDirection(direction);
                }
            }
            else
            {
                Debug.LogWarning($"[ProjectileShooter] 投射物預製物上沒有 Projectile 腳本");
            }
            
            return projectile;
        }
        
        /// <summary>
        /// 獲取發射方向
        /// </summary>
        private Vector3 GetShootDirection()
        {
            if (hasTarget)
            {
                Vector3 shootPosition = GetShootPosition();
                Vector3 targetPos = targetTransform != null ? targetTransform.position : targetPosition;
                return (targetPos - shootPosition).normalized;
            }
            else if (useCustomDirection)
            {
                return customDirection.normalized;
            }
            else
            {
                return transform.forward;
            }
        }
        
        /// <summary>
        /// 獲取發射位置
        /// </summary>
        private Vector3 GetShootPosition()
        {
            Transform shootTransform = useThisTransform ? transform : customShootPoint;
            if (shootTransform == null)
                shootTransform = transform;
            
            return shootTransform.position + shootTransform.TransformDirection(shootOffset);
        }
        
        /// <summary>
        /// 應用散射角度
        /// </summary>
        private Vector3 ApplySpread(Vector3 baseDirection, int shotIndex)
        {
            if (spreadAngle <= 0f || shotCount <= 1)
                return baseDirection;
            
            // 計算每發之間的角度
            float angleStep = spreadAngle / (shotCount - 1);
            float currentAngle = -spreadAngle * 0.5f + angleStep * shotIndex;
            
            // 繞 Y 軸旋轉
            Quaternion spreadRotation = Quaternion.AngleAxis(currentAngle, Vector3.up);
            return spreadRotation * baseDirection;
        }
        
        /// <summary>
        /// 播放發射音效
        /// </summary>
        private void PlayShootSound()
        {
            if (audioSource != null && shootSound != null)
            {
                audioSource.PlayOneShot(shootSound);
            }
        }
        
        /// <summary>
        /// 手動觸發發射
        /// </summary>
        [ContextMenu("手動發射")]
        public void ManualShoot()
        {
            if (!hasShot)
            {
                StartCoroutine(ShootAfterDelay());
            }
        }
        
        /// <summary>
        /// 設定目標
        /// </summary>
        public void SetTarget(Transform target)
        {
            targetTransform = target;
            hasTarget = target != null;
        }
        
        /// <summary>
        /// 設定目標位置
        /// </summary>
        public void SetTarget(Vector3 position)
        {
            targetPosition = position;
            targetTransform = null;
            hasTarget = true;
        }
        
        /// <summary>
        /// 重置發射狀態
        /// </summary>
        public void ResetShooter()
        {
            hasShot = false;
            StopAllCoroutines();
        }
        
        /// <summary>
        /// 檢查是否已發射
        /// </summary>
        public bool HasShot()
        {
            return hasShot;
        }
        
        // 在編輯器中顯示發射方向
        void OnDrawGizmosSelected()
        {
            if (!showDebug) return;
            
            Vector3 shootPos = GetShootPosition();
            Vector3 shootDir = GetShootDirection();
            
            // 顯示發射點
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(shootPos, 0.1f);
            
            // 顯示發射方向
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(shootPos, shootDir * 3f);
            
            // 顯示目標
            if (hasTarget)
            {
                Vector3 targetPos = targetTransform != null ? targetTransform.position : targetPosition;
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(targetPos, 0.2f);
                Gizmos.DrawLine(shootPos, targetPos);
            }
        }
    }
}