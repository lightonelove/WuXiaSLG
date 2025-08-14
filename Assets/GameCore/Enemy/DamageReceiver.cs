using UnityEngine;
using UnityEngine.Events;

namespace Wuxia.GameCore
{
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
        public CombatEntity ownerEntity;
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
                if (!dealer.CanDamageTarget(ownerEntity))
                {
                    Debug.Log($"[DamageReceiver] {other.transform.parent.name} 的陣營檢查失敗，不接受傷害");
                    return;
                }
                
                // 自己不處理傷害邏輯，而是呼叫 Health 元件的 TakeDamage 方法
                // 將傷害處理的權力交出去
                float damageAmount = dealer.GetDamage();
                if (healthComponent != null)
                {
                    healthComponent.TakeDamage(damageAmount);
                    onDamaged?.Invoke(damageAmount);
                    Debug.Log($"[DamageReceiver] {gameObject.name} 受到 {damageAmount} 點傷害");
                }
                else
                {
                    Debug.LogWarning($"[DamageReceiver] {gameObject.name} 沒有設定 Health 組件");
                }
            }
        }
    }
}