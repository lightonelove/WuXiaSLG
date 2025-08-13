using System;
using UnityEngine;
using UnityEngine.UI; // 記得引用UI命名空間

/// <summary>
/// 掛在敵人身上，負責實體化並管理其對應的血條。
/// </summary>
namespace Wuxia.GameCore
{
    public class EnemyHealthBarController : MonoBehaviour
    {
        [Header("設置")] [Tooltip("血條的UI Prefab")]
        public GameObject healthBarPrefab;

        [Tooltip("血條要跟隨的敵人頭頂錨點")] public Transform anchor;

        public Health health;



        // --- 私有變數 ---
        private GameObject healthBarInstance;
        private UIFollowWorldObject followScript;
        private Slider healthSlider;

        // 假設敵人有這個腳本來管理血量
        // private EnemyHealth enemyHealth; 

        void Start()
        {
            // 如果沒有指定錨點，就用敵人自己的transform
            if (anchor == null)
            {
                anchor = this.transform;
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
        /// 其他腳本 (例如敵人的血量腳本) 可以呼叫這個函式。
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
            UpdateHealth(health.CurrentHealth, health.MaxHealth);
        }


        // 當敵人物件被銷毀時，也要一併銷毀它對應的血條
        void OnDestroy()
        {
            if (healthBarInstance != null)
            {
                Destroy(healthBarInstance);
            }
        }

        // 當敵人被禁用時，也隱藏血條
        void OnDisable()
        {
            if (healthBarInstance != null)
            {
                healthBarInstance.SetActive(false);
            }
        }

        // 當敵人被重新啟用時，也顯示血條
        void OnEnable()
        {
            if (healthBarInstance != null)
            {
                healthBarInstance.SetActive(true);
            }
        }
    }
}