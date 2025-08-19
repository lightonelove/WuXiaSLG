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
        
        private void Awake()
        {
            // 在 Awake 時自動收集啟用的 Actions
            CollectActiveActions();
        }
        
        private void Start()
        {
            // 在 Start 時再次收集（以防某些 Action 在 Awake 時還沒準備好）
            CollectActiveActions();
        }
        
        /// <summary>
        /// 自動收集所有啟用的 Action
        /// </summary>
        public void CollectActiveActions()
        {
            // 清空現有的行動列表
            Actions.Clear();
            
            // 取得所有的 BaseEnemyAction 元件（包括子物件）
            BaseEnemyAction[] allActions = GetComponentsInChildren<BaseEnemyAction>();
            
            if (allActions != null && allActions.Length > 0)
            {
                foreach (var action in allActions)
                {
                    // 只加入啟用的 Action
                    if (action != null && action.gameObject.activeInHierarchy && action.enabled)
                    {
                        Actions.Add(action);
                        Debug.Log($"[AI] Strategy '{strategyName}' 找到啟用的 Action: {action.GetActionName()}");
                    }
                }
                Debug.Log($"[AI] Strategy '{strategyName}' 總共找到 {Actions.Count} 個啟用的 Action");
            }
            else
            {
                Debug.LogWarning($"[AI] Strategy '{strategyName}' 沒有找到任何 Action");
            }
        }
        
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
            
            // 初始化所有的 Actions
            foreach (var action in Actions)
            {
                if (action != null)
                {
                    action.InitializeAction(enemy);
                }
            }
            
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