using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Wuxia.GameCore
{
    [System.Serializable]
    public class EnemyStrategy : MonoBehaviour
    {
        public List<BaseCondition> Condition;
        public List<BaseEnemyAction> Actions = new List<BaseEnemyAction>();
        [SerializeField] private string strategyName;
        [SerializeField] private float priority = 1f;
        
        
        public string StrategyName => strategyName;
        public float Priority => priority;
        
        public bool CanExecute(EnemyCore enemy)
        {
            for (int i = 0; i < Condition.Count; i++)
            {
                if (!Condition[i].Evaluate(enemy))
                    return false;
            }

            // 至少要有一個行動可以執行
            foreach (var action in Actions)
            {
                if (action.CanExecute(enemy))
                    return true;
            }
            
            return Actions.Count == 0; // 如果沒有行動，也算可執行
        }
        
        public IEnumerator ExecuteStrategy(EnemyCore enemy)
        {
            Debug.Log($"[AI] Executing strategy: {strategyName}");
            
            foreach (var action in Actions)
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
    }
}