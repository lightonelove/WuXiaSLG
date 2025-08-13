using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Wuxia.GameCore
{
    public interface IEnemyAction
    {
        void InitializeAction(EnemyCore enemy);
        IEnumerator Execute(EnemyCore enemy);
        string GetActionName();
        bool CanExecute(EnemyCore enemy);
    }
    
    [System.Serializable]
    public abstract class BaseEnemyAction : MonoBehaviour, IEnemyAction
    {
        [SerializeField] public List<BaseCondition> Condition;
        
        public virtual void InitializeAction(EnemyCore enemy)
        {
            // 基本的初始化邏輯，子類別可以覆寫
            Debug.Log($"[AI] Initializing action: {GetActionName()} for {enemy.gameObject.name}");
        }

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