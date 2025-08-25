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
                if (enableBlocking)
                {
                    bool isBlocked = false;
                    
                    // 玩家使用 BlockingSystem
                    if (ownerEntity != null && ownerEntity.Faction == CombatEntityFaction.Ally)
                    {
                        if (BlockingSystem.Instance != null && BlockingSystem.Instance.CheckBlockWindow(Time.time))
                        {
                            isBlocked = true;
                        }
                    }
                    // 敵人使用機率格擋
                    else if (ownerEntity != null && ownerEntity.Faction == CombatEntityFaction.Hostile)
                    {
                        isBlocked = CheckEnemyAutoBlock();
                    }
                    
                    if (isBlocked)
                    {
                        // 格擋成功，傷害變為 0
                        finalDamage = 0f;
                        onDamageBlocked?.Invoke();
                        Debug.Log($"[DamageReceiver] {gameObject.name} 成功格擋了 {originalDamage} 點傷害！");
                        
                        // 嘗試反彈投射物
                        TryReflectProjectile(dealer);
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
        /// 檢查敵人是否自動格擋
        /// </summary>
        /// <returns>是否格擋成功</returns>
        private bool CheckEnemyAutoBlock()
        {
            if (ownerEntity == null || ownerEntity.entityStats == null)
            {
                Debug.LogError($"[DamageReceiver] CheckEnemyAutoBlock: ownerEntity 或 entityStats 為 null");
                return false;
            }
            
            // 根據 BlockRate 機率決定是否格擋
            float blockRate = ownerEntity.entityStats.BlockRate;
            float randomValue = Random.Range(0f, 100f);
            bool willBlock = randomValue < blockRate;
            
            if (willBlock)
            {
                Debug.Log($"[DamageReceiver] {ownerEntity.Name} 觸發自動格擋 (機率: {blockRate:F1}%, 擲骰: {randomValue:F1})");
            }
            
            return willBlock;
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
            
            // 敵人的自動格擋機制
            if (ownerEntity != null && ownerEntity.Faction == CombatEntityFaction.Hostile)
            {
                // 檢查是否有屬性資料
                if (ownerEntity.entityStats == null)
                {
                    Debug.LogError($"[DamageReceiver] {ownerEntity.Name} 沒有設定 CombatEntityStats");
                    return false;
                }
                
                // 根據 BlockRate 機率決定是否格擋
                float blockRate = ownerEntity.entityStats.BlockRate;
                float randomValue = Random.Range(0f, 100f);
                bool willBlock = randomValue < blockRate;
                
                if (willBlock)
                {
                    Debug.Log($"[DamageReceiver] {ownerEntity.Name} 觸發自動格擋 (機率: {blockRate:F1}%, 擲骰: {randomValue:F1})");
                }
                
                return willBlock;
            }
            
            // 其他實體都可以格擋（如果有 BlockingSystem）
            return true;
        }
        
        /// <summary>
        /// 嘗試反彈投射物
        /// </summary>
        /// <param name="damageDealer">造成傷害的 DamageDealer</param>
        private void TryReflectProjectile(DamageDealer damageDealer)
        {
            if (damageDealer == null || ownerEntity == null)
            {
                Debug.LogWarning($"[DamageReceiver] TryReflectProjectile: damageDealer 或 ownerEntity 為 null");
                return;
            }
            
            // 往上查找 Projectile 元件（在 DamageDealer 的父物件中）
            Projectile projectile = damageDealer.GetComponentInParent<Projectile>();
            
            if (projectile == null)
            {
                Debug.Log($"[DamageReceiver] {damageDealer.gameObject.name} 不是投射物，無法反彈");
                return;
            }
            
            // 檢查投射物是否可以被反彈
            if (!projectile.CanBeReflected())
            {
                Debug.Log($"[DamageReceiver] 投射物 {projectile.gameObject.name} 無法被反彈");
                return;
            }
            
            // 執行反彈（讓 Projectile 自動計算方向）
            projectile.ReflectProjectile(ownerEntity);
            
            Debug.Log($"[DamageReceiver] {ownerEntity.Name} 成功反彈了投射物 {projectile.gameObject.name}！");
        }
        
    }
}