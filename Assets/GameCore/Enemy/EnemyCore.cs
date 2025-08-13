using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities.Content; // 如果需要使用協程 (Coroutines)
using UnityEngine.AI;

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
        [Tooltip("每回合可用的最大行動點數 (AP)")]
        [SerializeField] public float maxActionPoints = 60f;
        
        public float currentActionPoints;
    
        [Header("目前狀態")]
        [Tooltip("顯示敵人目前的狀態，主要用於偵錯")]
        [SerializeField] private EnemyState currentState = EnemyState.Idle;
    
        [Header("移動相關")]
        public CharacterController characterController;
        public float moveSpeed = 8f;
        public float turnRate = 360f;
        public float stoppingDistance = 2f; // 停止追蹤的距離
        public float apCostPerMeter = 5f; // 每公尺消耗的AP
        
        [Header("AI尋路")]
        public NavMeshAgent navAgent;
        public bool useNavMesh = true; // 是否使用NavMesh尋路
        
        [Header("目標追蹤")]
        public Transform currentTarget; // 當前追蹤的目標
        private Vector2 lastPosition;
        
        public DamageReceiver damageReceiver;
    
        // C# 屬性 (Property)，方便外部程式碼安全地讀取數值
        public Health health;
    
        public Animator animator;
    
        public float CurrentActionPoints => currentActionPoints;
        public float MaxActionPoints => maxActionPoints;
        public EnemyState CurrentState => currentState;
    
    
        /// <summary>
        /// Awake 是在物件被建立時立刻呼叫的函數，早於 Start
        /// 通常用於初始化變數
        /// </summary>
        void Awake()
        {
            // 遊戲開始時，將當前血量設為最大血量
            // 回合開始時，恢復所有行動點數
            RestoreActionPoints();
            
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
            
            lastPosition = new Vector2(transform.position.x, transform.position.z);
        }
    
        /// <summary>
        /// Update 是每一幀 (Frame) 都會被呼叫的函數
        /// 我們可以在這裡根據不同狀態執行不同邏輯
        /// </summary>
        void Update()
        {
            // 這是一個簡單的狀態機 (State Machine)
            switch (currentState)
            {
                case EnemyState.Idle:
                    IdleLogic();
                    break;
    
                case EnemyState.Moving:
                    MovingLogic();
                    break;
    
                case EnemyState.Attacking:
                    AttackingLogic();
                    break;
    
                case EnemyState.Dead:
                    // 如果死亡，就不做任何事
                    break;
            }
        }
    
        // --- 狀態邏輯 ---
    
        private void IdleLogic()
        {
            // 在待機狀態下，敵人可能會：
            // 1. 偵測玩家是否進入範圍
            // 2. 如果是回合制，等待輪到自己的回合
            // Debug.Log(gameObject.name + " 正在待機...");
        }
    
        private void MovingLogic()
        {
            // 在移動狀態下，敵人會：
            // 1. 朝目標位置移動
            // 2. 到達後切換回待機狀態或攻擊狀態
            // 這個邏輯通常會與 NavMeshAgent (導航系統) 配合
            // Debug.Log(gameObject.name + " 正在移動...");
        }
    
        private void AttackingLogic()
        {
            // 在攻擊狀態下，敵人會：
            // 1. 播放攻擊動畫
            // 2. 在特定時間點對玩家造成傷害
            // 3. 攻擊結束後切換回待機狀態
            // Debug.Log(gameObject.name + " 正在攻擊!");
        }
    
    
        // --- 公開方法 (Public Methods) ---
        // 這些方法可以被其他腳本（例如：玩家的攻擊腳本、遊戲管理器）呼叫
    
        /// <summary>
        /// 讓敵人受到傷害
        /// </summary>
        /// <param name="damage">受到的傷害數值</param>
        
        private void OnTriggerEnter(Collider other)
        {
            // 檢查進入的物件標籤 (Tag) 是否為 "PlayerWeapon"
            // 這是為了確保只有玩家的武器能造成傷害，而不是玩家的身體或其他東西
            if (other.CompareTag("PlayerWeapon"))
            {
                Debug.Log(gameObject.name + " 被 " + other.name + " 擊中了!", this.gameObject);
    
                // 嘗試從擊中我們的物件上獲取 Weapon 元件
                //Weapon weapon = other.GetComponent<Weapon>();
    
                // 如果成功獲取到 Weapon 元件 (代表這個攻擊是有效的)
                /*
                if (weapon != null)
                {
                    // 呼叫自己的 TakeDamage 方法，並傳入武器的傷害值
                    TakeDamage(weapon.damage);
                }
                */
            }
        }
        
        /// <summary>
        /// 消耗AP來移動 (用於回合制)
        /// </summary>
        /// <param name="cost">移動需要消耗的AP</param>
        /// <returns>如果AP足夠則回傳true，否則回傳false</returns>
        public bool SpendActionPoints(float cost)
        {
            if (currentActionPoints >= cost)
            {
                currentActionPoints -= cost;
                Debug.Log(gameObject.name + " 消耗了 " + cost + " AP, 剩餘AP: " + currentActionPoints);
                return true;
            }
            else
            {
                Debug.Log(gameObject.name + " AP不足，無法行動!");
                return false;
            }
        }
    
        /// <summary>
        /// 進入戰鬥Preview狀態
        /// </summary>
        public void ToPreview()
        {
            if (currentState == EnemyState.Attacked)
                return;
            SetState(EnemyState.Attacked);
            animator.Play("Attacked_Preview");
        }
    
        /// <summary>
        /// 在新回合開始時恢復所有AP
        /// </summary>
        public void RestoreActionPoints()
        {
            currentActionPoints = maxActionPoints;
        }
    
        /// <summary>
        /// 切換敵人狀態
        /// </summary>
        /// <param name="newState">要切換到的新狀態</param>
        public void SetState(EnemyState newState)
        {
            if (currentState == newState) return;
    
            currentState = newState;
            Debug.Log(gameObject.name + " 狀態切換為: " + newState);
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
        
        
        // --- 回合制移動方法 ---
        
        /// <summary>
        /// 開始敵人的回合
        /// </summary>
        public void StartTurn()
        {
            Debug.Log($"{gameObject.name} StartTurn called, current state: {currentState}");
            
            SetState(EnemyState.ExecutingTurn);
            RestoreActionPoints();
            
            // 尋找最近的玩家作為目標
            FindClosestPlayer();
            
            // 如果使用NavMesh，啟用NavMeshAgent並設定目標
            if (useNavMesh && navAgent != null && currentTarget != null)
            {
                navAgent.enabled = true;
                navAgent.speed = 0; // 先設為0，透過程式控制移動
                navAgent.SetDestination(currentTarget.position);
            }
            
            Debug.Log($"{gameObject.name} 開始回合，AP: {currentActionPoints}, Target: {(currentTarget ? currentTarget.name : "None")}");
        }
        
        /// <summary>
        /// 執行敵人回合的AI邏輯
        /// </summary>
        public IEnumerator ExecuteTurn()
        {
            Debug.Log($"{gameObject.name} ExecuteTurn started, state: {currentState}, target: {(currentTarget ? currentTarget.name : "None")}, AP: {currentActionPoints}");
            
            if (currentState != EnemyState.ExecutingTurn) 
            {
                Debug.Log($"{gameObject.name} ExecuteTurn 退出: 狀態不正確 ({currentState})");
                yield break;
            }
            
            float turnStartTime = Time.time;
            float maxTurnDuration = 5f; // 最長回合時間
            
            // 如果有目標，開始追蹤
            while (currentTarget != null && currentActionPoints > 0 && 
                   (Time.time - turnStartTime) < maxTurnDuration)
            {
                // 每幀重新尋找最近的玩家，以防玩家在執行狀態中移動
                FindClosestPlayer();
                
                if (currentTarget == null) break;
                
                float distanceToTarget = Vector3.Distance(transform.position, currentTarget.position);
                
                // 如果已經足夠接近目標，結束移動
                if (distanceToTarget <= stoppingDistance)
                {
                    Debug.Log($"{gameObject.name} 已接近目標，停止移動 (距離: {distanceToTarget})");
                    break;
                }
                
                // 如果使用NavMesh，更新目標位置
                if (useNavMesh && navAgent != null && navAgent.enabled)
                {
                    navAgent.SetDestination(currentTarget.position);
                }
                
                // 移動向目標
                bool moved = MoveTowardsTarget();
                
                if (!moved)
                {
                    Debug.Log($"{gameObject.name} 無法繼續移動，剩餘AP: {currentActionPoints}");
                    break;
                }
                
                yield return null; // 等待下一幀
            }
            
            Debug.Log($"{gameObject.name} ExecuteTurn 完成，原因: Target={currentTarget}, AP={currentActionPoints}, Time={(Time.time - turnStartTime)}");
            
            // 回合結束
            EndTurn();
        }
        
        /// <summary>
        /// 向目標移動
        /// </summary>
        bool MoveTowardsTarget()
        {
            if (currentTarget == null)
            {
                Debug.Log($"{gameObject.name} MoveTowardsTarget: 沒有目標");
                return false;
            }
            
            if (currentActionPoints <= 0)
            {
                Debug.Log($"{gameObject.name} MoveTowardsTarget: AP不足 ({currentActionPoints})");
                return false;
            }
            
            if (useNavMesh && navAgent != null && navAgent.enabled)
            {
                // 使用NavMeshAgent移動
                //Debug.Log($"{gameObject.name} 使用NavMesh移動");
                return MoveWithNavMesh();
            }
            else
            {
                // 使用直接移動
                //Debug.Log($"{gameObject.name} 使用直接移動");
                return MoveDirectly();
            }
        }
        
        /// <summary>
        /// 使用NavMesh移動
        /// </summary>
        bool MoveWithNavMesh()
        {
            if (!navAgent.pathPending && navAgent.remainingDistance > 0.1f)
            {
                // 計算這一幀要移動的距離
                float frameDistance = moveSpeed * Time.deltaTime;
                float apCost = frameDistance * apCostPerMeter;
                Debug.Log("有進來");
                // 檢查AP是否足夠
                if (currentActionPoints < apCost)
                {
                    frameDistance = currentActionPoints / apCostPerMeter;
                    apCost = currentActionPoints;
                }
                
                if (frameDistance > 0)
                {
                    // 設定NavMeshAgent的速度來控制移動
                    navAgent.speed = frameDistance / Time.deltaTime;
                    
                    // 消耗AP
                    currentActionPoints -= apCost;
                    
                    if (animator != null)
                    {
                        animator.SetBool("isMoving", true);
                    }
                    
                    return true;
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// 直接移動（不使用NavMesh）
        /// </summary>
        bool MoveDirectly()
        {
            Vector3 direction = (currentTarget.position - transform.position).normalized;
            direction.y = 0; // 保持在水平面上
            
            // 轉向目標
            if (direction.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnRate * Time.deltaTime / 360f);
                
                if (animator != null)
                {
                    animator.SetBool("isMoving", true);
                }
            }
            
            // 計算這一幀的移動距離
            float moveDistance = moveSpeed * Time.deltaTime;
            Vector3 moveVector = direction * moveDistance;
            
            // 計算AP消耗
            float apCost = moveDistance * apCostPerMeter;
            
            // 檢查AP是否足夠
            if (currentActionPoints < apCost)
            {
                // 用剩餘的AP移動部分距離
                moveDistance = currentActionPoints / apCostPerMeter;
                moveVector = direction * moveDistance;
                apCost = currentActionPoints;
            }
            
            // 使用CharacterController移動
            if (characterController != null && characterController.enabled)
            {
                characterController.Move(moveVector);
            }
            else
            {
                // 直接移動
                transform.position += moveVector;
            }
            
            // 消耗AP
            currentActionPoints -= apCost;
            
            return true;
        }
        
        /// <summary>
        /// 尋找最近的玩家
        /// </summary>
        void FindClosestPlayer()
        {
            float closestDistance = float.MaxValue;
            CharacterCore closestCharacter = null;
            
            // 從CombatCore獲取所有玩家角色
            if (CombatCore.Instance != null)
            {
                foreach (var character in CombatCore.Instance.AllCharacters)
                {
                    if (character != null && character.gameObject.activeInHierarchy)
                    {
                        // 使用角色的真實位置計算距離
                        Vector3 playerRealPos = character.GetRealPosition();
                        float distance = Vector3.Distance(transform.position, playerRealPos);
                        
                        if (distance < closestDistance)
                        {
                            closestDistance = distance;
                            closestCharacter = character;
                        }
                    }
                }
            }
            
            // 設定目標為最近角色的真實 Transform
            if (closestCharacter != null)
            {
                currentTarget = closestCharacter.GetRealTransform();
                Debug.Log($"{gameObject.name} 鎖定目標: {closestCharacter.name} (真實位置: {closestCharacter.GetRealPosition()})");
            }
            else
            {
                currentTarget = null;
                Debug.Log($"{gameObject.name} 沒有找到目標");
            }
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
            Debug.Log($"{gameObject.name} 回合結束，剩餘AP: {currentActionPoints}");
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