using System.Collections;
using UnityEngine;

namespace Wuxia.GameCore
{
    public interface IEnemyAction
    {
        ICondition Condition { get; set; }
        IEnumerator Execute(EnemyCore enemy);
        string GetActionName();
        bool CanExecute(EnemyCore enemy);
    }
    
    [System.Serializable]
    public abstract class BaseEnemyAction : IEnemyAction
    {
        [SerializeField] protected BaseCondition condition;
        
        public ICondition Condition 
        { 
            get => condition; 
            set => condition = value as BaseCondition; 
        }
        
        public virtual bool CanExecute(EnemyCore enemy)
        {
            if (condition != null && !condition.Evaluate(enemy))
                return false;
            
            return CanExecuteInternal(enemy);
        }
        
        protected abstract bool CanExecuteInternal(EnemyCore enemy);
        
        public abstract IEnumerator Execute(EnemyCore enemy);
        
        public abstract string GetActionName();
    }
}