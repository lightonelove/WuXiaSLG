using Sirenix.OdinInspector;
using UnityEngine;

namespace Wuxia.GameCore
{
    public class UIDamageNumberSpawner : MonoBehaviour
    {
        public GameObject uiDamageNumberPrefab;
        public Vector3 spawnOffset = new Vector3(0, 1.5f, 0);
        public Canvas targetCanvas; // 指定要將 UI 元素放在哪個 Canvas 上
        
        [Header("格擋文字設定")]
        [Tooltip("格擋文字顏色")]
        public Color blockTextColor = Color.cyan;
        
        [Tooltip("格擋文字內容")]
        public string blockText = "格擋!!";

        public Health health;
        public DamageReceiver damageReceiver;

        private void Awake()
        {
            // 如果沒有手動指定 Canvas，嘗試在場景中尋找
            if (targetCanvas == null)
            {
                targetCanvas = FindObjectOfType<Canvas>();
                if (targetCanvas == null)
                {
                    Debug.LogError("UIDamageNumberSpawner: 找不到場景中的 Canvas！請手動指定。");
                    enabled = false;
                }
            }
        }

        private void OnEnable()
        {
            if (health != null)
            {
                health.OnDamageTaken.AddListener(CreateDamageNumberUI);
            }
            
            if (damageReceiver != null)
            {
                damageReceiver.onDamageBlocked.AddListener(CreateBlockTextUI);
            }
        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.OnDamageTaken.RemoveListener(CreateDamageNumberUI);
            }
            
            if (damageReceiver != null)
            {
                damageReceiver.onDamageBlocked.RemoveListener(CreateBlockTextUI);
            }
        }

        private void CreateDamageNumberUI(float damageAmount)
        {
            if (uiDamageNumberPrefab == null || targetCanvas == null)
            {
                Debug.LogError("UIDamageNumberSpawner: uiDamageNumberPrefab 或 targetCanvas 未設定！");
                return;
            }

            // 計算敵人頭頂在螢幕上的位置
            Vector3 worldPosition = transform.position + spawnOffset;
            Vector3 screenPosition = Camera.main.WorldToScreenPoint(worldPosition);

            // 在 Canvas 上生成 UI 傷害數字
            GameObject uiNumberInstance = Instantiate(uiDamageNumberPrefab, targetCanvas.transform);

            // 取得 RectTransform 以設定位置
            RectTransform rectTransform = uiNumberInstance.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                // 將 UI 元素的世界座標設定為轉換後的螢幕座標
                rectTransform.position = screenPosition;

                // 設定傷害數值
                FloatingUIDamageNumber damageNumber = uiNumberInstance.GetComponent<FloatingUIDamageNumber>();
                if (damageNumber != null)
                {
                    damageNumber.Setup(damageAmount);
                }
                else
                {
                    Debug.LogWarning("在生成的 UI 傷害數字 Prefab 上找不到 FloatingUIDamageNumber 腳本！");
                }
            }
            else
            {
                Debug.LogError("生成的 UI 物件沒有 RectTransform 元件！");
                Destroy(uiNumberInstance); // 清理錯誤生成的物件
            }
        }
        
        /// <summary>
        /// 創建格擋文字 UI
        /// </summary>
        private void CreateBlockTextUI()
        {
            if (uiDamageNumberPrefab == null || targetCanvas == null)
            {
                Debug.LogError("UIDamageNumberSpawner: uiDamageNumberPrefab 或 targetCanvas 未設定！");
                return;
            }

            // 計算實體頭頂在螢幕上的位置
            Vector3 worldPosition = transform.position + spawnOffset;
            Vector3 screenPosition = Camera.main.WorldToScreenPoint(worldPosition);

            // 在 Canvas 上生成格擋文字 UI
            GameObject uiNumberInstance = Instantiate(uiDamageNumberPrefab, targetCanvas.transform);

            // 取得 RectTransform 以設定位置
            RectTransform rectTransform = uiNumberInstance.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                // 將 UI 元素的世界座標設定為轉換後的螢幕座標
                rectTransform.position = screenPosition;

                // 設定格擋文字
                FloatingUIDamageNumber damageNumber = uiNumberInstance.GetComponent<FloatingUIDamageNumber>();
                if (damageNumber != null)
                {
                    damageNumber.SetupBlockText(blockText, blockTextColor);
                    Debug.Log($"[UIDamageNumberSpawner] 在 {gameObject.name} 上顯示格擋文字: {blockText}");
                }
                else
                {
                    Debug.LogWarning("在生成的 UI 格擋文字 Prefab 上找不到 FloatingUIDamageNumber 腳本！");
                }
            }
            else
            {
                Debug.LogError("生成的 UI 物件沒有 RectTransform 元件！");
                Destroy(uiNumberInstance); // 清理錯誤生成的物件
            }
        }
        
        /// <summary>
        /// 手動觸發格擋文字顯示（用於測試）
        /// </summary>
        [Button]
        public void TestBlockText()
        {
            CreateBlockTextUI();
        }
    }
}