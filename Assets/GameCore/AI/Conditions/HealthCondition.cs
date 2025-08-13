using UnityEngine;

namespace Wuxia.GameCore
{
    [System.Serializable]
    public class HealthCondition : BaseCondition
    {
        public enum ComparisonType
        {
            GreaterThan,
            LessThan,
            Equal,
            GreaterOrEqual,
            LessOrEqual
        }
        
        [SerializeField] private ComparisonType comparison = ComparisonType.LessThan;
        [SerializeField] private float healthPercentage = 0.5f;
        
        protected override bool EvaluateInternal(EnemyCore enemy)
        {
            if (enemy == null) return false;
            
            Health health = enemy.GetComponent<Health>();
            if (health == null) return false;
            
            float currentHealthPercent = health.CurrentHealth / health.MaxHealth;
            
            switch (comparison)
            {
                case ComparisonType.GreaterThan:
                    return currentHealthPercent > healthPercentage;
                case ComparisonType.LessThan:
                    return currentHealthPercent < healthPercentage;
                case ComparisonType.Equal:
                    return Mathf.Approximately(currentHealthPercent, healthPercentage);
                case ComparisonType.GreaterOrEqual:
                    return currentHealthPercent >= healthPercentage;
                case ComparisonType.LessOrEqual:
                    return currentHealthPercent <= healthPercentage;
                default:
                    return false;
            }
        }
        
        public override string GetConditionName()
        {
            return $"Health {comparison} {healthPercentage * 100}%";
        }
    }
}