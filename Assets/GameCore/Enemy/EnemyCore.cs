using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities.Content; // 如果需要使用協程 (Coroutines)
using UnityEngine.AI;
using UnityEngine.Events;

namespace Wuxia.GameCore
{
    /// <summary>
    /// 定義敵人的所有可能狀態
    /// </summary>
    public enum EnemyState
    {
        Idle,       // 待機狀態
        Moving,     // 移動中
        Attacking,  // 攻擊中
        Dead,        // 死亡狀態
        Hurt,          //受傷狀態
        Attacked,     // 在Preview中被攻擊到的狀態
        WaitingTurn,  // 等待回合
        ExecutingTurn, // 執行回合中
        TurnComplete  // 回合完成
    }

    public class EnemyCore : MonoBehaviour
    {
        [Header("敵人基本數值")]
    
        [Tooltip("敵人的攻擊力")]
        [SerializeField] public float attackPower = 15f;
        [SerializeField] public float speed = 10f;
    
        [Header("行動與狀態 (用於回合制)")]
    
        [Header("目前狀態")]
        [Tooltip("顯示敵人目前的狀態，主要用於偵錯")]
        [SerializeField] private EnemyState currentState = EnemyState.Idle;
    
        [Header("移動相關")]
        public CharacterController characterController;
        public float moveSpeed = 8f;
        public float turnRate = 360f;
        public float stoppingDistance = 2f; // 停止追蹤的距離
        public float apCostPerMeter = 5f; // 每公尺消耗的AP
        
        [Header("AI相關")]
        public NavMeshAgent navAgent;
        public bool useNavMesh = true; // 是否使用NavMesh尋路
        public EnemyAISystem enemyAISystem;
        
        public DamageReceiver damageReceiver;
        
        [Header("狀態事件")]
        [HideInInspector] public UnityEvent onAttackStateEnter = new UnityEvent();
        [HideInInspector] public UnityEvent onAttackStateExit = new UnityEvent();
        
        public CombatEntity combatEntity;
    
        // C# 屬性 (Property)，方便外部程式碼安全地讀取數值
        private Health health;
    
        public Animator animator;
        public EnemyState CurrentState => currentState;
    
    
        /// <summary>
        /// Awake 是在物件被建立時立刻呼叫的函數，早於 Start
        /// 通常用於初始化變數
        /// </summary>
        void Awake()
        {
            // 遊戲開始時，將當前血量設為最大血量
            health = combatEntity.health;
            // 初始化元件
            if (characterController == null)
                characterController = GetComponent<CharacterController>();
            if (navAgent == null)
                navAgent = GetComponent<NavMeshAgent>();
                
            // 如果使用NavMesh，初始設定
            if (navAgent != null && useNavMesh)
            {
                navAgent.speed = moveSpeed;
                navAgent.angularSpeed = turnRate;
                navAgent.stoppingDistance = stoppingDistance;
                navAgent.enabled = false; // 初始時關閉，回合開始時才啟用
            }
        }
    
        private void Start()
        {
            damageReceiver.onDamaged.AddListener(ToHurt);
            
            // 訂閱格擋事件
            if (damageReceiver.onDamageBlocked != null)
            {
                damageReceiver.onDamageBlocked.AddListener(PlayParryAnimation);
            }
            
            // 訂閱架勢破壞事件
            if (combatEntity.PosturePoint != null && combatEntity.PosturePoint.OnPostureBroken != null)
            {
                combatEntity.PosturePoint.OnPostureBroken.AddListener(PlayPostureBreakAnimation);
            }
        }

        // --- 公開方法 (Public Methods) ---
        // 這些方法可以被其他腳本（例如：玩家的攻擊腳本、遊戲管理器）呼叫
        
        /// <summary>
        /// 消耗AP來移動 (用於回合制)
        /// </summary>
        /// <param name="cost">移動需要消耗的AP</param>
        /// <returns>如果AP足夠則回傳true，否則回傳false</returns>
        public bool SpendActionPoints(float cost)
        {
            if (combatEntity.ActionPoint == null)
            {
                Debug.LogError($"[EnemyCore] {gameObject.name} 沒有 ActionPoint 組件");
                return false;
            }
            
            if (combatEntity.ActionPoint.AP >= cost)
            {
                combatEntity.ActionPoint.ConsumeAP(cost);
                return true;
            }
            else
            {
                return false;
            }
        }
        /// <summary>
        /// 在新回合開始時恢復所有AP
        /// </summary>
        public void RestoreActionPoints()
        {
            if (combatEntity.ActionPoint == null)
            {
                Debug.LogError($"[EnemyCore] {gameObject.name} 沒有 ActionPoint 組件");
                return;
            }
            
            combatEntity.ActionPoint.RefillAP();
        }
    
