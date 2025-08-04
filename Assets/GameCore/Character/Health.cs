using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 獨立的生命值元件。負責管理物件的生命值、受傷和死亡邏輯。
/// 這是物件生命狀態的唯一權威。
/// </summary>
public class Health : MonoBehaviour
{
    [Header("血量設定")]
    [SerializeField]
    [Tooltip("最大血量")]
    private float maxHealth = 100f;
    
    public float currentHealth;

    // 唯讀屬性，供外部腳本（如UI）安全地讀取數值
    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public bool IsDead => currentHealth <= 0;
    
    [Header("事件系統")]
    [Tooltip("當受到傷害時觸發的事件。參數: 造成的傷害值")]
    public UnityEvent<float> OnDamageTaken;

    [Tooltip("當血量變更時觸發的事件。參數: 當前血量, 最大血量")]
    public UnityEvent<float, float> OnHealthChanged;
    
    [Tooltip("當血量歸零時觸發的事件")]
    public UnityEvent OnDie;

    private void Awake()
    {
        // 初始化血量
        currentHealth = maxHealth;
    }

    /// <summary>
    /// 對此物件造成傷害的核心方法。
    /// </summary>
    /// <param name="damageAmount">要扣除的血量</param>
    public void TakeDamage(float damageAmount)
    {
        // 如果已經死亡，直接返回
        if (IsDead)
        {
            return;
        }

        float actualDamage = Mathf.Max(damageAmount, 0); // 確保傷害不是負數
        currentHealth -= actualDamage;

        Debug.Log(gameObject.name + " 的 Health 元件處理了 " + actualDamage + " 點傷害。");

        // 觸發「受到傷害」事件
        OnDamageTaken?.Invoke(actualDamage);
        
        // 觸發「血量變更」事件
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        // 檢查是否死亡
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log(gameObject.name + " 的 Health 元件確認死亡。");
        // 觸發「死亡」事件，讓其他關心此事的系統去處理後續
        // （例如：動畫、掉寶、移除物件等）
        OnDie?.Invoke();
    }

    /// <summary>
    /// 治療物件
    /// </summary>
    public void Heal(float amount)
    {
        if (IsDead) return;

        currentHealth = Mathf.Min(currentHealth + amount, maxHealth); // 確保不會超過最大血量
        OnHealthChanged?.Invoke(currentHealth, maxHealth); // 同樣通知UI更新
    }
    
    /// <summary>
    /// 取得當前血量的方法（與屬性 CurrentHealth 功能相同）
    /// </summary>
    /// <returns>當前血量值</returns>
    public float GetCurrentHealth()
    {
        return currentHealth;
    }
    
    /// <summary>
    /// 取得最大血量的方法（與屬性 MaxHealth 功能相同）
    /// </summary>
    /// <returns>最大血量值</returns>
    public float GetMaxHealth()
    {
        return maxHealth;
    }
    
    /// <summary>
    /// 設定最大血量
    /// </summary>
    /// <param name="newMaxHealth">新的最大血量值</param>
    public void SetMaxHealth(float newMaxHealth)
    {
        maxHealth = newMaxHealth;
        // 如果當前血量超過新的最大血量，調整當前血量
        if (currentHealth > maxHealth)
        {
            currentHealth = maxHealth;
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }
    }
    
    /// <summary>
    /// 重置血量至最大值
    /// </summary>
    public void ResetHealth()
    {
        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }
}