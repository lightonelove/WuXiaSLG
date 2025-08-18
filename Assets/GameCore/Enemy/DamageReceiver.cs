using UnityEngine;
using UnityEngine.Events;

namespace Wuxia.GameCore
{
    /// <summary>
    /// 傷害接收器。它的唯一職責是偵測來自 DamageDealer 的碰撞/觸發，
    /// 然後通知 Health 元件去處理傷害。
    /// 現在包含格擋系統整合。
    /// </summary>
    public class DamageReceiver : MonoBehaviour
    {
        // 對 Health 元件的引用
        public Health healthComponent;
        
        [Header("格擋設定")]
        [Tooltip("是否啟用格擋功能")]
        [SerializeField] private bool enableBlocking = true;
        
        [Tooltip("是否只有玩家可以格擋")]
        [SerializeField] private bool playerOnlyBlocking = true;
        
        [System.Serializable]
        public class DamageEvent : UnityEvent<float> { }
        public DamageEvent onDamaged;
        
        [System.Serializable]
        public class BlockEvent : UnityEvent { }
        [Header("格擋事件")]
        public BlockEvent onDamageBlocked;
        
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
                
                // 獲取原始傷害值
                float originalDamage = dealer.GetDamage();
                float finalDamage = originalDamage;
                
                // 檢查格擋
                if (enableBlocking && CanBlock())
                {
                    if (BlockingSystem.Instance != null && BlockingSystem.Instance.CheckBlockWindow(Time.time))
                    {
                        // 格擋成功，傷害變為 0
                        finalDamage = 0f;
                        onDamageBlocked?.Invoke();
                        Debug.Log($"[DamageReceiver] {gameObject.name} 成功格擋了 {originalDamage} 點傷害！");
                    }
                }
                
                // 處理傷害
                if (healthComponent != null)
                {
                    if (finalDamage > 0f)
                    {
                        healthComponent.TakeDamage(finalDamage);
                        onDamaged?.Invoke(finalDamage);
                        Debug.Log($"[DamageReceiver] {gameObject.name} 受到 {finalDamage} 點傷害");
                    }
                    else
                    {
                        Debug.Log($"[DamageReceiver] {gameObject.name} 傷害被完全格擋");
                    }
                }
                else
                {
                    Debug.LogWarning($"[DamageReceiver] {gameObject.name} 沒有設定 Health 組件");
                }
            }
        }
        
        /// <summary>
        /// 檢查是否可以進行格擋
        /// </summary>
        /// <returns>是否可以格擋</returns>
        private bool CanBlock()
        {
            // 如果設定為只有玩家可以格擋
            if (playerOnlyBlocking)
            {
                // 檢查是否為玩家陣營
                return ownerEntity != null && ownerEntity.Faction == CombatEntityFaction.Ally;
            }
            
            // 所有實體都可以格擋（如果有 BlockingSystem）
            return true;
        }
    }
}