        /// <summary>
        /// 切換敵人狀態
        /// </summary>
        /// <param name="newState">要切換到的新狀態</param>
        public void SetState(EnemyState newState)
        {
            if (currentState == newState) return;
            
            // 觸發舊狀態的退出事件
            if (currentState == EnemyState.Attacking)
            {
                onAttackStateExit?.Invoke();
                Debug.Log($"{gameObject.name} 退出攻擊狀態");
            }
    
            EnemyState oldState = currentState;
            currentState = newState;
            Debug.Log(gameObject.name + " 狀態切換為: " + newState);
            
            // 觸發新狀態的進入事件
            if (newState == EnemyState.Attacking)
            {
                onAttackStateEnter?.Invoke();
                Debug.Log($"{gameObject.name} 進入攻擊狀態");
            }
        }
    
        public void ToHurt(float amount)
        {
            Debug.Log("Hurted");
            currentState = EnemyState.Hurt;
            animator.Play("Hurt");
            animator.playbackTime = 0;
        }
    
        public void ReturnFromPreview()
        {
            SetState(EnemyState.Idle);
            animator.Play("Idle");
        }
        
        /// <summary>
        /// 播放格擋動畫（無參數版本，保持向後兼容）
        /// </summary>
        public void PlayParryAnimation()
        {
            PlayParryAnimation(null);
        }
        
        /// <summary>
        /// 播放格擋動畫（帶攻擊者資訊，會自動轉向攻擊者）
        /// </summary>
        /// <param name="attacker">攻擊者的 DamageDealer</param>
        public void PlayParryAnimation(DamageDealer attacker)
        {
            if (animator == null)
            {
                Debug.LogError($"[EnemyCore] {gameObject.name} 沒有 Animator 組件，無法播放格擋動畫");
                return;
            }
            
            // 轉向攻擊者
            FaceAttacker(attacker);
            
            Debug.Log($"[EnemyCore] {gameObject.name} 播放格擋動畫並面對攻擊者");
            animator.Play("Parry");
            animator.playbackTime = 0;
        }
        
        /// <summary>
        /// 讓角色面對攻擊者
        /// </summary>
        /// <param name="attacker">攻擊者的 DamageDealer</param>
        private void FaceAttacker(DamageDealer attacker)
        {
            if (attacker == null)
            {
                Debug.LogWarning($"[EnemyCore] {gameObject.name} 無法面對攻擊者：攻擊者為 null");
                return;
            }
            
            // 獲取攻擊者位置
            Vector3 attackerPosition = Vector3.zero;
            
            // 嘗試從 DamageDealer 的 sourceCombatEntity 獲取位置
            if (attacker.sourceCombatEntity != null)
            {
                attackerPosition = attacker.sourceCombatEntity.transform.position;
                Debug.Log($"[EnemyCore] {gameObject.name} 使用攻擊者戰鬥實體位置: {attacker.sourceCombatEntity.Name}");
            }
            else
            {
                // 如果沒有 sourceCombatEntity，使用 DamageDealer 本身的位置
                attackerPosition = attacker.transform.position;
                Debug.Log($"[EnemyCore] {gameObject.name} 使用 DamageDealer 位置");
            }
            
            // 計算面對方向（只考慮 X-Z 平面，忽略 Y 軸差異）
            Vector3 direction = attackerPosition - transform.position;
            direction.y = 0; // 忽略垂直方向差異
            
            if (direction.magnitude > 0.1f) // 避免距離太近造成的計算問題
            {
                // 平滑轉向攻擊者
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 1.0f);
                
                Debug.Log($"[EnemyCore] {gameObject.name} 轉向攻擊者，方向: {direction}");
            }
            else
            {
                Debug.LogWarning($"[EnemyCore] {gameObject.name} 與攻擊者距離太近，無法計算轉向");
            }
        }
        
