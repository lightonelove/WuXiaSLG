using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace Wuxia.GameCore
{
    [System.Serializable]
    public class EnemyMoveAction : BaseEnemyAction
    {
        public enum MoveTargetType
        {
            ToPlayer,
            AwayFromPlayer,
            ToPosition,
            RandomPosition
        }
        
        [SerializeField] private MoveTargetType moveType = MoveTargetType.ToPlayer;
        [SerializeField] private float moveDistance = 5f;
        [SerializeField] private Vector3 targetPosition;
        [SerializeField] private float randomRadius = 10f;
        
        [Header("ToPlayer 追逐設定")]
        [SerializeField] private float stoppingDistance = 2f; // 停止追蹤的距離
        [SerializeField] private float stoppingRemainingAP = 30f; // 剩下多少ap就不移動
        [SerializeField] private bool useNavMesh = true; // 是否使用NavMesh
        
        // 快取的目標和初始化狀態
        private Transform cachedTarget;
        private bool isInitialized;
        
        public override void InitializeAction(EnemyCore enemy)
        {
            base.InitializeAction(enemy);
            
            // 重置初始化狀態和快取
            isInitialized = false;
            cachedTarget = null;
            
            // EnemyMoveAction 特定的初始化邏輯
            if (moveType == MoveTargetType.ToPlayer)
            {
                // 預先檢查 NavMeshAgent 是否可用
                if (useNavMesh && enemy.navAgent == null)
                {
                    Debug.LogWarning($"[AI] {enemy.gameObject.name} 的 EnemyMoveAction 需要 NavMeshAgent 但找不到該元件");
                    return;
                }
                
                // 一次性尋找目標並初始化 NavMesh
                cachedTarget = FindClosestPlayer(enemy);
                if (cachedTarget != null)
                {
                    // 初始化 NavMesh
                    if (useNavMesh && enemy.navAgent != null)
                    {
                        if (!enemy.navAgent.enabled)
                        {
                            enemy.navAgent.enabled = true;
                            enemy.navAgent.speed = 0; // 先設為0，透過程式控制移動
                        }
                        enemy.navAgent.SetDestination(cachedTarget.position);
                        Debug.Log($"[AI] {enemy.gameObject.name} NavMeshAgent 已初始化並設定目標");
                    }
                    
                    isInitialized = true;
                    Debug.Log($"[AI] {enemy.gameObject.name} ToPlayer 移動已初始化，目標: {cachedTarget.name}");
                }
                else
                {
                    Debug.LogWarning($"[AI] {enemy.gameObject.name} 初始化失敗：找不到目標玩家");
                }
            }
            else
            {
                // 其他移動類型也標記為已初始化
                isInitialized = true;
                Debug.Log($"[AI] {enemy.gameObject.name} {moveType} 移動已初始化");
            }
        }
        
        protected override bool CanExecuteInternal(EnemyCore enemy)
        {
            if (enemy.CurrentActionPoints < CombatConfig.Instance.APCostPerMeter)
                return false;
            
            return true; // 簡單檢查，可以加入更複雜的邏輯
        }
        
        public override IEnumerator Execute(EnemyCore enemy)
        {
            if (moveType == MoveTargetType.ToPlayer)
            {
                // 使用從 EnemyCore 移植的完整追逐邏輯
                yield return ExecuteToPlayerChase(enemy);
            }
            else
            {
                // 其他移動類型使用原本的簡單邏輯
                yield return ExecuteSimpleMovement(enemy);
            }
        }
        
        /// <summary>
        /// 執行追逐玩家的完整邏輯 (使用快取的目標)
        /// </summary>
        private IEnumerator ExecuteToPlayerChase(EnemyCore enemy)
        {
            // 檢查是否已初始化
            if (!isInitialized || cachedTarget == null)
            {
                Debug.LogError($"[AI] {enemy.gameObject.name} ToPlayer 移動未正確初始化，無法執行");
                yield break;
            }
            
            Debug.Log($"[AI] {enemy.gameObject.name} 開始追逐玩家, AP: {enemy.CurrentActionPoints}, 目標: {cachedTarget.name}");
            
            // 持續追逐直到條件不滿足
            while (enemy.CurrentActionPoints > stoppingRemainingAP)
            {
                // 檢查目標是否仍然存在且有效
                if (cachedTarget == null || !cachedTarget.gameObject.activeInHierarchy)
                {
                    Debug.Log($"[AI] {enemy.gameObject.name} 目標玩家已失效，停止追逐");
                    break;
                }
                
                float distanceToTarget = Vector3.Distance(enemy.transform.position, cachedTarget.position);
                
                // 如果已經足夠接近目標，結束移動
                if (distanceToTarget <= stoppingDistance)
                {
                    Debug.Log($"[AI] {enemy.gameObject.name} 已接近目標，停止移動 (距離: {distanceToTarget})");
                    break;
                }
                
                // 移動向目標（NavMesh 已經在 InitializeAction 中設定）
                bool moved = MoveTowardsTarget(enemy, cachedTarget);
                
                if (!moved)
                {
                    Debug.Log($"[AI] {enemy.gameObject.name} 無法繼續移動，剩餘AP: {enemy.CurrentActionPoints}");
                    break;
                }
                
                yield return null; // 等待下一幀
            }
            
            Debug.Log($"[AI] {enemy.gameObject.name} 追逐完成，剩餘AP: {enemy.CurrentActionPoints}");
            
            // 清理NavMesh
            CleanupNavMesh(enemy);
        }
        
        /// <summary>
        /// 其他移動類型的簡單執行邏輯
        /// </summary>
        private IEnumerator ExecuteSimpleMovement(EnemyCore enemy)
        {
            Vector3 destination = CalculateDestination(enemy);
            float distanceToMove = Vector3.Distance(enemy.transform.position, destination);
            float apCost = distanceToMove * CombatConfig.Instance.APCostPerMeter;
            
            // 檢查AP是否足夠
            if (enemy.CurrentActionPoints < apCost)
            {
                // 只移動能負擔的距離
                distanceToMove = enemy.CurrentActionPoints / CombatConfig.Instance.APCostPerMeter;
                Vector3 direction = (destination - enemy.transform.position).normalized;
                destination = enemy.transform.position + direction * distanceToMove;
                apCost = enemy.CurrentActionPoints;
            }
            
            Debug.Log($"[AI] Enemy moving to {destination}, distance: {distanceToMove}, AP cost: {apCost}");
            
            // 簡化版移動 - 直接設置位置
            float moveSpeed = 5f;
            Vector3 startPos = enemy.transform.position;
            float journey = 0f;
            
            while (journey <= 1f)
            {
                journey += Time.deltaTime * moveSpeed / distanceToMove;
                enemy.transform.position = Vector3.Lerp(startPos, destination, journey);
                yield return null;
            }
            
            // 消耗AP
            enemy.SpendActionPoints(apCost);
        }
        
        private Vector3 CalculateDestination(EnemyCore enemy)
        {
            Vector3 enemyPosition = enemy.transform.position;
            
            switch (moveType)
            {
                case MoveTargetType.ToPlayer:
                    CharacterCore player = GameObject.FindObjectOfType<CharacterCore>();
                    if (player != null)
                    {
                        Vector3 direction = (player.transform.position - enemyPosition).normalized;
                        return enemyPosition + direction * moveDistance;
                    }
                    break;
                    
                case MoveTargetType.AwayFromPlayer:
                    player = GameObject.FindObjectOfType<CharacterCore>();
                    if (player != null)
                    {
                        Vector3 direction = (enemyPosition - player.transform.position).normalized;
                        return enemyPosition + direction * moveDistance;
                    }
                    break;
                    
                case MoveTargetType.ToPosition:
                    return targetPosition;
                    
                case MoveTargetType.RandomPosition:
                    Vector2 randomCircle = Random.insideUnitCircle * randomRadius;
                    return enemyPosition + new Vector3(randomCircle.x, 0, randomCircle.y);
            }
            
            return enemyPosition;
        }
        
        public override string GetActionName()
        {
            return $"Move {moveType}";
        }
        
        /// <summary>
        /// 尋找最近的玩家 (從 EnemyCore.FindClosestPlayer 移植)
        /// </summary>
        private Transform FindClosestPlayer(EnemyCore enemy)
        {
            float closestDistance = float.MaxValue;
            CharacterCore closestCharacter = null;
            
            // 從CombatCore獲取所有玩家角色
            if (CombatCore.Instance != null)
            {
                foreach (var character in CombatCore.Instance.AllCharacters)
                {
                    if (character != null && character.gameObject.activeInHierarchy)
                    {
                        // 使用角色的真實位置計算距離
                        Vector3 playerRealPos = character.GetRealPosition();
                        float distance = Vector3.Distance(enemy.transform.position, playerRealPos);
                        
                        if (distance < closestDistance)
                        {
                            closestDistance = distance;
                            closestCharacter = character;
                        }
                    }
                }
            }
            
            // 設定目標為最近角色的真實 Transform
            if (closestCharacter != null)
            {
                Transform target = closestCharacter.GetRealTransform();
                Debug.Log($"[AI] {enemy.gameObject.name} 鎖定目標: {closestCharacter.name} (真實位置: {closestCharacter.GetRealPosition()})");
                return target;
            }
            
            Debug.Log($"[AI] {enemy.gameObject.name} 沒有找到目標");
            return null;
        }
        
        
        /// <summary>
        /// 清理NavMesh (從 EnemyCore.EndTurn 移植)
        /// </summary>
        private void CleanupNavMesh(EnemyCore enemy)
        {
            if (enemy.animator != null)
            {
                enemy.animator.SetBool("isMoving", false);
            }
            
            // 如果使用NavMesh，停止移動並關閉NavMeshAgent
            if (useNavMesh && enemy.navAgent != null && enemy.navAgent.enabled)
            {
                enemy.navAgent.ResetPath();
                enemy.navAgent.enabled = false;
            }
        }
        
        /// <summary>
        /// 向目標移動 (從 EnemyCore.MoveTowardsTarget 移植)
        /// </summary>
        private bool MoveTowardsTarget(EnemyCore enemy, Transform target)
        {
            if (target == null)
            {
                Debug.Log($"[AI] {enemy.gameObject.name} MoveTowardsTarget: 沒有目標");
                return false;
            }
            
            if (enemy.CurrentActionPoints <= 0)
            {
                Debug.Log($"[AI] {enemy.gameObject.name} MoveTowardsTarget: AP不足 ({enemy.CurrentActionPoints})");
                return false;
            }
            // 統一使用NavMeshAgent移動
            if (enemy.navAgent != null && enemy.navAgent.enabled)
            {
                return MoveWithNavMesh(enemy, target);
            }
            
            Debug.Log($"[AI] {enemy.gameObject.name} NavMeshAgent 未啟用或不存在");
            return false;
        }
        
        /// <summary>
        /// 使用NavMesh移動 (從 EnemyCore.MoveWithNavMesh 移植)
        /// </summary>
        private bool MoveWithNavMesh(EnemyCore enemy, Transform target)
        {
            // 更新目標位置
            enemy.navAgent.SetDestination(target.position);
            
            if (!enemy.navAgent.pathPending && enemy.navAgent.remainingDistance > 0.1f)
            {
                // 計算這一幀要移動的距離
                float frameDistance = enemy.moveSpeed * Time.deltaTime;
                float apCost = frameDistance * CombatConfig.Instance.APCostPerMeter;
                
                // 檢查AP是否足夠
                if (enemy.CurrentActionPoints < apCost)
                {
                    frameDistance = enemy.CurrentActionPoints / CombatConfig.Instance.APCostPerMeter;
                    apCost = enemy.CurrentActionPoints;
                }
                
                if (frameDistance > 0)
                {
                    // 設定NavMeshAgent的速度來控制移動
                    enemy.navAgent.speed = frameDistance / Time.deltaTime;
                    
                    // 消耗AP
                    enemy.SpendActionPoints(apCost);
                    
                    if (enemy.animator != null)
                    {
                        enemy.animator.SetBool("isMoving", true);
                    }
                    
                    return true;
                }
            }
            
            return false;
        }
    }
}