using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Wuxia.GameCore
{
    public interface IEnemyAction
    {
        IEnumerator Execute(EnemyCore enemy);
        string GetActionName();
        bool CanExecute(EnemyCore enemy);
    }
    
    [System.Serializable]
    public abstract class BaseEnemyAction : MonoBehaviour, IEnemyAction
    {
        [SerializeField] public List<BaseCondition> Condition;
        

        public virtual bool CanExecute(EnemyCore enemy)
        {
            for (int i = 0; i < Condition.Count; i++)
            {
                if (!Condition[i].Evaluate(enemy))
                    return false;
            }
            
            return CanExecuteInternal(enemy);
        }
        
        protected abstract bool CanExecuteInternal(EnemyCore enemy);
        
        public abstract IEnumerator Execute(EnemyCore enemy);
        
        public abstract string GetActionName();
    }
}