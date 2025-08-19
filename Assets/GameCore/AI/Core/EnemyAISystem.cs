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
        private CombatEntity enemyCombatEntity; // 快取敵人的 CombatEntity
        private EnemyStrategy currentStrategy;
        private Coroutine currentExecutionCoroutine;
        
        public EnemyStrategy CurrentStrategy => currentStrategy;
        public bool IsExecuting => currentExecutionCoroutine != null;
        
        private void Awake()
        {
            InitializeEnemyReferences();
        }
        
        private void Start()
        {
            // 再次確保初始化（以防某些元件在 Awake 時還沒準備好）
            if (enemy == null || enemyCombatEntity == null)
            {
                InitializeEnemyReferences();
            }
        }
        
        /// <summary>
        /// 初始化敵人相關的參考
        /// </summary>
        private void InitializeEnemyReferences()
        {
            // 如果 enemy 還沒設定，往上找最近的 EnemyCore
            if (enemy == null)
            {
                Transform current = transform;
                while (current != null)
                {
                    EnemyCore foundEnemy = current.GetComponent<EnemyCore>();
                    if (foundEnemy != null)
                    {
                        enemy = foundEnemy;
                        break;
                    }
                    current = current.parent;
                }
                
                if (enemy == null)
                {
                    return;
                }
            }
            
            // 取得 CombatEntity
            enemyCombatEntity = enemy.GetComponent<CombatEntity>();
            if (enemyCombatEntity == null)
            {
                return;
            }
            
            // 將 CombatEntity 設定到所有的 EnemyAction 中
            SetCombatEntityToAllActions();
        }
        
        /// <summary>
        /// 計算兩個 Transform 之間的層級距離
        /// </summary>
        private int GetHierarchyDistance(Transform from, Transform to)
        {
            int distance = 0;
            Transform current = from;
            while (current != null && current != to)
            {
                current = current.parent;
                distance++;
            }
            return distance;
        }
        
        /// <summary>
        /// 將 CombatEntity 設定到所有的 EnemyAction 中
        /// </summary>
        private void SetCombatEntityToAllActions()
        {
            // 取得所有的 BaseEnemyAction 元件（包括子物件）
            BaseEnemyAction[] allActions = GetComponentsInChildren<BaseEnemyAction>();
            
            if (allActions != null && allActions.Length > 0)
            {
                foreach (var action in allActions)
                {
                    if (action != null)
                    {
                        action.SetEnemyCombatEntity(enemyCombatEntity);
                    }
                }
            }
            
            // 同時自動抓取所有啟用的 Strategy
            CollectActiveStrategies();
        }
        
        /// <summary>
        /// 自動收集所有啟用的 Strategy
        /// </summary>
        private void CollectActiveStrategies()
        {
            // 清空現有的策略列表
            strategies.Clear();
            
            // 取得所有的 EnemyStrategy 元件（包括子物件）
            EnemyStrategy[] allStrategies = GetComponentsInChildren<EnemyStrategy>();
            
            if (allStrategies != null && allStrategies.Length > 0)
            {
                foreach (var strategy in allStrategies)
                {
                    // 只加入啟用的 Strategy
                    if (strategy != null && strategy.gameObject.activeInHierarchy && strategy.enabled)
                    {
                        strategies.Add(strategy);
                        
                        // 讓每個 Strategy 也自動收集它的 Actions
                        strategy.CollectActiveActions();
                    }
                }
            }
        }
        
        public EnemyStrategy SelectStrategy()
        {
            if (strategies == null || strategies.Count == 0)
            {
                return null;
            }
            
            var availableStrategies = strategies
                .Where(s => s.CanExecute(enemy))
                .OrderByDescending(s => s.Priority)
                .ToList();
            
            if (availableStrategies.Count == 0)
            {
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
            yield return currentStrategy.ExecuteStrategy(enemy);
            
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
            }
        }
        
        public void AddStrategy(EnemyStrategy strategy)
        {
            if (strategy != null && !strategies.Contains(strategy))
            {
                strategies.Add(strategy);
            }
        }
        
        public void RemoveStrategy(EnemyStrategy strategy)
        {
            if (strategies.Remove(strategy))
            {
            }
        }
        
        public void ClearStrategies()
        {
            strategies.Clear();
        }
        
        public IReadOnlyList<EnemyStrategy> GetStrategies()
        {
            return strategies.AsReadOnly();
        }
        
        /// <summary>
        /// 手動重新收集所有啟用的 Strategy 和 Action
        /// </summary>
        public void RefreshStrategiesAndActions()
        {
            CollectActiveStrategies();
        }
        
        #if UNITY_EDITOR
        [ContextMenu("Test AI Execution")]
        private void TestExecution()
        {
            if (Application.isPlaying)
            {
                ExecuteAI();
            }

        }
        #endif
    }
}