        /// <summary>
        /// 播放架勢破壞動畫
        /// </summary>
        public void PlayPostureBreakAnimation()
        {
            if (animator == null)
            {
                Debug.LogError($"[EnemyCore] {gameObject.name} 沒有 Animator 組件，無法播放架勢破壞動畫");
                return;
            }
            
            Debug.Log($"[EnemyCore] {gameObject.name} 播放架勢破壞動畫");
            animator.Play("PostureBreak");
            animator.playbackTime = 0;
        }
        
        
        // --- 回合制移動方法 ---
        
        /// <summary>
        /// 開始敵人的回合
        /// </summary>
        public void StartTurn()
        {
            Debug.Log($"{gameObject.name} StartTurn called, current state: {currentState}");
            
            SetState(EnemyState.ExecutingTurn);
            RestoreActionPoints();
            
            Debug.Log($"{gameObject.name} 開始回合，AP: {combatEntity.ActionPoint.AP}");
        }
        
        /// <summary>
        /// 執行敵人回合的AI邏輯 - 使用策略系統
        /// </summary>
        public IEnumerator ExecuteTurn()
        {
            Debug.Log($"{gameObject.name} ExecuteTurn started, state: {currentState}, AP: {combatEntity.ActionPoint?.AP}");
            
            if (currentState != EnemyState.ExecutingTurn) 
            {
                Debug.Log($"{gameObject.name} ExecuteTurn 退出: 狀態不正確 ({currentState})");
                yield break;
            }
            
            // 檢查是否有 AI 系統
            if (enemyAISystem == null)
            {
                Debug.LogError($"{gameObject.name} 沒有 AI 系統，無法執行回合");
                EndTurn();
                yield break;
            }
            
            // 選擇合適的策略
            EnemyStrategy selectedStrategy = enemyAISystem.SelectStrategy();
            if (selectedStrategy == null)
            {
                Debug.LogWarning($"{gameObject.name} 沒有找到合適的策略，結束回合");
                EndTurn();
                yield break;
            }
            
            Debug.Log($"{gameObject.name} 選擇策略: {selectedStrategy.StrategyName}");
            
            // 按順序執行 Action 直到完成或無法繼續執行
            foreach (var action in selectedStrategy.Actions)
            {
                if (action != null)
                {
                    action.InitializeAction(this);
                }
                else
                {
                    break;
                }
                if (action.CanExecute(this))
                {
                    Debug.Log($"{gameObject.name} 執行行動: {action.GetActionName()}");
                    yield return action.Execute(this);
                    
                    // 檢查是否還有 AP 繼續執行其他行動
                    if (combatEntity.ActionPoint.AP <= 0)
                    {
                        Debug.Log($"{gameObject.name} AP 耗盡，結束回合");
                        break;
                    }
                }
                else if (action != null)
                {
                    Debug.Log($"{gameObject.name} 跳過行動: {action.GetActionName()} (條件不滿足)");
                }
            }
            
            Debug.Log($"{gameObject.name} 策略執行完畢，剩餘 AP: {combatEntity.ActionPoint.AP}");
            
            // 結束回合
            EndTurn();
        }
        
        
        /// <summary>
        /// 結束回合
        /// </summary>
        public void EndTurn()
        {
            if (animator != null)
            {
                animator.SetBool("isMoving", false);
            }
            
            // 如果使用NavMesh，停止移動並關閉NavMeshAgent
            if (useNavMesh && navAgent != null && navAgent.enabled)
            {
                navAgent.ResetPath();
                navAgent.enabled = false;
            }
            
            SetState(EnemyState.TurnComplete);
            Debug.Log($"{gameObject.name} 回合結束，剩餘AP: {combatEntity.ActionPoint.AP}");
        }
        
        // --- 私有方法 (Private Methods) ---
    
        /// <summary>
        /// 處理死亡邏輯
        /// </summary>
        private void Die()
        {
            SetState(EnemyState.Dead);
            Debug.Log(gameObject.name + " 已被擊敗!");
    
            // 在這裡可以加入：
            // 1. 播放死亡動畫
            // 2. 掉落物品
            // 3. 幾秒後摧毀這個遊戲物件
            Destroy(gameObject, 3f); // 3秒後從場景中移除
        }
    }
}