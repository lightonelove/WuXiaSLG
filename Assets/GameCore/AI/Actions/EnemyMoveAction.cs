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
        [SerializeField] private float apCostPerUnit = 10f;
        [SerializeField] private Vector3 targetPosition;
        [SerializeField] private float randomRadius = 10f;
        
        protected override bool CanExecuteInternal(EnemyCore enemy)
        {
            if (enemy.CurrentActionPoints < apCostPerUnit)
                return false;
            
            return true; // 簡單檢查，可以加入更複雜的邏輯
        }
        
        public override IEnumerator Execute(EnemyCore enemy)
        {
            Vector3 destination = CalculateDestination(enemy);
            float distanceToMove = Vector3.Distance(enemy.transform.position, destination);
            float apCost = distanceToMove * apCostPerUnit;
            
            // 檢查AP是否足夠
            if (enemy.CurrentActionPoints < apCost)
            {
                // 只移動能負擔的距離
                distanceToMove = enemy.CurrentActionPoints / apCostPerUnit;
                Vector3 direction = (destination - enemy.transform.position).normalized;
                destination = enemy.transform.position + direction * distanceToMove;
                apCost = enemy.CurrentActionPoints;
            }
            
            Debug.Log($"[AI] Enemy moving to {destination}, distance: {distanceToMove}, AP cost: {apCost}");
            
            // 簡化版移動 - 直接設置位置
            // 在實際項目中，你可能會想要使用更平滑的移動方式
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
    }
}