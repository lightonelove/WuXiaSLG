using UnityEngine;

/// <summary>
/// 讓一個UI元素(RectTransform)跟隨一個3D世界中的目標(Transform)。
/// 這個腳本應該被掛在UI Prefab的根物件上。
/// </summary>
public class UIFollowWorldObject : MonoBehaviour
{
    [Tooltip("要跟隨的3D世界目標")]
    public Transform target;

    [Tooltip("UI在螢幕空間的微調偏移量")]
    public Vector3 screenOffset = new Vector3(0, 30, 0);

    // 私有變數
    private Camera mainCamera;
    private RectTransform rectTransform;

    void Awake()
    {
        // 獲取自身的RectTransform
        rectTransform = GetComponent<RectTransform>();
    }

    void Start()
    {
        // 獲取主攝影機的引用
        mainCamera = Camera.main;
    }

    // 使用LateUpdate確保所有物件的Update都執行完畢，避免抖動
    void LateUpdate()
    {
        if (target == null || mainCamera == null)
        {
            // 如果沒有目標或找不到攝影機，則隱藏自己
            gameObject.SetActive(false);
            return;
        }

        // 進行座標轉換
        Vector3 worldPosition = target.position;
        Vector3 screenPosition = mainCamera.WorldToScreenPoint(worldPosition);

        // 如果目標在攝影機後面，則不顯示UI
        // screenPosition.z < 0 代表物件在攝影機平面的後方
        if (screenPosition.z < 0)
        {
            gameObject.SetActive(false);
        }
        else
        {
            gameObject.SetActive(true);
            // 更新UI位置，並加上偏移量
            rectTransform.position = screenPosition + screenOffset;
        }
    }

    /// <summary>
    /// 設定要跟隨的目標
    /// </summary>
    /// <param name="targetToFollow">3D世界中的目標Transform</param>
    public void SetTarget(Transform targetToFollow)
    {
        this.target = targetToFollow;
    }
}