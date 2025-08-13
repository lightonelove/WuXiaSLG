using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Wuxia.GameCore
{
    [System.Serializable]
    public class EnemyStrategy
    {
        [SerializeField] private string strategyName;
        [SerializeField] private BaseCondition condition;
        [SerializeField] private float priority = 1f;
        [SerializeField] private List<BaseEnemyAction> actions = new List<BaseEnemyAction>();
        
        public string StrategyName => strategyName;
        public ICondition Condition => condition;
        public float Priority => priority;
        public IReadOnlyList<BaseEnemyAction> Actions => actions;
        
        public EnemyStrategy(string name, float priority = 1f)
        {
            this.strategyName = name;
            this.priority = priority;
        }
        
        public bool CanExecute(EnemyCore enemy)
        {
            if (condition != null && !condition.Evaluate(enemy))
                return false;
                
            // 至少要有一個行動可以執行
            foreach (var action in actions)
            {
                if (action.CanExecute(enemy))
                    return true;
            }
            
            return actions.Count == 0; // 如果沒有行動，也算可執行
        }
        
        public IEnumerator ExecuteStrategy(EnemyCore enemy)
        {
            Debug.Log($"[AI] Executing strategy: {strategyName}");
            
            foreach (var action in actions)
            {
                if (action.CanExecute(enemy))
                {
                    Debug.Log($"[AI] Executing action: {action.GetActionName()}");
                    yield return action.Execute(enemy);
                }
                else
                {
                    Debug.Log($"[AI] Skipping action: {action.GetActionName()} (condition not met)");
                }
            }
        }
        
        public void AddAction(BaseEnemyAction action)
        {
            if (action != null && !actions.Contains(action))
            {
                actions.Add(action);
            }
        }
        
        public void RemoveAction(BaseEnemyAction action)
        {
            actions.Remove(action);
        }
        
        public void SetCondition(BaseCondition newCondition)
        {
            condition = newCondition;
        }
    }
}