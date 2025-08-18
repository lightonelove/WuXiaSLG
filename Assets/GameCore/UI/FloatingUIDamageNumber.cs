using UnityEngine;
using TMPro;

public class FloatingUIDamageNumber : MonoBehaviour
{
    public float lifeTime = 1f;
    public float moveSpeed = 100f;
    public float fadeStartTimeRatio = 0.5f;

    private TextMeshProUGUI textMeshProUGUI;
    private RectTransform rectTransform;
    private float timer;
    private Color initialColor;

    private void Awake()
    {
        textMeshProUGUI = GetComponent<TextMeshProUGUI>();
        rectTransform = GetComponent<RectTransform>();
        if (textMeshProUGUI == null || rectTransform == null)
        {
            Debug.LogError("FloatingUIDamageNumber: 找不到 TextMeshProUGUI 或 RectTransform 元件！");
            enabled = false;
        }
    }

    void Start()
    {
        Destroy(gameObject, lifeTime);
        timer = 0f;
        if (textMeshProUGUI != null)
        {
            initialColor = textMeshProUGUI.color;
        }
    }

    void Update()
    {
        timer += Time.deltaTime;

        // 向上移動 (在 UI 空間中)
        rectTransform.anchoredPosition += Vector2.up * moveSpeed * Time.deltaTime;

        if (timer >= lifeTime * fadeStartTimeRatio && textMeshProUGUI != null)
        {
            float fadeProgress = (timer - lifeTime * fadeStartTimeRatio) / (lifeTime * (1 - fadeStartTimeRatio));
            Color newColor = initialColor;
            newColor.a = Mathf.Lerp(1f, 0f, fadeProgress);
            textMeshProUGUI.color = newColor;
        }
    }

    public void Setup(float damageAmount)
    {
        if (textMeshProUGUI != null)
        {
            textMeshProUGUI.text = Mathf.RoundToInt(damageAmount).ToString();
        }
    }
    
    /// <summary>
    /// 設定格擋文字
    /// </summary>
    /// <param name="blockText">格擋文字內容</param>
    /// <param name="blockColor">格擋文字顏色</param>
    public void SetupBlockText(string blockText, Color blockColor)
    {
        if (textMeshProUGUI != null)
        {
            textMeshProUGUI.text = blockText;
            textMeshProUGUI.color = blockColor;
            initialColor = blockColor;
        }
    }
}