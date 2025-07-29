using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 傷害接收器。它的唯一職責是偵測來自 DamageDealer 的碰撞/觸發，
/// 然後通知 Health 元件去處理傷害。
/// </summary>
public class DamageReceiver : MonoBehaviour
{
    // 對 Health 元件的引用
    public Health healthComponent;
    [System.Serializable]
    public class DamageEvent : UnityEvent<float> { }
    public DamageEvent onDamaged;
    private void Awake()
    {
        // 在初始時，自動獲取掛在同一個物件上的 Health 元件
    }

    private void OnTriggerEnter(Collider other)
    {
        // 嘗試從碰到的物件上獲取 DamageDealer
        DamageDealer dealer = other.GetComponent<DamageDealer>();

        // 如果對方是個 DamageDealer
        if (dealer != null)
        {
            // 自己不處理傷害邏輯，而是呼叫 Health 元件的 TakeDamage 方法
            // 將傷害處理的權力交出去
            float damageAmount = dealer.GetDamage();
            healthComponent.TakeDamage(dealer.GetDamage());
            onDamaged?.Invoke(damageAmount);
        }
    }
    
    
}