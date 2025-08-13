using UnityEngine;

namespace Wuxia.GameCore
{
    public interface ICondition
    {
        bool Evaluate(EnemyCore enemy);
        string GetConditionName();
    }
    
    [System.Serializable]
    public abstract class BaseCondition : MonoBehaviour, ICondition
    {
        [SerializeField] protected bool invert = false;
        
        public bool Evaluate(EnemyCore enemy)
        {
            bool result = EvaluateInternal(enemy);
            return invert ? !result : result;
        }
        
        protected abstract bool EvaluateInternal(EnemyCore enemy);
        public abstract string GetConditionName();
    }
    
    [System.Serializable]
    public class CompositeCondition : BaseCondition
    {
        public enum LogicOperator
        {
            AND,
            OR
        }
        
        [SerializeField] private LogicOperator logicOperator = LogicOperator.AND;
        [SerializeField] private BaseCondition[] conditions;
        
        protected override bool EvaluateInternal(EnemyCore enemy)
        {
            if (conditions == null || conditions.Length == 0)
                return true;
                
            if (logicOperator == LogicOperator.AND)
            {
                foreach (var condition in conditions)
                {
                    if (!condition.Evaluate(enemy))
                        return false;
                }
                return true;
            }
            else
            {
                foreach (var condition in conditions)
                {
                    if (condition.Evaluate(enemy))
                        return true;
                }
                return false;
            }
        }
        
        public override string GetConditionName()
        {
            return $"Composite({logicOperator})";
        }
    }
}