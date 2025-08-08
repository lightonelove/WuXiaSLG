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
        
        Debug.Log("[SLGCoreUI] Start() called - gameObject is active: " + gameObject.activeInHierarchy);
        
        // 初始化cursor相关组件
        if (cursorImage != null)
        {
            cursorRectTransform = cursorImage.GetComponent<RectTransform>();
            parentCanvas = GetComponentInParent<Canvas>();
            
            // 讓CustomCursor不擋住UI按鈕點擊
            cursorImage.raycastTarget = false;
            
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
        // 檢查滑鼠是否在UI元素上，如果是則不處理地板點擊
        if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            return;
            
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
            // 處理技能目標選擇
            else if (currentCharacter.currentActionMode == CharacterCore.PlayerActionMode.SkillTargeting)
            {
                if (IsMouseOverFloor())
                {
                    Vector3 targetPosition = GetMouseFloorPosition();
                    if (targetPosition != Vector3.zero)
                    {
                        // 執行對應的技能
                        ExecuteSkillAtTargetLocation(currentCharacter, targetPosition);
                    }
                }
            }
        }
        
        // 檢查滑鼠右鍵點擊
        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            // 如果在Move/Skill狀態，取消回到None狀態
            if (currentCharacter.currentActionMode != CharacterCore.PlayerActionMode.None)
            {
                SetActionMode(CharacterCore.PlayerActionMode.None);
            }
            else
            {
                // 如果已經是None狀態，則確認回合
                currentCharacter.ConfirmTurn();
            }
        }
    }
    
    void Awake()
    {
        Debug.Log("[SLGCoreUI] Awake() called - gameObject is active: " + gameObject.activeInHierarchy);
    }
    
    void OnEnable()
    {
        Debug.Log("[SLGCoreUI] OnEnable() called");
    }
    
    void OnDisable()
    {
        Debug.Log("[SLGCoreUI] OnDisable() called - Stack Trace:");
        Debug.Log(System.Environment.StackTrace);
    }
    
    void OnDestroy()
    {
        Debug.Log("[SLGCoreUI] OnDestroy() called");
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
                ToggleActionMode(CharacterCore.PlayerActionMode.Move);
            });
        }
        else
        {
        }
        
        if (skillAButton != null)
        {
            skillAButton.onClick.AddListener(() => {
                ExecuteSkillButton('A');
            });
        }
        else
        {
        }
        
        if (skillBButton != null)
        {
            skillBButton.onClick.AddListener(() => {
                ExecuteSkillButton('B');
            });
        }
        else
        {
        }
        
        if (skillCButton != null)
        {
            skillCButton.onClick.AddListener(() => {
                ExecuteSkillButton('C');
            });
        }
        else
        {
        }
        
        if (skillDButton != null)
        {
            skillDButton.onClick.AddListener(() => {
                ExecuteSkillButton('D');
            });
        }
        else
        {
        }
        
    }
    
    /// <summary>
    /// 切換動作模式：如果已經是該模式則取消回到None，否則設定為該模式
    /// </summary>
    /// <param name="mode">要切換的動作模式</param>
    private void ToggleActionMode(CharacterCore.PlayerActionMode mode)
    {
        // 檢查是否有戰鬥核心和當前玩家
        if (CombatCore.Instance == null)
            return;
        
        if (!CombatCore.Instance.IsPlayerTurn())
            return;
            
        CombatEntity currentEntity = CombatCore.Instance.GetCurrentTurnEntity();
        if (currentEntity == null)
            return;
            
        CharacterCore currentCharacter = currentEntity.GetComponent<CharacterCore>();
        if (currentCharacter == null)
            return;
        
        if (currentCharacter.nowState != CharacterCore.CharacterCoreState.ControlState)
            return;
        
        // 移除舊的技能模式處理邏輯，因為現在由ExecuteSkillButton處理
        
        // 如果目前已經是該模式，則取消回到None；否則設定為該模式
        CharacterCore.PlayerActionMode newMode = (currentCharacter.currentActionMode == mode) 
            ? CharacterCore.PlayerActionMode.None 
            : mode;
        
        SetActionMode(newMode);
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
        
        // 根據模式更新FloorIndicator顏色
        UpdateFloorIndicatorForMode(mode);
        
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
        UpdateButtonColor(skillAButton, 
            currentCharacter.currentActionMode == CharacterCore.PlayerActionMode.SkillTargeting && currentCharacter.currentSelectedSkill == currentCharacter.skillA, 
            currentCharacter.CanUseSkillA());
        UpdateButtonColor(skillBButton, 
            currentCharacter.currentActionMode == CharacterCore.PlayerActionMode.SkillTargeting && currentCharacter.currentSelectedSkill == currentCharacter.skillB, 
            currentCharacter.CanUseSkillB());
        UpdateButtonColor(skillCButton, 
            currentCharacter.currentActionMode == CharacterCore.PlayerActionMode.SkillTargeting && currentCharacter.currentSelectedSkill == currentCharacter.skillC, 
            currentCharacter.CanUseSkillC());
        UpdateButtonColor(skillDButton, 
            currentCharacter.currentActionMode == CharacterCore.PlayerActionMode.SkillTargeting && currentCharacter.currentSelectedSkill == currentCharacter.skillD, 
            currentCharacter.CanUseSkillD());
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
    
    /// <summary>
    /// 執行技能按鈕點擊
    /// </summary>
    /// <param name="skillType">技能類型 ('A', 'B', 'C', 'D')</param>
    private void ExecuteSkillButton(char skillType)
    {
        // 檢查是否有戰鬥核心和當前玩家
        if (CombatCore.Instance == null || !CombatCore.Instance.IsPlayerTurn())
            return;
            
        CombatEntity currentEntity = CombatCore.Instance.GetCurrentTurnEntity();
        if (currentEntity == null)
            return;
            
        CharacterCore currentCharacter = currentEntity.GetComponent<CharacterCore>();
        if (currentCharacter == null || currentCharacter.nowState != CharacterCore.CharacterCoreState.ControlState)
            return;
            
        // 檢查對應技能是否可用
        bool canUse = false;
        CombatSkill skill = null;
        
        switch (skillType)
        {
            case 'A':
                canUse = currentCharacter.CanUseSkillA();
                skill = currentCharacter.skillA;
                break;
            case 'B':
                canUse = currentCharacter.CanUseSkillB();
                skill = currentCharacter.skillB;
                break;
            case 'C':
                canUse = currentCharacter.CanUseSkillC();
                skill = currentCharacter.skillC;
                break;
            case 'D':
                canUse = currentCharacter.CanUseSkillD();
                skill = currentCharacter.skillD;
                break;
        }
        
        // 如果技能可用，進入目標選擇模式
        if (canUse && skill != null)
        {
            Debug.Log($"進入技能 {skill.SkillName} 的目標選擇模式");
            
            // 設定當前選擇的技能和目標選擇模式
            currentCharacter.currentSelectedSkill = skill;
            currentCharacter.currentActionMode = CharacterCore.PlayerActionMode.SkillTargeting;
            
            // 更新FloorIndicator顏色
            UpdateFloorIndicatorForMode(CharacterCore.PlayerActionMode.SkillTargeting);
        }
        else
        {
            Debug.Log($"無法執行技能: AP不足或技能未設定");
        }
    }
    
    /// <summary>
    /// 在目標位置執行技能
    /// </summary>
    /// <param name="character">執行技能的角色</param>
    /// <param name="targetLocation">目標位置</param>
    private void ExecuteSkillAtTargetLocation(CharacterCore character, Vector3 targetLocation)
    {
        if (character == null || character.currentSelectedSkill == null)
            return;
            
        CombatSkill skill = character.currentSelectedSkill;
        
        Debug.Log($"在位置 {targetLocation} 執行技能: {skill.SkillName}");
        
        // 呼叫CharacterCore的ExecuteSkillAtLocation方法
        character.ExecuteSkillAtLocation(targetLocation, skill);
        
        // 技能執行後更新FloorIndicator顏色 (因為CharacterCore已經重置為None狀態)
        UpdateFloorIndicatorForMode(CharacterCore.PlayerActionMode.None);
    }
    
    /// <summary>
    /// 根據動作模式更新FloorIndicator顏色
    /// </summary>
    /// <param name="mode">當前動作模式</param>
    private void UpdateFloorIndicatorForMode(CharacterCore.PlayerActionMode mode)
    {
        if (floorMouseIndicator == null) return;
        
        int colorMode;
        
        // 根據不同模式設定不同顏色
        if (mode == CharacterCore.PlayerActionMode.None)
        {
            colorMode = 0; // 灰色
        }
        else if (mode == CharacterCore.PlayerActionMode.Move)
        {
            colorMode = 1; // 綠色
        }
        else if (mode == CharacterCore.PlayerActionMode.SkillTargeting)
        {
            colorMode = 2; // 紅色
        }
        else
        {
            colorMode = 0; // 其他情況默認灰色
        }
        
        // 設定指示器顏色
        floorMouseIndicator.SetIndicatorColorMode(colorMode);
    }
}
