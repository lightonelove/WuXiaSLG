using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;

namespace Wuxia.GameCore
{
    /// <summary>
    /// 格擋系統 - 處理玩家格擋輸入和時間窗口檢測
    /// 整合了格擋管理功能
    /// </summary>
    public class BlockingSystem : MonoBehaviour
    {
        [Header("格擋時間設定")]
        [Tooltip("攻擊前格擋窗口（毫秒）")]
        [SerializeField] private float preBlockWindowMs = 230f;
        
        [Tooltip("攻擊後格擋窗口（毫秒）")]
        [SerializeField] private float postBlockWindowMs = 66f;
        
        [Header("格擋事件")]
        public UnityEvent onBlockSuccess = new UnityEvent();
        public UnityEvent onBlockAttempt = new UnityEvent();
        
        [Header("管理設定")]
        [Tooltip("是否在場景開始時自動初始化")]
        [SerializeField] private bool autoInitialize = true;
        
        [Tooltip("格擋回饋元件")]
        [SerializeField] private BlockFeedback blockFeedback;
        
        [Tooltip("是否自動尋找格擋回饋元件")]
        [SerializeField] private bool autoFindBlockFeedback = true;
        
        [Header("調試設定")]
        [Tooltip("顯示格擋調試資訊")]
        [SerializeField] private bool showDebugInfo = true;
        
        // 格擋輸入追蹤
        private float lastRightClickTime = -1f;
        private bool isBlockingEnabled = true;
        
        // 單例模式
        public static BlockingSystem Instance { get; private set; }
        
        void Awake()
        {
            // 確保只有一個實例
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        void Start()
        {
            if (autoInitialize)
            {
                SetupBlockFeedback();
            }
        }
        
        void Update()
        {
            // 檢測右鍵輸入
            if (isBlockingEnabled && Mouse.current.rightButton.wasPressedThisFrame)
            {
                HandleBlockInput();
            }
        }
        
        /// <summary>
        /// 處理格擋輸入
        /// </summary>
        private void HandleBlockInput()
        {
            lastRightClickTime = Time.time;
            onBlockAttempt?.Invoke();
            Debug.Log($"[BlockingSystem] 格擋輸入時間: {lastRightClickTime:F3}");
        }
        
        /// <summary>
        /// 檢查是否在格擋窗口內
        /// </summary>
        /// <param name="damageTime">受到傷害的時間</param>
        /// <returns>是否成功格擋</returns>
        public bool CheckBlockWindow(float damageTime)
        {
            if (lastRightClickTime < 0f)
            {
                return false; // 沒有格擋輸入
            }
            
            float timeDifference = (damageTime - lastRightClickTime) * 1000f; // 轉換為毫秒
            
            Debug.Log($"[BlockingSystem] 檢查格擋窗口 - 傷害時間: {damageTime:F3}, 格擋時間: {lastRightClickTime:F3}, 時間差: {timeDifference:F1}ms");
            
            // 檢查預判格擋窗口（230ms 前按下右鍵）
            if (timeDifference >= 0f && timeDifference <= preBlockWindowMs)
            {
                Debug.Log($"[BlockingSystem] 預判格擋成功！時間差: {timeDifference:F1}ms");
                onBlockSuccess?.Invoke();
                return true;
            }
            
            // 檢查反應格擋窗口（66ms 內按下右鍵）
            if (timeDifference >= -postBlockWindowMs && timeDifference < 0f)
            {
                Debug.Log($"[BlockingSystem] 反應格擋成功！時間差: {timeDifference:F1}ms");
                onBlockSuccess?.Invoke();
                return true;
            }
            
            Debug.Log($"[BlockingSystem] 格擋失敗，不在有效窗口內");
            return false;
        }
        
        /// <summary>
        /// 重置格擋狀態（用於新回合或特殊情況）
        /// </summary>
        public void ResetBlockState()
        {
            lastRightClickTime = -1f;
            Debug.Log("[BlockingSystem] 格擋狀態已重置");
        }
        
        /// <summary>
        /// 啟用或禁用格擋功能
        /// </summary>
        /// <param name="enabled">是否啟用</param>
        public void SetBlockingEnabled(bool enabled)
        {
            isBlockingEnabled = enabled;
            Debug.Log($"[BlockingSystem] 格擋功能已{(enabled ? "啟用" : "禁用")}");
        }
        
        /// <summary>
        /// 獲取當前格擋設定
        /// </summary>
        public (float preWindow, float postWindow) GetBlockWindows()
        {
            return (preBlockWindowMs, postBlockWindowMs);
        }
        
        /// <summary>
        /// 設定格擋窗口時間
        /// </summary>
        public void SetBlockWindows(float preWindowMs, float postWindowMs)
        {
            preBlockWindowMs = preWindowMs;
            postBlockWindowMs = postWindowMs;
            Debug.Log($"[BlockingSystem] 格擋窗口已更新 - 預判: {preWindowMs}ms, 反應: {postWindowMs}ms");
        }
        
        /// <summary>
        /// 取得最後一次格擋時間（用於調試）
        /// </summary>
        public float GetLastBlockTime()
        {
            return lastRightClickTime;
        }
        
        // === 以下是整合的管理功能 ===
        
        /// <summary>
        /// 設置格擋回饋
        /// </summary>
        private void SetupBlockFeedback()
        {
            if (blockFeedback == null && autoFindBlockFeedback)
            {
                // 嘗試在場景中尋找格擋回饋元件
                blockFeedback = FindObjectOfType<BlockFeedback>();
            }
            
            if (blockFeedback != null && showDebugInfo)
            {
                Debug.Log($"[BlockingSystem] 找到格擋回饋元件: {blockFeedback.name}");
            }
            else if (showDebugInfo)
            {
                Debug.LogWarning("[BlockingSystem] 未找到格擋回饋元件");
            }
        }
        
        /// <summary>
        /// 測試格擋回饋
        /// </summary>
        [ContextMenu("測試格擋回饋")]
        public void TestBlockFeedback()
        {
            if (blockFeedback != null)
            {
                blockFeedback.ShowBlockFeedback();
            }
            else if (showDebugInfo)
            {
                Debug.LogWarning("[BlockingSystem] 無法測試格擋回饋：找不到 BlockFeedback 元件");
            }
        }
        
        /// <summary>
        /// 顯示格擋系統狀態
        /// </summary>
        [ContextMenu("顯示格擋系統狀態")]
        public void ShowSystemStatus()
        {
            if (showDebugInfo)
            {
                Debug.Log($"[BlockingSystem] 格擋系統狀態:\n" +
                         $"- 預判窗口: {preBlockWindowMs}ms\n" +
                         $"- 反應窗口: {postBlockWindowMs}ms\n" +
                         $"- 最後格擋時間: {lastRightClickTime:F3}\n" +
                         $"- 格擋功能啟用: {isBlockingEnabled}\n" +
                         $"- 回饋元件: {(blockFeedback != null ? blockFeedback.name : "無")}");
            }
        }
        
        /// <summary>
        /// 設定格擋回饋元件
        /// </summary>
        /// <param name="feedback">格擋回饋元件</param>
        public void SetBlockFeedback(BlockFeedback feedback)
        {
            blockFeedback = feedback;
            if (showDebugInfo)
            {
                Debug.Log($"[BlockingSystem] 設定格擋回饋元件: {(feedback != null ? feedback.name : "無")}");
            }
        }
    }
}