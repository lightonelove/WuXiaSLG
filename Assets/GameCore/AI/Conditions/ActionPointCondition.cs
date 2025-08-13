using UnityEngine;

namespace Wuxia.GameCore
{
    [System.Serializable]
    public class ActionPointCondition : BaseCondition
    {
        public enum ComparisonType
        {
            GreaterThan,
            LessThan,
            Equal,
            GreaterOrEqual,
            LessOrEqual
        }
        
        [SerializeField] private ComparisonType comparison = ComparisonType.GreaterOrEqual;
        [SerializeField] private float apValue = 50f;
        
        protected override bool EvaluateInternal(EnemyCore enemy)
        {
            if (enemy == null) return false;
            
            float currentAP = enemy.CurrentActionPoints;
            
            switch (comparison)
            {
                case ComparisonType.GreaterThan:
                    return currentAP > apValue;
                case ComparisonType.LessThan:
                    return currentAP < apValue;
                case ComparisonType.Equal:
                    return Mathf.Approximately(currentAP, apValue);
                case ComparisonType.GreaterOrEqual:
                    return currentAP >= apValue;
                case ComparisonType.LessOrEqual:
                    return currentAP <= apValue;
                default:
                    return false;
            }
        }
        
        public override string GetConditionName()
        {
            return $"AP {comparison} {apValue}";
        }
    }
}