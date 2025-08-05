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
    
    public static SLGCoreUI Instance;
    
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
    
    void OnDestroy()
    {
        // 确保恢复系统游标
        Cursor.visible = true;
    }
}
