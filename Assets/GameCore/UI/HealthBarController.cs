using System;
using UnityEngine;
using UnityEngine.UI; // 記得引用UI命名空間

/// <summary>
/// 通用的血條控制器，可掛在任何有 Health 組件的角色身上，負責實體化並管理其對應的血條。
/// </summary>
namespace Wuxia.GameCore
{
    public class HealthBarController : MonoBehaviour
    {
        [Header("設置")] [Tooltip("血條的UI Prefab")]
        public GameObject healthBarPrefab;

        [Tooltip("血條要跟隨的角色頭頂錨點")] public Transform anchor;
        
        [Tooltip("自動尋找 Health 組件（如果未設定）")] 
        public bool autoFindHealth = true;
        
        [Tooltip("是否顯示血條（可動態控制）")] 
        public bool showHealthBar = true;

        public Health health;
        
        // --- 私有變數 ---
        private GameObject healthBarInstance;
        private UIFollowWorldObject followScript;
        private Slider healthSlider;

        // Health 組件自動在上方設定，用於管理血量 

        void Start()
        {
            // 如果沒有指定錨點，就用角色自己的transform
            if (anchor == null)
            {
                anchor = this.transform;
            }
            
            // 如果沒有設定 Health 組件且啟用自動尋找，嘗試自動獲取
            if (health == null && autoFindHealth)
            {
                health = GetComponent<Health>();
                if (health == null)
                {
                    Debug.LogError($"[HealthBarController] {gameObject.name} 沒有找到 Health 組件！", this);
                    return;
                }
            }
            
            // 檢查是否有有效的 Health 組件
            if (health == null)
            {
                Debug.LogError($"[HealthBarController] {gameObject.name} 沒有設定 Health 組件！", this);
                return;
            }

            // 實體化血條
            InstantiateHealthBar();

            UpdateHealth(health.CurrentHealth, health.MaxHealth);

        }

        private void InstantiateHealthBar()
        {
            // 找到場景中的主Canvas
            Canvas mainCanvas = FindObjectOfType<Canvas>();
            if (mainCanvas == null)
            {
                Debug.LogError("場景中找不到Canvas！無法創建血條。");
                return;
            }

            // 在Canvas底下實體化血條Prefab
            healthBarInstance = Instantiate(healthBarPrefab, mainCanvas.transform);

            // 獲取血條上的必要元件
            followScript = healthBarInstance.GetComponent<UIFollowWorldObject>();
            healthSlider = healthBarInstance.GetComponent<Slider>();
            Transform fillTransform = healthBarInstance.transform.Find("Fill");

            // 設定跟隨目標
            if (followScript != null)
            {
                followScript.SetTarget(anchor);
            }
            else
            {
                Debug.LogError("血條Prefab上缺少 UIFollowWorldObject 腳本！", healthBarInstance);
            }
        }

        /// <summary>
        /// 公開的函式，用來更新血條的顯示。
        /// 其他腳本 (例如角色的血量腳本) 可以呼叫這個函式。
        /// </summary>
        /// <param name="currentHealth">目前血量</param>
        /// <param name="maxHealth">最大血量</param>
        public void UpdateHealth(float currentHealth, float maxHealth)
        {
            if (healthSlider == null) return;
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;
        }

        public void Update()
        {
            if (health != null)
            {
                UpdateHealth(health.CurrentHealth, health.MaxHealth);
            }
            
            // 根據 showHealthBar 設定來控制血條顯示
            if (healthBarInstance != null && healthBarInstance.activeInHierarchy != showHealthBar)
            {
                healthBarInstance.SetActive(showHealthBar);
            }
        }
        
        /// <summary>
        /// 設定血條是否顯示
        /// </summary>
        /// <param name="show">是否顯示血條</param>
        public void SetHealthBarVisible(bool show)
        {
            showHealthBar = show;
            if (healthBarInstance != null)
            {
                healthBarInstance.SetActive(show);
            }
        }
        
        /// <summary>
        /// 手動設定 Health 組件
        /// </summary>
        /// <param name="newHealth">新的 Health 組件</param>
        public void SetHealth(Health newHealth)
        {
            health = newHealth;
        }
        
        /// <summary>
        /// 檢查血條是否已經初始化
        /// </summary>
        /// <returns>血條是否已初始化</returns>
        public bool IsHealthBarInitialized()
        {
            return healthBarInstance != null;
        }


        // 當角色物件被銷毀時，也要一併銷毀它對應的血條
        void OnDestroy()
        {
            if (healthBarInstance != null)
            {
                Destroy(healthBarInstance);
            }
        }

        // 當角色被禁用時，也隱藏血條
        void OnDisable()
        {
            if (healthBarInstance != null)
            {
                healthBarInstance.SetActive(false);
            }
        }

        // 當角色被重新啟用時，也顯示血條
        void OnEnable()
        {
            if (healthBarInstance != null)
            {
                healthBarInstance.SetActive(true);
            }
        }
    }
}