using System;
using UnityEngine;
using System.Collections;
using Unity.Entities.Content; // 如果需要使用協程 (Coroutines)

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
    Attacked     // 在Preview中被攻擊到的狀態
}

public class EnemyCore : MonoBehaviour
{
    [Header("敵人基本數值")]
    [Tooltip("敵人的最大血量")]
    [SerializeField] private float maxHealth = 100f;
    
    [Tooltip("敵人的當前血量")]
    private float currentHealth;

    [Tooltip("敵人的攻擊力")]
    [SerializeField] private float attackPower = 15f;

    [Header("行動與狀態 (用於回合制)")]
    [Tooltip("每回合可用的最大行動點數 (AP)")]
    [SerializeField] private int maxActionPoints = 40;
    
    private int currentActionPoints;

    [Header("目前狀態")]
    [Tooltip("顯示敵人目前的狀態，主要用於偵錯")]
    [SerializeField] private EnemyState currentState = EnemyState.Idle;

    public DamageReceiver damageReceiver;

    // C# 屬性 (Property)，方便外部程式碼安全地讀取數值
    public Health health;

    public Animator animator;

    public int CurrentActionPoints => currentActionPoints;
    public EnemyState CurrentState => currentState;


    /// <summary>
    /// Awake 是在物件被建立時立刻呼叫的函數，早於 Start
    /// 通常用於初始化變數
    /// </summary>
    void Awake()
    {
        // 遊戲開始時，將當前血量設為最大血量
        currentHealth = maxHealth;
        // 回合開始時，恢復所有行動點數
        RestoreActionPoints();
    }

    private void Start()
    {
        damageReceiver.onDamaged.AddListener(ToHurt);
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
    public void TakeDamage(float damage)
    {
        // 如果已經死亡，就不要再受傷了
        if (currentState == EnemyState.Dead) return;

        currentHealth -= damage;
        Debug.Log(gameObject.name + " 受到 " + damage + " 點傷害，剩餘血量: " + currentHealth);
        
        // 檢查血量是否歸零
        if (currentHealth <= 0)
        {
            currentHealth = 0; // 避免血量變負數
            Die();
        }
    }
    
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
    public bool SpendActionPoints(int cost)
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