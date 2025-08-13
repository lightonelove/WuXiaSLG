using UnityEngine;

namespace Wuxia.GameCore
{
    [System.Serializable]
    public class DistanceCondition : BaseCondition
    {
        public enum DistanceCheckType
        {
            ToPlayer,
            ToTarget,
            ToPosition
        }
        
        public enum ComparisonType
        {
            Within,
            Beyond
        }
        
        [SerializeField] private DistanceCheckType checkType = DistanceCheckType.ToPlayer;
        [SerializeField] private ComparisonType comparison = ComparisonType.Within;
        [SerializeField] private float distance = 5f;
        [SerializeField] private Vector3 targetPosition;
        [SerializeField] private Transform targetTransform;
        
        protected override bool EvaluateInternal(EnemyCore enemy)
        {
            if (enemy == null) return false;
            
            Vector3 enemyPosition = enemy.transform.position;
            Vector3 comparePosition = Vector3.zero;
            bool hasValidTarget = false;
            
            switch (checkType)
            {
                case DistanceCheckType.ToPlayer:
                    CharacterCore player = GameObject.FindObjectOfType<CharacterCore>();
                    if (player != null)
                    {
                        comparePosition = player.transform.position;
                        hasValidTarget = true;
                    }
                    break;
                    
                case DistanceCheckType.ToTarget:
                    if (targetTransform != null)
                    {
                        comparePosition = targetTransform.position;
                        hasValidTarget = true;
                    }
                    break;
                    
                case DistanceCheckType.ToPosition:
                    comparePosition = targetPosition;
                    hasValidTarget = true;
                    break;
            }
            
            if (!hasValidTarget) return false;
            
            float currentDistance = Vector3.Distance(enemyPosition, comparePosition);
            
            return comparison == ComparisonType.Within ? 
                currentDistance <= distance : 
                currentDistance > distance;
        }
        
        public override string GetConditionName()
        {
            return $"Distance {checkType} {comparison} {distance}m";
        }
    }
}