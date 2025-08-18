using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Wuxia.GameCore
{
    [System.Serializable]
    public class EnemyAttackAction : BaseEnemyAction
    {
        public enum AttackTargetType
        {
            ClosestPlayer,      // 攻擊最近的玩家
            LowestHealth,       // 攻擊血量最低的玩家
            HighestThreat,      // 攻擊威脅值最高的玩家
            SpecificTarget      // 攻擊特定目標
        }
        
        [Header("攻擊基礎設定")]
        [SerializeField] private AttackTargetType targetType = AttackTargetType.ClosestPlayer;
        [SerializeField] private float attackRange = 2f;
        [SerializeField] private float attackDamage = 10f;
        [SerializeField] private float apCost = 30f;
        
        [Header("攻擊動畫設定")]
        [SerializeField] private float attackDuration = 1f;
        [SerializeField] private string attackAnimationName = "Attack"; // 動畫名稱
        
        
        // 快取的目標和元件
        private CombatEntity cachedTarget;
        private Animator cachedAnimator;
        private bool isInitialized;
        private List<CombatEntity> availableTargets = new List<CombatEntity>();
        private CombatEntity enemyCombatEntity; // 快取敵人的 CombatEntity
        
        public override void InitializeAction(EnemyCore enemy)
        {
            base.InitializeAction(enemy);
            
            // 重置初始化狀態和快取
            isInitialized = false;
            cachedTarget = null;
            cachedAnimator = null;
            availableTargets = new List<CombatEntity>();
            
            // 快取敵人的 CombatEntity
            enemyCombatEntity = enemy.GetComponent<CombatEntity>();
            if (enemyCombatEntity == null)
            {
                Debug.LogError($"[AI] {enemy.gameObject.name} 沒有 CombatEntity 組件！");
                return;
            }
            
            // 快取 Animator
            cachedAnimator = enemy.animator;
            if (cachedAnimator == null)
            {
                Debug.LogWarning($"[AI] {enemy.gameObject.name} 的 EnemyAttackAction 需要 Animator 但找不到該元件");
            }
            else if (string.IsNullOrEmpty(attackAnimationName))
            {
                Debug.LogWarning($"[AI] {enemy.gameObject.name} 的 EnemyAttackAction 未設定攻擊動畫名稱");
            }
            
            // 尋找並快取可攻擊的目標
            RefreshTargetList(enemy);
            
            // 根據目標類型選擇目標
            cachedTarget = SelectTarget(enemy);
            
            if (cachedTarget != null)
            {
                isInitialized = true;
                Debug.Log($"[AI] {enemy.gameObject.name} 攻擊初始化完成，目標: {cachedTarget.name}, 攻擊類型: {targetType}");
            }
            else
            {
                Debug.LogWarning($"[AI] {enemy.gameObject.name} 攻擊初始化失敗：找不到合適的目標");
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
                // 使用 CombatEntity 列表來尋找目標
                foreach (var entity in CombatCore.Instance.AllCombatEntity)
                {
                    if (entity != null && entity.gameObject.activeInHierarchy)
                    {
                        // 檢查陣營 - 只攻擊不同陣營的目標
                        if (IsHostileFaction(entity))
                        {
                            // 檢查目標是否在攻擊範圍內
                            float distance = Vector3.Distance(enemy.transform.position, entity.transform.position);
                            Debug.Log($"[AI] 檢查目標 {entity.Name}, 陣營: {entity.Faction}, 距離: {distance}");
                            
                            if (distance <= attackRange)
                            {
                                availableTargets.Add(entity);
                            }
                        }
                    }
                }
            }
            
            Debug.Log($"[AI] {enemy.gameObject.name} 找到 {availableTargets.Count} 個可攻擊目標");
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
                case AttackTargetType.ClosestPlayer:
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
                    
                case AttackTargetType.LowestHealth:
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
                    
                case AttackTargetType.HighestThreat:
                    // TODO: 實作威脅值系統
                    // 暫時使用最近的目標
                    selectedTarget = availableTargets[0];
                    break;
                    
                case AttackTargetType.SpecificTarget:
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
                Debug.Log($"[AI] {enemy.gameObject.name} AP不足無法攻擊 (需要: {apCost}, 當前: {enemy.CurrentActionPoints})");
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
                if (distance > attackRange)
                {
                    Debug.Log($"[AI] {enemy.gameObject.name} 的目標超出攻擊範圍 (距離: {distance}, 範圍: {attackRange})");
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
                Debug.LogError($"[AI] {enemy.gameObject.name} 執行攻擊時沒有有效目標");
                yield break;
            }
            
            // 再次檢查目標有效性
            if (!cachedTarget.gameObject.activeInHierarchy)
            {
                Debug.Log($"[AI] {enemy.gameObject.name} 的攻擊目標在執行時已失效");
                yield break;
            }
            
            Debug.Log($"[AI] {enemy.gameObject.name} 開始攻擊 {cachedTarget.name}, AP: {enemy.CurrentActionPoints}");
            
            // 面向目標
            Vector3 lookDirection = cachedTarget.transform.position - enemy.transform.position;
            lookDirection.y = 0;
            if (lookDirection != Vector3.zero)
            {
                enemy.transform.rotation = Quaternion.LookRotation(lookDirection);
            }
            
            // 執行單次攻擊
            yield return ExecuteSingleAttack(enemy);
            
            // 消耗AP
            enemy.SpendActionPoints(apCost);
            
            Debug.Log($"[AI] {enemy.gameObject.name} 攻擊完成，剩餘AP: {enemy.CurrentActionPoints}");
        }
        
        /// <summary>
        /// 執行單次攻擊
        /// </summary>
        private IEnumerator ExecuteSingleAttack(EnemyCore enemy)
        {
            // 取得相機控制器
            CombatCameraController cameraController = Camera.main?.GetComponent<CombatCameraController>();
            
            // 設定相機事件監聽
            if (cameraController != null)
            {
                // 聚焦到敵人位置
                cameraController.FocusOnGameObject(enemy.gameObject);
                
                // 註冊攻擊狀態的相機縮放事件
                UnityEngine.Events.UnityAction onAttackEnter = () => {
                    cameraController.StartAttackZoom(0.85f, 0.5f);
                };
                
                UnityEngine.Events.UnityAction onAttackExit = () => {
                    cameraController.EndAttackZoom(0.5f);
                };
                
                // 添加事件監聽
                enemy.onAttackStateEnter.AddListener(onAttackEnter);
                enemy.onAttackStateExit.AddListener(onAttackExit);
                
                // 確保在攻擊結束後移除監聽（避免記憶體洩漏）
                System.Action cleanup = () => {
                    enemy.onAttackStateEnter.RemoveListener(onAttackEnter);
                    enemy.onAttackStateExit.RemoveListener(onAttackExit);
                };
                
                // 切換到攻擊狀態（這會觸發 onAttackStateEnter 事件）
                enemy.SetState(EnemyState.Attacking);
                
                // 等待縮放動畫完成
                yield return new WaitForSeconds(0.5f);
                
                // 播放攻擊動畫
                Debug.Log("ExecuteSingleAttack");
                if (cachedAnimator != null && !string.IsNullOrEmpty(attackAnimationName))
                {
                    // 強制從頭開始播放動畫（第二個參數 0 表示 layer，第三個參數 0 表示從頭開始）
                    cachedAnimator.Play(attackAnimationName, 0, 0f);
                    Debug.Log($"[AI] {enemy.gameObject.name} 播放攻擊動畫: {attackAnimationName}");
                    
                    // 等待一幀確保動畫狀態切換
                    yield return null;
                    
                    // 等待動畫播放完成
                    AnimatorStateInfo stateInfo;
                    bool isPlayingAttack = false;
                    float animationStartTime = Time.time;
                    float maxWaitTime = 5f; // 最大等待時間，防止無限循環
                    
                    // 先等待動畫真正開始
                    while (!isPlayingAttack && (Time.time - animationStartTime) < maxWaitTime)
                    {
                        stateInfo = cachedAnimator.GetCurrentAnimatorStateInfo(0);
                        if (stateInfo.IsName(attackAnimationName) && stateInfo.normalizedTime < 0.95f)
                        {
                            isPlayingAttack = true;
                            Debug.Log($"[AI] {enemy.gameObject.name} 攻擊動畫開始播放");
                        }
                        yield return null;
                    }
                    
                    // 等待動畫播放完成
                    while (isPlayingAttack && (Time.time - animationStartTime) < maxWaitTime)
                    {
                        stateInfo = cachedAnimator.GetCurrentAnimatorStateInfo(0);
                        
                        // 檢查是否正在播放攻擊動畫
                        if (stateInfo.IsName(attackAnimationName))
                        {
                            // 檢查動畫是否播放完成 (normalizedTime >= 0.95 表示動畫接近完成)
                            if (stateInfo.normalizedTime >= 0.95f && !stateInfo.loop)
                            {
                                isPlayingAttack = false;
                                Debug.Log($"[AI] {enemy.gameObject.name} 攻擊動畫播放完成 (normalizedTime: {stateInfo.normalizedTime})");
                            }
                        }
                        else
                        {
                            // 如果狀態已經不是攻擊動畫，表示動畫已結束或被中斷
                            isPlayingAttack = false;
                            Debug.Log($"[AI] {enemy.gameObject.name} 攻擊動畫已結束或切換到其他狀態");
                        }
                        
                        yield return null;
                    }
                    
                    if ((Time.time - animationStartTime) >= maxWaitTime)
                    {
                        Debug.LogWarning($"[AI] {enemy.gameObject.name} 攻擊動畫等待超時");
                    }
                }
                else
                {
                    // 如果沒有 Animator 或動畫名稱，使用備用的等待時間
                    Debug.LogWarning($"[AI] {enemy.gameObject.name} 無法播放攻擊動畫，使用備用等待時間");
                    yield return new WaitForSeconds(attackDuration);
                }
                
                // 攻擊結束，切換回執行回合狀態（這會觸發 onAttackStateExit 事件）
                enemy.SetState(EnemyState.ExecutingTurn);
                
                // 清理事件監聽
                cleanup();
                
                // 等待相機恢復動畫完成
                yield return new WaitForSeconds(0.5f);
            }
            else
            {
                // 沒有相機控制器時的處理
                enemy.SetState(EnemyState.Attacking);
                
                // 播放攻擊動畫
                Debug.Log("ExecuteSingleAttack");
                if (cachedAnimator != null && !string.IsNullOrEmpty(attackAnimationName))
                {
                    // 強制從頭開始播放動畫
                    cachedAnimator.Play(attackAnimationName, 0, 0f);
                    Debug.Log($"[AI] {enemy.gameObject.name} 播放攻擊動畫: {attackAnimationName}");
                    
                    // 等待一幀確保動畫狀態切換
                    yield return null;
                    
                    // 等待動畫播放完成
                    AnimatorStateInfo stateInfo;
                    bool isPlayingAttack = false;
                    float animationStartTime = Time.time;
                    float maxWaitTime = 5f;
                    
                    // 先等待動畫真正開始
                    while (!isPlayingAttack && (Time.time - animationStartTime) < maxWaitTime)
                    {
                        stateInfo = cachedAnimator.GetCurrentAnimatorStateInfo(0);
                        if (stateInfo.IsName(attackAnimationName) && stateInfo.normalizedTime < 0.95f)
                        {
                            isPlayingAttack = true;
                            Debug.Log($"[AI] {enemy.gameObject.name} 攻擊動畫開始播放");
                        }
                        yield return null;
                    }
                    
                    // 等待動畫播放完成
                    while (isPlayingAttack && (Time.time - animationStartTime) < maxWaitTime)
                    {
                        stateInfo = cachedAnimator.GetCurrentAnimatorStateInfo(0);
                        
                        if (stateInfo.IsName(attackAnimationName))
                        {
                            if (stateInfo.normalizedTime >= 0.95f && !stateInfo.loop)
                            {
                                isPlayingAttack = false;
                                Debug.Log($"[AI] {enemy.gameObject.name} 攻擊動畫播放完成");
                            }
                        }
                        else
                        {
                            isPlayingAttack = false;
                            Debug.Log($"[AI] {enemy.gameObject.name} 攻擊動畫已結束或切換");
                        }
                        
                        yield return null;
                    }
                }
                else
                {
                    Debug.LogWarning($"[AI] {enemy.gameObject.name} 無法播放攻擊動畫，使用備用等待時間");
                    yield return new WaitForSeconds(attackDuration);
                }
                
                enemy.SetState(EnemyState.ExecutingTurn);
            }
        }
        
        public override string GetActionName()
        {
            return $"Attack {targetType} (Range: {attackRange}, Damage: {attackDamage}, AP: {apCost})";
        }
        
        /// <summary>
        /// 繪製攻擊範圍（用於除錯）
        /// </summary>
        public void DrawAttackRange(EnemyCore enemy)
        {
            #if UNITY_EDITOR
            UnityEditor.Handles.color = Color.red;
            UnityEditor.Handles.DrawWireDisc(enemy.transform.position, Vector3.up, attackRange);
            #endif
        }
    }
}