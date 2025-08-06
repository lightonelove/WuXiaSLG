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
    
    [Header("Action Buttons")]
    public GameObject actionButtonsPanel;    // 按鈕面板
    public Button moveButton;               // 移動按鈕
    public Button skillAButton;             // 技能A按鈕
    public Button skillBButton;             // 技能B按鈕
    public Button skillCButton;             // 技能C按鈕
    public Button skillDButton;             // 技能D按鈕
    
    [Header("Button Colors")]
    public Color normalButtonColor = Color.white;
    public Color selectedButtonColor = Color.green;
    public Color disabledButtonColor = Color.gray;
    
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
        
        // 檢查基本UI設定
        CheckUISetup();
        
        // 初始化動作按鈕
        InitializeActionButtons();
    }


    public void TestClick()
    {
        Debug.Log("[SLGCoreUI] TestClick() called");
    }
    // Update is called once per frame
    void Update()
    {
        // 更新cursor位置
        if (enableCustomCursor && cursorImage != null && cursorRectTransform != null)
        {
            UpdateCursorPosition();
        }
        
        // 處理滑鼠點擊移動
        HandleMouseClickMovement();
        
        // 更新按鈕狀態
        UpdateActionButtons();
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
    
    /// <summary>
    /// 處理滑鼠點擊移動
    /// </summary>
    private void HandleMouseClickMovement()
    {
        // 檢查是否有戰鬥核心
        if (CombatCore.Instance == null)
            return;
            
        // 檢查是否為玩家回合
        if (!CombatCore.Instance.IsPlayerTurn())
            return;
            
        // 取得當前回合實體
        CombatEntity currentEntity = CombatCore.Instance.GetCurrentTurnEntity();
        if (currentEntity == null)
            return;
            
        // 檢查是否為玩家角色
        CharacterCore currentCharacter = currentEntity.GetComponent<CharacterCore>();
        if (currentCharacter == null)
            return;
            
        // 檢查角色是否在控制狀態
        if (currentCharacter.nowState != CharacterCore.CharacterCoreState.ControlState)
            return;
        
        // 只在Move模式下預覽路徑
        if (currentCharacter.currentActionMode == CharacterCore.PlayerActionMode.Move)
        {
            if (IsMouseOverFloor() && !currentCharacter.isMoving)
            {
                Vector3 hoverPosition = GetMouseFloorPosition();
                if (hoverPosition != Vector3.zero)
                {
                    // 預覽路徑但不執行移動
                    currentCharacter.PreviewPath(hoverPosition);
                }
            }
            else if (!currentCharacter.isMoving)
            {
                // 如果滑鼠不在Floor上或角色正在移動，清除路徑顯示
                currentCharacter.ClearPathDisplay();
            }
        }
        else
        {
            // 不在Move模式，清除任何路徑預覽
            currentCharacter.ClearPathDisplay();
        }
            
        // 檢查滑鼠左鍵點擊（只在對應模式下處理）
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (currentCharacter.currentActionMode == CharacterCore.PlayerActionMode.Move)
            {
                // 檢查滑鼠是否指向Floor
                if (IsMouseOverFloor())
                {
                    // 取得滑鼠在Floor上的位置
                    Vector3 targetPosition = GetMouseFloorPosition();
                    if (targetPosition != Vector3.zero)
                    {
                        // 讓角色移動到該位置
                        currentCharacter.MoveTo(targetPosition);
                    }
                }
            }
        }
        
        // 檢查滑鼠右鍵點擊（確認回合）
        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            currentCharacter.ConfirmTurn();
        }
    }
    
    void OnDestroy()
    {
        // 确保恢复系统游标
        Cursor.visible = true;
    }
    
    /// <summary>
    /// 檢查UI基本設定
    /// </summary>
    private void CheckUISetup()
    {
        // 檢查EventSystem
        UnityEngine.EventSystems.EventSystem eventSystem = FindObjectOfType<UnityEngine.EventSystems.EventSystem>();
        if (eventSystem == null)
        {
        }
        else
        {
        }
        
        // 檢查Canvas和GraphicRaycaster
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
        }
        else
        {
            UnityEngine.UI.GraphicRaycaster raycaster = canvas.GetComponent<UnityEngine.UI.GraphicRaycaster>();
            if (raycaster == null)
            {
            }
            else
            {
            }
        }
        
        // 檢查按鈕引用
    }
    
    /// <summary>
    /// 初始化動作按鈕
    /// </summary>
    private void InitializeActionButtons()
    {
        
        // 設定按鈕點擊事件
        if (moveButton != null)
        {
            moveButton.onClick.AddListener(() => {
                SetActionMode(CharacterCore.PlayerActionMode.Move);
            });
        }
        else
        {
        }
        
        if (skillAButton != null)
        {
            skillAButton.onClick.AddListener(() => {
                SetActionMode(CharacterCore.PlayerActionMode.SkillA);
            });
        }
        else
        {
        }
        
        if (skillBButton != null)
        {
            skillBButton.onClick.AddListener(() => {
                SetActionMode(CharacterCore.PlayerActionMode.SkillB);
            });
        }
        else
        {
        }
        
        if (skillCButton != null)
        {
            skillCButton.onClick.AddListener(() => {
                SetActionMode(CharacterCore.PlayerActionMode.SkillC);
            });
        }
        else
        {
        }
        
        if (skillDButton != null)
        {
            skillDButton.onClick.AddListener(() => {
                SetActionMode(CharacterCore.PlayerActionMode.SkillD);
            });
        }
        else
        {
        }
        
        // 初始隱藏按鈕面板
        if (actionButtonsPanel != null)
        {
            actionButtonsPanel.SetActive(false);
        }
        else
        {
        }
    }
    
    /// <summary>
    /// 設定當前玩家的動作模式
    /// </summary>
    /// <param name="mode">動作模式</param>
    private void SetActionMode(CharacterCore.PlayerActionMode mode)
    {
        
        // 檢查是否有戰鬥核心和當前玩家
        if (CombatCore.Instance == null)
        {
            return;
        }
        
        if (!CombatCore.Instance.IsPlayerTurn())
        {
            return;
        }
            
        CombatEntity currentEntity = CombatCore.Instance.GetCurrentTurnEntity();
        if (currentEntity == null)
        {
            return;
        }
            
        CharacterCore currentCharacter = currentEntity.GetComponent<CharacterCore>();
        if (currentCharacter == null)
        {
            return;
        }
        
        if (currentCharacter.nowState != CharacterCore.CharacterCoreState.ControlState)
        {
            return;
        }
        
        
        // 設定動作模式
        currentCharacter.currentActionMode = mode;
        
        // 清除任何現有的路徑預覽
        currentCharacter.ClearPathDisplay();
        
    }
    
    /// <summary>
    /// 更新動作按鈕的顯示狀態
    /// </summary>
    private void UpdateActionButtons()
    {
        // 檢查是否為玩家回合
        bool isPlayerTurn = CombatCore.Instance != null && CombatCore.Instance.IsPlayerTurn();
        
        if (actionButtonsPanel != null)
        {
            bool wasActive = actionButtonsPanel.activeSelf;
            actionButtonsPanel.SetActive(isPlayerTurn);
            if (wasActive != isPlayerTurn)
            {
            }
        }
        
        if (!isPlayerTurn)
            return;
            
        // 取得當前玩家角色
        CombatEntity currentEntity = CombatCore.Instance.GetCurrentTurnEntity();
        if (currentEntity == null)
            return;
            
        CharacterCore currentCharacter = currentEntity.GetComponent<CharacterCore>();
        if (currentCharacter == null)
            return;
        
        // 更新按鈕顏色根據當前模式
        UpdateButtonColor(moveButton, currentCharacter.currentActionMode == CharacterCore.PlayerActionMode.Move);
        UpdateButtonColor(skillAButton, currentCharacter.currentActionMode == CharacterCore.PlayerActionMode.SkillA, currentCharacter.CanUseSkillA());
        UpdateButtonColor(skillBButton, currentCharacter.currentActionMode == CharacterCore.PlayerActionMode.SkillB, currentCharacter.CanUseSkillB());
        UpdateButtonColor(skillCButton, currentCharacter.currentActionMode == CharacterCore.PlayerActionMode.SkillC, currentCharacter.CanUseSkillC());
        UpdateButtonColor(skillDButton, currentCharacter.currentActionMode == CharacterCore.PlayerActionMode.SkillD, currentCharacter.CanUseSkillD());
    }
    
    /// <summary>
    /// 更新按鈕顏色
    /// </summary>
    /// <param name="button">要更新的按鈕</param>
    /// <param name="isSelected">是否被選中</param>
    /// <param name="isAvailable">是否可用（技能專用）</param>
    private void UpdateButtonColor(Button button, bool isSelected, bool isAvailable = true)
    {
        if (button == null)
            return;
            
        ColorBlock colors = button.colors;
        
        if (!isAvailable)
        {
            // 技能不可用時顯示為灰色
            colors.normalColor = disabledButtonColor;
            colors.selectedColor = disabledButtonColor;
            button.interactable = false;
        }
        else if (isSelected)
        {
            // 被選中時顯示為選中顏色
            colors.normalColor = selectedButtonColor;
            colors.selectedColor = selectedButtonColor;
            button.interactable = true;
        }
        else
        {
            // 正常狀態
            colors.normalColor = normalButtonColor;
            colors.selectedColor = normalButtonColor;
            button.interactable = true;
        }
        
        button.colors = colors;
    }
}
