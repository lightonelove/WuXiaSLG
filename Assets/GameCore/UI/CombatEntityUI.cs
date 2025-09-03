using System;
using UnityEngine;
using UnityEngine.UI; // 記得引用UI命名空間

/// <summary>
/// 戰鬥實體UI控制器，負責管理戰鬥實體頭上的所有UI元素（血條、狀態等）。
/// </summary>
namespace Wuxia.GameCore
{
    public class CombatEntityUI : MonoBehaviour
    {
        [Header("設置")] [Tooltip("CombatEntity的UI Prefab")]
        public GameObject CombatEntityUIPrefab;
        public CombatEntity combatEntity;

        [Tooltip("血條要跟隨的角色頭頂錨點")] public Transform anchor;
        
        [Tooltip("是否顯示血條（可動態控制）")] 
        public bool showHealthBar = true;
        
        [Tooltip("是否顯示架勢條（可動態控制）")]
        public bool showPostureBar = true;
        
        // --- 私有變數 ---
        private GameObject combatEntityInstance;
        private GameObject healthBarObj;
        private GameObject postureBarObj;
        
        private UIFollowWorldObject followScript;
        private Slider healthSlider;
        private Slider postureSlider;
        private Health health;
        private PosturePoint posturePoint;

        // Health 組件自動在上方設定，用於管理血量 

        void Start()
        {
            // 如果沒有指定錨點，就用角色自己的transform
            if (anchor == null)
            {
                anchor = this.transform;
                Debug.LogError("沒有設定Anchor");
            }

            health = combatEntity.health;
            posturePoint = combatEntity.PosturePoint;
            // 實體化血條
            InstantiateHealthBar();
            UpdateHealth(health.CurrentHealth, health.MaxHealth);
            if (posturePoint != null)
            {
                UpdatePosture(posturePoint.CurrentPP, posturePoint.MaxPP);
            }

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
            Debug.Log("Hello?");
            // 在Canvas底下實體化血條Prefab
            combatEntityInstance = Instantiate(CombatEntityUIPrefab, mainCanvas.transform);
            // 獲取血條上的必要元件
            followScript = combatEntityInstance.GetComponent<UIFollowWorldObject>();
            healthBarObj = combatEntityInstance.transform.Find("HealthBar").gameObject;
            healthSlider = healthBarObj.GetComponent<Slider>();
            
            // 獲取架勢條元件
            Transform postureBarTransform = combatEntityInstance.transform.Find("PostureBar");
            if (postureBarTransform != null)
            {
                postureBarObj = postureBarTransform.gameObject;
                postureSlider = postureBarObj.GetComponent<Slider>();
            }
            else
            {
                Debug.LogWarning("找不到 PostureBar，請確認 Prefab 中有 PostureBar 物件");
            }
            
            Debug.Log("Hello?2222");
            // 設定跟隨目標
            if (followScript != null)
            {
                followScript.SetTarget(anchor);
                Debug.Log("Hello?33333333");
            }
            else
            {
                Debug.LogError("血條Prefab上缺少 UIFollowWorldObject 腳本！", healthBarObj);
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
        
        /// <summary>
        /// 更新架勢條的顯示
        /// </summary>
        /// <param name="currentPosture">當前架勢點數</param>
        /// <param name="maxPosture">最大架勢點數</param>
        public void UpdatePosture(float currentPosture, float maxPosture)
        {
            if (postureSlider == null) return;
            postureSlider.maxValue = maxPosture;
            postureSlider.value = currentPosture;
        }

        public void Update()
        {
            if (health != null)
            {
                UpdateHealth(health.CurrentHealth, health.MaxHealth);
            }
            
            if (posturePoint != null)
            {
                UpdatePosture(posturePoint.CurrentPP, posturePoint.MaxPP);
            }
            
            // 根據 showHealthBar 設定來控制血條顯示
            if (healthBarObj != null && healthBarObj.activeInHierarchy != showHealthBar)
            {
                healthBarObj.SetActive(showHealthBar);
            }
            
            // 根據 showPostureBar 設定來控制架勢條顯示
            if (postureBarObj != null && postureBarObj.activeInHierarchy != showPostureBar)
            {
                postureBarObj.SetActive(showPostureBar);
            }
        }
        
        /// <summary>
        /// 設定血條是否顯示
        /// </summary>
        /// <param name="show">是否顯示血條</param>
        public void SetHealthBarVisible(bool show)
        {
            showHealthBar = show;
            if (healthBarObj != null)
            {
                healthBarObj.SetActive(show);
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
        /// 手動設定 PosturePoint 組件
        /// </summary>
        /// <param name="newPosturePoint">新的 PosturePoint 組件</param>
        public void SetPosturePoint(PosturePoint newPosturePoint)
        {
            posturePoint = newPosturePoint;
        }
        
        /// <summary>
        /// 設定架勢條是否顯示
        /// </summary>
        /// <param name="show">是否顯示架勢條</param>
        public void SetPostureBarVisible(bool show)
        {
            showPostureBar = show;
            if (postureBarObj != null)
            {
                postureBarObj.SetActive(show);
            }
        }

        // 當角色物件被銷毀時，也要一併銷毀它對應的血條
        void OnDestroy()
        {
            if (combatEntityInstance != null)
            {
                Destroy(combatEntityInstance);
            }
        }

        // 當角色被禁用時，也隱藏UI
        void OnDisable()
        {
            if (combatEntityInstance != null)
            {
                combatEntityInstance.SetActive(false);
            }
        }

        // 當角色被重新啟用時，也顯示UI
        void OnEnable()
        {
            if (combatEntityInstance != null)
            {
                combatEntityInstance.SetActive(true);
            }
        }
    }
}