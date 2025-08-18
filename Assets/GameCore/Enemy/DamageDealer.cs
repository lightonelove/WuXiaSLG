using UnityEngine;

namespace Wuxia.GameCore
{
    public class DamageDealer : MonoBehaviour
    {
        [Header("傷害設定")]
        [SerializeField]
        private float damage = 25f;
        
        [Header("陣營設定")]
        [Tooltip("傷害來源的戰鬥實體（用於判斷陣營）")]
        [SerializeField]
        public CombatEntity sourceCombatEntity;
        
        [Tooltip("是否啟用陣營檢查")]
        [SerializeField]
        private bool enableFactionCheck = true;
        
        [Tooltip("是否自動尋找來源的 CombatEntity")]
        [SerializeField]
        private bool autoFindSourceEntity = true;

        void Start()
        {
            if (sourceCombatEntity != null)
            {
                Debug.Log($"[DamageDealer] {gameObject.name} 自動找到來源實體: {sourceCombatEntity.Name} (陣營: {sourceCombatEntity.Faction})");
            }
            else if (enableFactionCheck)
            {
                Debug.LogWarning($"[DamageDealer] {gameObject.name} 啟用了陣營檢查但找不到來源的 CombatEntity");
            }
        }

        /// <summary>
        /// 公開方法，讓外部可以讀取傷害值
        /// </summary>
        public float GetDamage()
        {
            return damage;
        }
        
        
        /// <summary>
        /// 啟用或關閉陣營檢查
        /// </summary>
        /// <param name="enabled">是否啟用陣營檢查</param>
        public void SetFactionCheckEnabled(bool enabled)
        {
            enableFactionCheck = enabled;
        }
        
        /// <summary>
        /// 獲取來源戰鬥實體
        /// </summary>
        /// <returns>來源戰鬥實體</returns>
        public CombatEntity GetSourceEntity()
        {
            return sourceCombatEntity;
        }
        
        /// <summary>
        /// 檢查是否可以對目標造成傷害（基於陣營）
        /// </summary>
        /// <param name="targetEntity">目標戰鬥實體</param>
        /// <returns>是否可以造成傷害</returns>
        public bool CanDamageTarget(CombatEntity targetEntity)
        {
            // 如果沒有啟用陣營檢查，所有目標都可以傷害
            if (!enableFactionCheck)
            {
                return true;
            }
            // 如果沒有來源實體或目標實體，無法判斷陣營
            
            if (sourceCombatEntity == null || targetEntity == null)
            {
                Debug.LogWarning($"[DamageDealer] 無法進行陣營檢查 - 來源: {sourceCombatEntity?.Name}, 目標: {targetEntity?.Name}");
                return !enableFactionCheck; // 如果沒有實體資訊且啟用陣營檢查，則不造成傷害
            }
            // 不能傷害同陣營的目標
            if (sourceCombatEntity.Faction == targetEntity.Faction)
            {
                Debug.Log($"[DamageDealer] {sourceCombatEntity.Name} 不能傷害同陣營目標 {targetEntity.Name} (陣營: {targetEntity.Faction})");
                return false;
            }
            // 根據來源陣營判斷可以攻擊的目標
            bool canDamage = IsHostileTarget(sourceCombatEntity.Faction, targetEntity.Faction);
            Debug.Log("canDamage:" + canDamage);
            if (canDamage)
            {
                Debug.Log($"[DamageDealer] {sourceCombatEntity.Name} ({sourceCombatEntity.Faction}) 可以傷害 {targetEntity.Name} ({targetEntity.Faction})");
            }
            else
            {
                Debug.Log($"[DamageDealer] {sourceCombatEntity.Name} ({sourceCombatEntity.Faction}) 不能傷害 {targetEntity.Name} ({targetEntity.Faction})");
            }
            return canDamage;
        }
        
        /// <summary>
        /// 判斷目標是否為敵對陣營
        /// </summary>
        /// <param name="sourceFaction">來源陣營</param>
        /// <param name="targetFaction">目標陣營</param>
        /// <returns>是否為敵對目標</returns>
        private bool IsHostileTarget(CombatEntityFaction sourceFaction, CombatEntityFaction targetFaction)
        {
            // 如果來源是敵對陣營，可以傷害友軍和中立
            if (sourceFaction == CombatEntityFaction.Hostile)
            {
                return targetFaction == CombatEntityFaction.Ally || 
                       targetFaction == CombatEntityFaction.Neutral;
            }
            // 如果來源是友軍，可以傷害敵對
            else if (sourceFaction == CombatEntityFaction.Ally)
            {
                return targetFaction == CombatEntityFaction.Hostile;
            }
            // 如果來源是中立，通常不主動傷害其他陣營（可根據需要調整）
            else if (sourceFaction == CombatEntityFaction.Neutral)
            {
                return false; // 中立通常不主動攻擊
            }
            
            return false;
        }

        private void OnTriggerEnter(Collider other)
        {
            // 尋找 DamageReceiver
            DamageReceiver receiver = other.GetComponent<DamageReceiver>();
            if (receiver != null)
            {
                // DamageReceiver 會自己處理陣營檢查和傷害邏輯
                // 這裡只處理 DamageDealer 的銷毀邏輯
                Debug.Log($"[DamageDealer] 與 {other.name} 發生碰撞，傷害由 DamageReceiver 處理");
            }
        }
    }
}