using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Wuxia.GameCore
{
    [System.Serializable]
    public class EnemyRangeAttackAction : BaseEnemyAction
    {
        public enum RangeAttackTargetType
        {
            ClosestPlayer,      // 攻擊最近的玩家
            LowestHealth,       // 攻擊血量最低的玩家
            HighestThreat,      // 攻擊威脅值最高的玩家
            SpecificTarget      // 攻擊特定目標
        }
        
        [Header("遠距攻擊基礎設定")]
        [SerializeField] private RangeAttackTargetType targetType = RangeAttackTargetType.ClosestPlayer;
        [SerializeField] private float minAttackRange = 3f; // 最小攻擊距離
        [SerializeField] private float maxAttackRange = 10f; // 最大攻擊距離
        [SerializeField] private float attackDamage = 15f;
        [SerializeField] private float apCost = 25f;
        
        [Header("視線檢查設定")]
        [SerializeField] private LayerMask obstacleLayerMask = -1; // 阻礙物圖層
        [SerializeField] private float raycastOffset = 1f; // Raycast 起始點的高度偏移
        [SerializeField] private bool showDebugRaycast = true; // 是否顯示除錯射線
        
        [Header("發射器設定")]
        [SerializeField] private ProjectileShooter projectileShooter; // 投射物發射器
        [SerializeField] private bool autoFindProjectileShooter = true; // 自動尋找發射器
        
        [Header("攻擊動畫設定")]
        [SerializeField] private float attackDuration = 1f;
        [SerializeField] private string attackAnimationName = "RangeAttack"; // 動畫名稱
        
        // 快取的目標和元件
        private CombatEntity cachedTarget;
        private Animator cachedAnimator;
        private bool isInitialized;
        private List<CombatEntity> availableTargets = new List<CombatEntity>();
        
        public override void InitializeAction(EnemyCore enemy)
        {
            base.InitializeAction(enemy);
            
            // 重置初始化狀態和快取
            isInitialized = false;
            cachedTarget = null;
            cachedAnimator = null;
            availableTargets = new List<CombatEntity>();
            
            // 檢查 enemyCombatEntity 是否已經被設定
            if (enemyCombatEntity == null)
            {
                Debug.LogError($"[AI] {enemy.gameObject.name} 的 enemyCombatEntity 尚未設定！請確保 EnemyAISystem 已正確初始化。");
                return;
            }
            
            // 快取 Animator
            cachedAnimator = enemy.animator;
            if (cachedAnimator == null)
            {
                Debug.LogWarning($"[AI] {enemy.gameObject.name} 的 EnemyRangeAttackAction 需要 Animator 但找不到該元件");
            }
            else if (string.IsNullOrEmpty(attackAnimationName))
            {
                Debug.LogWarning($"[AI] {enemy.gameObject.name} 的 EnemyRangeAttackAction 未設定攻擊動畫名稱");
            }
            
            // 自動尋找 ProjectileShooter
            if (autoFindProjectileShooter && projectileShooter == null)
            {
                projectileShooter = enemy.GetComponentInChildren<ProjectileShooter>();
                if (projectileShooter == null)
                {
                    Debug.LogWarning($"[AI] {enemy.gameObject.name} 的 EnemyRangeAttackAction 找不到 ProjectileShooter 元件");
                }
                else
                {
                    Debug.Log($"[AI] {enemy.gameObject.name} 自動找到 ProjectileShooter: {projectileShooter.gameObject.name}");
                }
            }
            
            // 尋找並快取可攻擊的目標
            RefreshTargetList(enemy);
            
            // 根據目標類型選擇目標
            cachedTarget = SelectTarget(enemy);
            
            if (cachedTarget != null)
            {
                isInitialized = true;
                Debug.Log($"[AI] {enemy.gameObject.name} 遠距攻擊初始化完成，目標: {cachedTarget.name}, 攻擊類型: {targetType}");
            }
            else
            {
                Debug.LogWarning($"[AI] {enemy.gameObject.name} 遠距攻擊初始化失敗：找不到合適的目標");
            }
        }
        
        /// <summary>
        /// 重新整理可攻擊目標列表
        /// </summary>
        private void RefreshTargetList(EnemyCore enemy)
        {
            availableTargets.Clear();
            
            if (CombatCore.Instance != null && enemyCombatEntity != null)
            {
                foreach (var entity in CombatCore.Instance.AllCombatEntity)
                {
                    if (entity != null && entity.gameObject.activeInHierarchy)
                    {
                        // 檢查陣營 - 只攻擊不同陣營的目標
                        if (IsHostileFaction(entity))
                        {
                            // 檢查目標是否在攻擊範圍內（最小距離到最大距離之間）
                            float distance = Vector3.Distance(enemy.transform.position, entity.transform.position);
                            
                            if (distance >= minAttackRange && distance <= maxAttackRange)
                            {
                                // 進行視線檢查
                                if (HasLineOfSight(enemy, entity))
                                {
                                    availableTargets.Add(entity);
                                    Debug.Log($"[AI] {enemy.gameObject.name} 找到可攻擊目標: {entity.Name}, 距離: {distance:F2} (範圍: {minAttackRange}-{maxAttackRange})");
                                }
                                else
                                {
                                    Debug.Log($"[AI] {enemy.gameObject.name} 對目標 {entity.Name} 沒有視線，距離: {distance:F2}");
                                }
                            }
                            else if (distance < minAttackRange)
                            {
                                Debug.Log($"[AI] {enemy.gameObject.name} 目標 {entity.Name} 太接近 (距離: {distance:F2}, 最小範圍: {minAttackRange})");
                            }
                            else
                            {
                                Debug.Log($"[AI] {enemy.gameObject.name} 目標 {entity.Name} 太遠 (距離: {distance:F2}, 最大範圍: {maxAttackRange})");
                            }
                        }
                    }
                }
            }
            
            Debug.Log($"[AI] {enemy.gameObject.name} 找到 {availableTargets.Count} 個可遠距攻擊目標");
        }
        
        /// <summary>
        /// 檢查是否有視線到目標（使用 Raycast）
        /// </summary>
        private bool HasLineOfSight(EnemyCore enemy, CombatEntity target)
        {
            Vector3 startPosition = enemy.transform.position + Vector3.up * raycastOffset;
            Vector3 targetPosition = target.transform.position + Vector3.up * raycastOffset;
            Vector3 direction = (targetPosition - startPosition).normalized;
            float distance = Vector3.Distance(startPosition, targetPosition);
            
            // 進行 Raycast 檢查
            RaycastHit hit;
            bool hasLineOfSight = true;
            
            if (Physics.Raycast(startPosition, direction, out hit, distance, obstacleLayerMask))
            {
                // 檢查碰撞到的物體是否是目標本身或其子物件
                Transform hitTransform = hit.transform;
                bool hitTarget = false;
                
                // 檢查是否碰撞到目標或目標的子物件
                while (hitTransform != null)
                {
                    if (hitTransform == target.transform)
                    {
                        hitTarget = true;
                        break;
                    }
                    hitTransform = hitTransform.parent;
                }
                
                if (!hitTarget)
                {
                    hasLineOfSight = false;
                    if (showDebugRaycast)
                    {
                        Debug.Log($"[AI] {enemy.gameObject.name} 到 {target.Name} 的視線被 {hit.collider.name} 阻擋");
                    }
                }
            }
            
            // 繪製除錯射線
            if (showDebugRaycast)
            {
                Color rayColor = hasLineOfSight ? Color.green : Color.red;
                Debug.DrawRay(startPosition, direction * distance, rayColor, 2f);
            }
            
            return hasLineOfSight;
        }
        
        /// <summary>
        /// 判斷目標是否為敵對陣營
        /// </summary>
        private bool IsHostileFaction(CombatEntity target)
        {
            // 如果自己是敵對陣營，攻擊友軍和中立
            if (enemyCombatEntity.Faction == CombatEntityFaction.Hostile)
            {
                return target.Faction == CombatEntityFaction.Ally || 
                       target.Faction == CombatEntityFaction.Neutral;
            }
            // 如果自己是友軍，攻擊敵對
            else if (enemyCombatEntity.Faction == CombatEntityFaction.Ally)
            {
                return target.Faction == CombatEntityFaction.Hostile;
            }
            // 如果自己是中立，通常不主動攻擊
            else if (enemyCombatEntity.Faction == CombatEntityFaction.Neutral)
            {
                return false;
            }
            
            return false;
        }
        
        /// <summary>
        /// 根據攻擊目標類型選擇目標
        /// </summary>
        private CombatEntity SelectTarget(EnemyCore enemy)
        {
            if (availableTargets.Count == 0)
                return null;
            
            CombatEntity selectedTarget = null;
            
            switch (targetType)
            {
                case RangeAttackTargetType.ClosestPlayer:
                    float closestDistance = float.MaxValue;
                    foreach (var target in availableTargets)
                    {
                        float distance = Vector3.Distance(enemy.transform.position, target.transform.position);
                        if (distance < closestDistance)
                        {
                            closestDistance = distance;
                            selectedTarget = target;
                        }
                    }
                    break;
                    
                case RangeAttackTargetType.LowestHealth:
                    float lowestHealth = float.MaxValue;
                    foreach (var target in availableTargets)
                    {
                        Health health = target.GetComponent<Health>();
                        if (health != null && health.currentHealth < lowestHealth)
                        {
                            lowestHealth = health.currentHealth;
                            selectedTarget = target;
                        }
                    }
                    break;
                    
                case RangeAttackTargetType.HighestThreat:
                    // TODO: 實作威脅值系統
                    // 暫時使用最近的目標
                    selectedTarget = availableTargets[0];
                    break;
                    
                case RangeAttackTargetType.SpecificTarget:
                    // 使用第一個可用的目標
                    selectedTarget = availableTargets[0];
                    break;
            }
            
            return selectedTarget;
        }
        
        protected override bool CanExecuteInternal(EnemyCore enemy)
        {
            // 檢查 AP 是否足夠
            if (enemy.CurrentActionPoints < apCost)
            {
                Debug.Log($"[AI] {enemy.gameObject.name} AP不足無法遠距攻擊 (需要: {apCost}, 當前: {enemy.CurrentActionPoints})");
                return false;
            }
            
            // 檢查是否有 ProjectileShooter
            if (projectileShooter == null)
            {
                Debug.LogWarning($"[AI] {enemy.gameObject.name} 沒有 ProjectileShooter，無法執行遠距攻擊");
                return false;
            }
            
            // 如果已初始化且有快取目標，檢查目標是否仍然有效
            if (isInitialized && cachedTarget != null)
            {
                // 檢查目標是否仍然存在且活躍
                if (!cachedTarget.gameObject.activeInHierarchy)
                {
                    Debug.Log($"[AI] {enemy.gameObject.name} 的快取目標已失效，需要重新選擇目標");
                    return false;
                }
                
                // 檢查距離是否仍在攻擊範圍內
                float distance = Vector3.Distance(enemy.transform.position, cachedTarget.transform.position);
                if (distance < minAttackRange || distance > maxAttackRange)
                {
                    if (distance < minAttackRange)
                        Debug.Log($"[AI] {enemy.gameObject.name} 的目標太接近 (距離: {distance:F2}, 最小範圍: {minAttackRange})");
                    else
                        Debug.Log($"[AI] {enemy.gameObject.name} 的目標超出攻擊範圍 (距離: {distance:F2}, 最大範圍: {maxAttackRange})");
                    return false;
                }
                
                // 檢查是否仍有視線
                if (!HasLineOfSight(enemy, cachedTarget))
                {
                    Debug.Log($"[AI] {enemy.gameObject.name} 對目標 {cachedTarget.name} 失去視線");
                    return false;
                }
                
                return true;
            }
            
            // 如果沒有快取目標，嘗試尋找新目標
            RefreshTargetList(enemy);
            return availableTargets.Count > 0;
        }
        
        public override IEnumerator Execute(EnemyCore enemy)
        {
            // 確保有有效的目標
            if (!isInitialized || cachedTarget == null)
            {
                Debug.LogError($"[AI] {enemy.gameObject.name} 執行遠距攻擊時沒有有效目標");
                yield break;
            }
            
            // 再次檢查目標有效性
            if (!cachedTarget.gameObject.activeInHierarchy)
            {
                Debug.Log($"[AI] {enemy.gameObject.name} 的攻擊目標在執行時已失效");
                yield break;
            }
            
            Debug.Log($"[AI] {enemy.gameObject.name} 開始遠距攻擊 {cachedTarget.name}, AP: {enemy.CurrentActionPoints}");
            
            // 面向目標
            Vector3 lookDirection = cachedTarget.transform.position - enemy.transform.position;
            lookDirection.y = 0;
            if (lookDirection != Vector3.zero)
            {
                enemy.transform.rotation = Quaternion.LookRotation(lookDirection);
            }
            
            // 執行遠距攻擊
            yield return ExecuteRangeAttack(enemy);
            
            // 消耗AP
            enemy.SpendActionPoints(apCost);
            
            Debug.Log($"[AI] {enemy.gameObject.name} 遠距攻擊完成，剩餘AP: {enemy.CurrentActionPoints}");
        }
        
        /// <summary>
        /// 執行遠距攻擊
        /// </summary>
        private IEnumerator ExecuteRangeAttack(EnemyCore enemy)
        {
            // 取得相機控制器
            CombatCameraController cameraController = Camera.main?.GetComponent<CombatCameraController>();
            
            if (cameraController != null)
            {
                // 聚焦到敵人位置
                cameraController.FocusOnGameObject(enemy.gameObject);
                
                // 設定相機事件監聽
                UnityEngine.Events.UnityAction onAttackEnter = () => {
                    cameraController.StartAttackZoom(0.85f, 0.5f);
                };
                
                UnityEngine.Events.UnityAction onAttackExit = () => {
                    cameraController.EndAttackZoom(0.5f);
                };
                
                // 添加事件監聽
                enemy.onAttackStateEnter.AddListener(onAttackEnter);
                enemy.onAttackStateExit.AddListener(onAttackExit);
                
                // 清理函數
                System.Action cleanup = () => {
                    enemy.onAttackStateEnter.RemoveListener(onAttackEnter);
                    enemy.onAttackStateExit.RemoveListener(onAttackExit);
                };
                
                // 切換到攻擊狀態
                enemy.SetState(EnemyState.Attacking);
                
                // 等待縮放動畫完成
                yield return new WaitForSeconds(0.5f);
                
                // 設定 ProjectileShooter 目標
                if (projectileShooter != null)
                {
                    projectileShooter.SetTarget(cachedTarget.transform);
                    Debug.Log($"[AI] {enemy.gameObject.name} 發射投射物攻擊 {cachedTarget.name}");
                }
                
                // 播放攻擊動畫
                if (cachedAnimator != null && !string.IsNullOrEmpty(attackAnimationName))
                {
                    cachedAnimator.Play(attackAnimationName, 0, 0f);
                    Debug.Log($"[AI] {enemy.gameObject.name} 播放遠距攻擊動畫: {attackAnimationName}");
                    
                    // 等待動畫播放完成
                    yield return StartCoroutine(WaitForAnimationComplete(cachedAnimator, attackAnimationName));
                }
                else
                {
                    // 使用備用等待時間
                    Debug.LogWarning($"[AI] {enemy.gameObject.name} 無法播放遠距攻擊動畫，使用備用等待時間");
                    yield return new WaitForSeconds(attackDuration);
                }
                
                // 攻擊結束
                enemy.SetState(EnemyState.ExecutingTurn);
                cleanup();
                
                // 等待相機恢復動畫完成
                yield return new WaitForSeconds(0.5f);
            }
            else
            {
                // 沒有相機控制器時的簡化處理
                enemy.SetState(EnemyState.Attacking);
                
                // 設定並啟用 ProjectileShooter
                if (projectileShooter != null)
                {
                    projectileShooter.SetTarget(cachedTarget.transform);
                    projectileShooter.gameObject.SetActive(true);
                }
                
                // 播放動畫或等待
                if (cachedAnimator != null && !string.IsNullOrEmpty(attackAnimationName))
                {
                    cachedAnimator.Play(attackAnimationName, 0, 0f);
                    yield return StartCoroutine(WaitForAnimationComplete(cachedAnimator, attackAnimationName));
                }
                else
                {
                    yield return new WaitForSeconds(attackDuration);
                }
                
                enemy.SetState(EnemyState.ExecutingTurn);
            }
        }
        
        /// <summary>
        /// 等待動畫播放完成
        /// </summary>
        private IEnumerator WaitForAnimationComplete(Animator animator, string animationName)
        {
            yield return null; // 等待一幀確保動畫狀態切換
            
            AnimatorStateInfo stateInfo;
            bool isPlayingAttack = false;
            float animationStartTime = Time.time;
            float maxWaitTime = 5f; // 最大等待時間，防止無限循環
            
            // 先等待動畫真正開始
            while (!isPlayingAttack && (Time.time - animationStartTime) < maxWaitTime)
            {
                stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                if (stateInfo.IsName(animationName) && stateInfo.normalizedTime < 0.95f)
                {
                    isPlayingAttack = true;
                    Debug.Log($"[AI] 遠距攻擊動畫開始播放");
                }
                yield return null;
            }
            
            // 等待動畫播放完成
            while (isPlayingAttack && (Time.time - animationStartTime) < maxWaitTime)
            {
                stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                
                if (stateInfo.IsName(animationName))
                {
                    if (stateInfo.normalizedTime >= 0.95f && !stateInfo.loop)
                    {
                        isPlayingAttack = false;
                        Debug.Log($"[AI] 遠距攻擊動畫播放完成");
                    }
                }
                else
                {
                    isPlayingAttack = false;
                    Debug.Log($"[AI] 遠距攻擊動畫已結束或切換到其他狀態");
                }
                
                yield return null;
            }
            
            if ((Time.time - animationStartTime) >= maxWaitTime)
            {
                Debug.LogWarning($"[AI] 遠距攻擊動畫等待超時");
            }
        }
        
        public override string GetActionName()
        {
            return $"RangeAttack {targetType} (Range: {minAttackRange}-{maxAttackRange}, Damage: {attackDamage}, AP: {apCost})";
        }
        
        /// <summary>
        /// 繪製攻擊範圍和視線檢查（用於除錯）
        /// </summary>
        public void DrawAttackRangeAndLineOfSight(EnemyCore enemy)
        {
            #if UNITY_EDITOR
            // 繪製最小攻擊範圍（紅色內圈）
            UnityEditor.Handles.color = Color.red;
            UnityEditor.Handles.DrawWireDisc(enemy.transform.position, Vector3.up, minAttackRange);
            
            // 繪製最大攻擊範圍（藍色外圈）
            UnityEditor.Handles.color = Color.blue;
            UnityEditor.Handles.DrawWireDisc(enemy.transform.position, Vector3.up, maxAttackRange);
            
            // 繪製有效攻擊區域（環形區域的邊界）
            UnityEditor.Handles.color = Color.yellow;
            UnityEditor.Handles.Label(
                enemy.transform.position + Vector3.up * 2f, 
                $"Range: {minAttackRange:F1} - {maxAttackRange:F1}"
            );
            
            // 繪製視線檢查
            if (cachedTarget != null)
            {
                Vector3 startPos = enemy.transform.position + Vector3.up * raycastOffset;
                Vector3 targetPos = cachedTarget.transform.position + Vector3.up * raycastOffset;
                
                bool hasLOS = HasLineOfSight(enemy, cachedTarget);
                UnityEditor.Handles.color = hasLOS ? Color.green : Color.red;
                UnityEditor.Handles.DrawLine(startPos, targetPos);
            }
            #endif
        }
    }
}