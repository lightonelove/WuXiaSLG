using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Wuxia.GameCore
{
    [System.Serializable]
    public class EnemyAISystem : MonoBehaviour
    {
        [Header("AI Configuration")]
        [SerializeField] private List<EnemyStrategy> strategies = new List<EnemyStrategy>();
        [SerializeField] private bool useRandomSelection = true;
        [SerializeField] private float randomSelectionWeight = 0.5f;
        
        public EnemyCore enemy;
        private EnemyStrategy currentStrategy;
        private Coroutine currentExecutionCoroutine;
        
        public EnemyStrategy CurrentStrategy => currentStrategy;
        public bool IsExecuting => currentExecutionCoroutine != null;
        
        public EnemyStrategy SelectStrategy()
        {
            if (strategies == null || strategies.Count == 0)
            {
                Debug.LogWarning("[AI] No strategies available!");
                return null;
            }
            
            var availableStrategies = strategies
                .Where(s => s.CanExecute(enemy))
                .OrderByDescending(s => s.Priority)
                .ToList();
            
            if (availableStrategies.Count == 0)
            {
                Debug.LogWarning("[AI] No strategies can execute!");
                return null;
            }
            
            EnemyStrategy selectedStrategy;
            
            if (useRandomSelection && availableStrategies.Count > 1)
            {
                // 基於優先級的加權隨機選擇
                float totalWeight = availableStrategies.Sum(s => s.Priority);
                float randomValue = Random.Range(0f, totalWeight);
                float currentWeight = 0f;
                
                selectedStrategy = availableStrategies[0];
                foreach (var strategy in availableStrategies)
                {
                    currentWeight += strategy.Priority;
                    if (randomValue <= currentWeight)
                    {
                        selectedStrategy = strategy;
                        break;
                    }
                }
            }
            else
            {
                // 選擇優先級最高的策略
                selectedStrategy = availableStrategies[0];
            }
            
            return selectedStrategy;
        }
        
        public void ExecuteAI()
        {
            if (IsExecuting)
            {
                Debug.LogWarning("[AI] Already executing a strategy!");
                return;
            }
            
            currentStrategy = SelectStrategy();
            if (currentStrategy != null)
            {
                currentExecutionCoroutine = StartCoroutine(ExecuteStrategyCoroutine());
            }
        }
        
        private IEnumerator ExecuteStrategyCoroutine()
        {
            Debug.Log($"[AI] Starting execution of strategy: {currentStrategy.StrategyName}");
            
            yield return currentStrategy.ExecuteStrategy(enemy);
            
            Debug.Log($"[AI] Completed execution of strategy: {currentStrategy.StrategyName}");
            currentExecutionCoroutine = null;
            currentStrategy = null;
        }
        
        public void StopExecution()
        {
            if (currentExecutionCoroutine != null)
            {
                StopCoroutine(currentExecutionCoroutine);
                currentExecutionCoroutine = null;
                currentStrategy = null;
                Debug.Log("[AI] Execution stopped");
            }
        }
        
        public void AddStrategy(EnemyStrategy strategy)
        {
            if (strategy != null && !strategies.Contains(strategy))
            {
                strategies.Add(strategy);
                Debug.Log($"[AI] Added strategy: {strategy.StrategyName}");
            }
        }
        
        public void RemoveStrategy(EnemyStrategy strategy)
        {
            if (strategies.Remove(strategy))
            {
                Debug.Log($"[AI] Removed strategy: {strategy.StrategyName}");
            }
        }
        
        public void ClearStrategies()
        {
            strategies.Clear();
            Debug.Log("[AI] Cleared all strategies");
        }
        
        public IReadOnlyList<EnemyStrategy> GetStrategies()
        {
            return strategies.AsReadOnly();
        }
        
        #if UNITY_EDITOR
        [ContextMenu("Test AI Execution")]
        private void TestExecution()
        {
            if (Application.isPlaying)
            {
                ExecuteAI();
            }
            else
            {
                Debug.LogWarning("Can only test in Play Mode!");
            }
        }
        #endif
    }
}