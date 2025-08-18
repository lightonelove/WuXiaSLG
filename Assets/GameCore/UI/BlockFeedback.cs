using UnityEngine;
using TMPro;
using System.Collections;

namespace Wuxia.GameCore
{
    /// <summary>
    /// 格擋視覺回饋元件 - 顯示格擋成功的視覺效果
    /// </summary>
    public class BlockFeedback : MonoBehaviour
    {
        [Header("視覺效果設定")]
        [Tooltip("格擋成功文字")]
        [SerializeField] private TextMeshProUGUI blockText;
        
        [Tooltip("格擋成功時的顏色")]
        [SerializeField] private Color blockColor = Color.cyan;
        
        [Tooltip("文字顯示時間")]
        [SerializeField] private float displayDuration = 1.5f;
        
        [Tooltip("文字淡出時間")]
        [SerializeField] private float fadeOutDuration = 0.5f;
        
        [Tooltip("文字上升距離")]
        [SerializeField] private float moveUpDistance = 50f;
        
        [Header("音效設定")]
        [Tooltip("格擋成功音效")]
        [SerializeField] private AudioClip blockSound;
        
        [Tooltip("音效播放器")]
        [SerializeField] private AudioSource audioSource;
        
        private Coroutine currentFeedbackCoroutine;
        private Vector3 originalPosition;
        private Color originalColor;
        
        void Awake()
        {
            // 初始化
            if (blockText != null)
            {
                originalPosition = blockText.transform.localPosition;
                originalColor = blockText.color;
                
                // 初始時隱藏文字
                blockText.gameObject.SetActive(false);
            }
            
            // 如果沒有音效播放器，嘗試獲取或創建
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                    audioSource.playOnAwake = false;
                }
            }
        }
        
        void Start()
        {
            // 註冊格擋事件
            if (BlockingSystem.Instance != null)
            {
                BlockingSystem.Instance.onBlockSuccess.AddListener(ShowBlockFeedback);
            }
        }
        
        void OnDestroy()
        {
            // 取消註冊事件
            if (BlockingSystem.Instance != null)
            {
                BlockingSystem.Instance.onBlockSuccess.RemoveListener(ShowBlockFeedback);
            }
        }
        
        /// <summary>
        /// 顯示格擋成功回饋
        /// </summary>
        public void ShowBlockFeedback()
        {
            // 停止之前的協程
            if (currentFeedbackCoroutine != null)
            {
                StopCoroutine(currentFeedbackCoroutine);
            }
            
            // 開始新的回饋效果
            currentFeedbackCoroutine = StartCoroutine(PlayBlockFeedback());
        }
        
        /// <summary>
        /// 播放格擋回饋效果
        /// </summary>
        private IEnumerator PlayBlockFeedback()
        {
            if (blockText == null) yield break;
            
            // 重置位置和顏色
            blockText.transform.localPosition = originalPosition;
            blockText.color = blockColor;
            blockText.text = "格擋！";
            blockText.gameObject.SetActive(true);
            
            // 播放音效
            if (audioSource != null && blockSound != null)
            {
                audioSource.PlayOneShot(blockSound);
            }
            
            // 計算移動和淡出
            Vector3 startPosition = originalPosition;
            Vector3 endPosition = originalPosition + Vector3.up * moveUpDistance;
            float elapsedTime = 0f;
            
            // 顯示階段
            float showTime = displayDuration - fadeOutDuration;
            while (elapsedTime < showTime)
            {
                elapsedTime += Time.deltaTime;
                float progress = elapsedTime / showTime;
                
                // 緩慢上升
                blockText.transform.localPosition = Vector3.Lerp(startPosition, endPosition, progress * 0.3f);
                
                yield return null;
            }
            
            // 淡出階段
            elapsedTime = 0f;
            Color startColor = blockText.color;
            Color endColor = new Color(startColor.r, startColor.g, startColor.b, 0f);
            
            while (elapsedTime < fadeOutDuration)
            {
                elapsedTime += Time.deltaTime;
                float progress = elapsedTime / fadeOutDuration;
                
                // 繼續上升並淡出
                float totalProgress = (showTime + elapsedTime) / displayDuration;
                blockText.transform.localPosition = Vector3.Lerp(startPosition, endPosition, totalProgress);
                blockText.color = Color.Lerp(startColor, endColor, progress);
                
                yield return null;
            }
            
            // 隱藏文字
            blockText.gameObject.SetActive(false);
            currentFeedbackCoroutine = null;
        }
        
        /// <summary>
        /// 設定格擋文字內容
        /// </summary>
        /// <param name="text">文字內容</param>
        public void SetBlockText(string text)
        {
            if (blockText != null)
            {
                blockText.text = text;
            }
        }
        
        /// <summary>
        /// 設定格擋顏色
        /// </summary>
        /// <param name="color">顏色</param>
        public void SetBlockColor(Color color)
        {
            blockColor = color;
        }
        
        /// <summary>
        /// 手動觸發格擋回饋（用於測試）
        /// </summary>
        [ContextMenu("測試格擋回饋")]
        public void TestBlockFeedback()
        {
            ShowBlockFeedback();
        }
    }
}