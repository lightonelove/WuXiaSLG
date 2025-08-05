using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class SLGCoreUI : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    public APBar apBar;
    public TurnOrderUIController turnOrderUIController;
    
    [Header("Custom Cursor")]
    public Image cursorImage;
    public bool enableCustomCursor = true;
    public Vector2 cursorOffset = Vector2.zero;
    
    [Header("Floor Mouse Indicator")]
    public GameObject floorIndicatorPrefab; // FloorIndicator prefab
    public LayerMask floorLayerMask = -1; // Floor層級遮罩
    public bool enableFloorIndicator = true;
    
    public static SLGCoreUI Instance;
    private FloorMouseIndicator floorMouseIndicator;
    
    private RectTransform cursorRectTransform;
    private Canvas parentCanvas;
    void Start()
    {
        Instance = this;
        
        // 初始化cursor相关组件
        if (cursorImage != null)
        {
            cursorRectTransform = cursorImage.GetComponent<RectTransform>();
            parentCanvas = GetComponentInParent<Canvas>();
            
            // 隐藏系统游标
            if (enableCustomCursor)
            {
                Cursor.visible = false;
            }
        }
        
        // 初始化Floor指示器
        InitializeFloorIndicator();
    }

    // Update is called once per frame
    void Update()
    {
        // 更新cursor位置
        if (enableCustomCursor && cursorImage != null && cursorRectTransform != null)
        {
            UpdateCursorPosition();
        }
    }
    
    private void UpdateCursorPosition()
    {
        // 获取鼠标位置
        Vector2 mousePosition = Mouse.current.position.ReadValue();
        
        // 转换屏幕坐标到Canvas坐标
        Vector2 canvasPosition;
        if (parentCanvas != null)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentCanvas.transform as RectTransform,
                mousePosition,
                parentCanvas.worldCamera,
                out canvasPosition
            );
        }
        else
        {
            canvasPosition = mousePosition;
        }
        
        // 应用偏移并设置位置
        cursorRectTransform.localPosition = canvasPosition + cursorOffset;
    }
    
    public void SetCursorVisibility(bool visible)
    {
        if (cursorImage != null)
        {
            cursorImage.gameObject.SetActive(visible);
        }
        
        // 同时控制系统游标
        Cursor.visible = !visible || !enableCustomCursor;
    }
    
    public void SetCursorSprite(Sprite newSprite)
    {
        if (cursorImage != null)
        {
            cursorImage.sprite = newSprite;
        }
    }
    
    private void InitializeFloorIndicator()
    {
        // 查找或創建FloorMouseIndicator組件
        floorMouseIndicator = GetComponentInChildren<FloorMouseIndicator>();
        
        if (floorMouseIndicator == null)
        {
            // 創建新的GameObject來承載FloorMouseIndicator
            GameObject indicatorObj = new GameObject("FloorMouseIndicator");
            indicatorObj.transform.SetParent(this.transform);
            floorMouseIndicator = indicatorObj.AddComponent<FloorMouseIndicator>();
        }
        
        // 設定Floor指示器參數
        if (floorMouseIndicator != null)
        {
            floorMouseIndicator.indicatorPrefab = floorIndicatorPrefab;
            floorMouseIndicator.floorLayerMask = floorLayerMask;
            floorMouseIndicator.showIndicator = enableFloorIndicator;
        }
    }
    
    /// <summary>
    /// 設定Floor指示器的顯示狀態
    /// </summary>
    /// <param name="visible">是否顯示</param>
    public void SetFloorIndicatorVisibility(bool visible)
    {
        enableFloorIndicator = visible;
        if (floorMouseIndicator != null)
        {
            floorMouseIndicator.SetIndicatorVisibility(visible);
        }
    }
    
    /// <summary>
    /// 獲取鼠標在Floor上的位置
    /// </summary>
    /// <returns>Floor位置，如果沒有命中則返回Vector3.zero</returns>
    public Vector3 GetMouseFloorPosition()
    {
        if (floorMouseIndicator != null)
        {
            return floorMouseIndicator.GetMouseFloorPosition();
        }
        return Vector3.zero;
    }
    
    /// <summary>
    /// 檢查鼠標是否指向Floor
    /// </summary>
    /// <returns>如果指向Floor則返回true</returns>
    public bool IsMouseOverFloor()
    {
        if (floorMouseIndicator != null)
        {
            return floorMouseIndicator.IsMouseOverFloor();
        }
        return false;
    }
    
    /// <summary>
    /// 設定Floor指示器的prefab
    /// </summary>
    /// <param name="newPrefab">新的指示器prefab</param>
    public void SetFloorIndicatorPrefab(GameObject newPrefab)
    {
        floorIndicatorPrefab = newPrefab;
        if (floorMouseIndicator != null)
        {
            floorMouseIndicator.SetIndicatorPrefab(newPrefab);
        }
    }
    
    /// <summary>
    /// 設定Floor圖層遮罩
    /// </summary>
    /// <param name="layerMask">圖層遮罩</param>
    public void SetFloorLayerMask(LayerMask layerMask)
    {
        floorLayerMask = layerMask;
        if (floorMouseIndicator != null)
        {
            floorMouseIndicator.SetFloorLayerMask(layerMask);
        }
    }
    
    void OnDestroy()
    {
        // 确保恢复系统游标
        Cursor.visible = true;
    }
}
