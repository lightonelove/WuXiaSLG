using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Serialization;


public class CharacterCore : MonoBehaviour
{
    // For Control //
    public float moveSpeed = 20f;
    public Transform cameraTransform;
    public float turnRate = 720;

    [Header("組件引用")]
    public CharacterMovement movementComponent;
    public CharacterResources characterResources;
    
    [Header("回合控制")]
    public float holdTimeToEndTurn = 1.0f;     // 長壓多少秒結束回合
    private float spaceKeyHoldTime = 0f;       // 空白鍵持續按住時間
    private bool isHoldingSpace = false;       // 是否正在按住空白鍵
    private bool hasTriggeredEndTurn = false;  // 是否已經觸發過結束回合
    
    
    
    public Animator CharacterControlAnimator;
    
    [Header("動畫根運動控制")]
    public AnimationRelativePos controllerAnimationRelativePos;
    public AnimationMoveScaler3D animationMoveScaler3D;
    
    [Header("技能配置")]
    public CombatSkill skillA;
    public CombatSkill skillB;
    public CombatSkill skillC;
    public CombatSkill skillD;

    public enum CharacterCoreState{ ControlState, ExcutionState, UsingSkill, ExecutingSkill, TurnComplete }

    public enum PlayerActionMode
    {
        None,           // 沒有選擇任何動作
        Move,           // 移動模式
        SkillTargeting, // 技能目標選擇模式
        SkillExecuting  // 技能執行模式
    }

    public CharacterCoreState nowState = CharacterCoreState.ControlState;
    public PlayerActionMode currentActionMode = PlayerActionMode.None;
    
    [Header("當前選擇的技能")]
    public CombatSkill currentSelectedSkill; // 當前選擇要執行的技能
    
    [Header("技能瞄準系統")]
    private Transform straightFrontTargetingAnchor; // 技能瞄準錨點
    private BoxCollider targetingCollider; // 瞄準用的BoxCollider
    private bool isSkillTargetValid = true; // 技能目標是否有效（沒有被Floor阻擋）
    public LayerMask floorLayerMask; // Floor圖層遮罩
    private Renderer cubeRenderer; // Cube的Renderer組件
    private Color originalCubeColor; // Cube的原始顏色
    private readonly Color blockedColor = new Color(0.5f, 0f, 0f, 1f); // 酒紅色
    public SkillTargetingCollisionDetector collisionDetector; // 碰撞檢測器
    
    void Start()
    {
        
        // 獲取組件引用
        if (movementComponent == null)
            movementComponent = GetComponent<CharacterMovement>();
        if (characterResources == null)
            characterResources = GetComponent<CharacterResources>();
        
        // 初始化AnimationRelativePos設定
        InitializeAnimationRelativePos();
        
        // 初始化AnimationMoveScaler3D設定
        InitializeAnimationMoveScaler3D();
        
        // 初始化技能瞄準系統
        InitializeSkillTargeting();
    }
    
    private void InitializeSkillTargeting()
    {
        // 尋找 StraightFrontTargetingAnchor
        straightFrontTargetingAnchor = transform.Find("StraightFrontTargetingAnchor");
        
        if (straightFrontTargetingAnchor != null)
        {
            // 尋找其下的 Cube 物件的 BoxCollider
            Transform cubeTransform = straightFrontTargetingAnchor.Find("Cube");
            if (cubeTransform != null)
            {
                targetingCollider = cubeTransform.GetComponent<BoxCollider>();
                cubeRenderer = cubeTransform.GetComponent<Renderer>();
                
                if (targetingCollider != null)
                {
                    // 儲存原始顏色
                    if (cubeRenderer != null && cubeRenderer.material != null)
                    {
                        originalCubeColor = cubeRenderer.material.color;
                    }
                    
                    // 初始化碰撞檢測器（已在 prefab 中設定引用）
                    if (collisionDetector != null)
                    {
                        collisionDetector.Initialize(this, floorLayerMask);
                    }
                    else
                    {
                    }
                    
                    // 確保初始時是隱藏的
                    straightFrontTargetingAnchor.gameObject.SetActive(false);
                }
            }
        }
        
        // 取得 Floor 圖層遮罩
        if (SLGCoreUI.Instance != null)
        {
            floorLayerMask = SLGCoreUI.Instance.floorLayerMask;
        }
        else
        {
            // 使用預設的 Floor 圖層
            floorLayerMask = LayerMask.GetMask("Floor");
        }
    }
    
    
    private void InitializeAnimationRelativePos()
    {
        // 檢查是否有AnimationRelativePos組件
        if (controllerAnimationRelativePos == null)
        {
            controllerAnimationRelativePos = GetComponent<AnimationRelativePos>();
        }
        
        if (controllerAnimationRelativePos != null)
        {
            // 確保AnimationRelativePos有正確的Animator引用
            if (controllerAnimationRelativePos.animator == null)
            {
                controllerAnimationRelativePos.animator = CharacterControlAnimator;
            }
            
            
        }
        else
        {
        }
    }
    
    private void InitializeAnimationMoveScaler3D()
    {
        // 檢查是否有AnimationMoveScaler3D組件
        if (animationMoveScaler3D == null)
        {
            animationMoveScaler3D = GetComponent<AnimationMoveScaler3D>();
        }
        
        if (animationMoveScaler3D != null)
        {
            // 確保AnimationMoveScaler3D有正確的Animator引用
            if (animationMoveScaler3D.animator == null)
            {
                animationMoveScaler3D.animator = CharacterControlAnimator;
            }
            
        }
        else
        {
        }
    }
    
    
    public void ControlUpdate()
    {
        // 更新移動狀態
        if (movementComponent != null)
        {
            movementComponent.UpdateMovement();
        }
    }
    
    
    /// <summary>
    /// 移動到指定位置
    /// </summary>
    /// <param name="destination">目標位置</param>
    public void MoveTo(Vector3 destination)
    {
        if (movementComponent != null)
        {
            movementComponent.MoveTo(destination);
        }
    }
    
    /// <summary>
    /// 停止移動
    /// </summary>
    public void StopMovement()
    {
        if (movementComponent != null)
        {
            movementComponent.StopMovement();
        }
    }
    
    
    /// <summary>
    /// 檢查是否可以移動到指定位置
    /// </summary>
    /// <param name="destination">目標位置</param>
    /// <returns>是否可以移動</returns>
    public bool CanMoveTo(Vector3 destination)
    {
        if (movementComponent != null)
        {
            return movementComponent.CanMoveTo(destination);
        }
        return false;
    }

    public bool CheckConfirm()
    {
        // 確認按鍵將改為滑鼠點擊
        return false;
    }
    
    public void ReFillAP()
    {
        if (characterResources != null)
        {
            characterResources.RefillAP();
        }
    }
    
    // Update is called once per frame
    void Update()
    {
        // 簡單的回合檢查：如果不是玩家回合，不允許任何操作
        if (CombatCore.Instance != null && !CombatCore.Instance.IsPlayerTurn())
        {
            return;
        }
        
        
        if (nowState == CharacterCoreState.ControlState)
        {
            ControlUpdate();
            
            // 處理長壓空白鍵結束回合
            HandleSpaceKeyInput();
            
            // 更新技能瞄準系統
            UpdateSkillTargeting();
        }
        else if (nowState == CharacterCoreState.ExcutionState)
        {
            // 在執行狀態下也應該可以用空白鍵強制結束回合
            HandleSpaceKeyInput();
        }
        else if (nowState == CharacterCoreState.UsingSkill)
        {
            AnimatorStateInfo stateInfo = CharacterControlAnimator.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.normalizedTime >= 1.0f)
            {
                // 技能動畫完成，清除AnimationMoveScaler3D狀態
                if (animationMoveScaler3D != null)
                {
                    animationMoveScaler3D.ClearEvaluate();
                }
                
                nowState = CharacterCoreState.ControlState;
            }
        }
        else
        {
            // 如果不在ControlState或ExcutionState，記錄原因
        }
    }
    
    
    
    /// <summary>
    /// 獲取玩家的真實位置（考慮 ExecutionState 時的 CharacterExecutor 位置）
    /// </summary>
    /// <returns>玩家的真實世界位置</returns>
    public Vector3 GetRealPosition()
    {
        // 如果在執行狀態且有 characterExecutor，使用 executor 的位置

        
        // 否則使用主物件的位置
        return transform.position;
    }
    
    /// <summary>
    /// 獲取玩家的真實 Transform（用於敵人追蹤）
    /// </summary>
    /// <returns>玩家的真實 Transform</returns>
    public Transform GetRealTransform()
    {
        // 如果在執行狀態且有 characterExecutor，使用 executor 的 transform

        // 否則使用主物件的 transform
        return transform;
    }
    
    /// <summary>
    /// 在回合結束時同步位置（將主物件位置更新為 CharacterExecutor 的位置）
    /// </summary>


    // 技能檢查方法將改為滑鼠/UI控制
    public bool CanUseSkillA()
    {
        return skillA != null && characterResources != null && characterResources.HasEnoughAP(skillA.SPCost);
    }
    
    public bool CanUseSkillB()
    {
        return skillB != null && characterResources != null && characterResources.HasEnoughAP(skillB.SPCost);
    }
    
    public bool CanUseSkillC()
    {
        return skillC != null && characterResources != null && characterResources.HasEnoughAP(skillC.SPCost);
    }
    
    public bool CanUseSkillD()
    {
        return skillD != null && characterResources != null && characterResources.HasEnoughAP(skillD.SPCost);
    }
    
    public void UseSkill(CombatSkill skill)
    {
        if (skill != null && characterResources != null && characterResources.HasEnoughAP(skill.SPCost))
        {
            nowState = CharacterCoreState.UsingSkill;
            
            CharacterControlAnimator.Play(skill.AnimationName);
            characterResources.ConsumeAP(skill.SPCost);
        }
    }
    
    /// <summary>
    /// 在指定位置執行技能
    /// </summary>
    /// <param name="targetLocation">技能目標位置</param>
    /// <param name="skill">要執行的技能</param>
    public void ExecuteSkillAtLocation(Vector3 targetLocation, CombatSkill skill)
    {
        if (skill != null && characterResources != null && characterResources.HasEnoughAP(skill.SPCost))
        {
            // 檢查技能路徑是否有效（沒有被 Floor 層阻擋）
            if (!isSkillTargetValid)
            {
                return;
            }
            
            // 讓角色面向目標位置
            Vector3 lookDirection = targetLocation - transform.position;
            lookDirection.y = 0;
            if (lookDirection != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(lookDirection);
            }
            
            // 設定AnimationMoveScaler3D的目標位置（用於技能動畫位移縮放）
            if (animationMoveScaler3D != null)
            {
                animationMoveScaler3D.SetClickPosition(targetLocation);
            }
            
            nowState = CharacterCoreState.UsingSkill;
            
            // 播放技能動畫
            CharacterControlAnimator.Play(skill.AnimationName);
            characterResources.ConsumeAP(skill.SPCost);
            
            // 隱藏技能瞄準系統
            if (straightFrontTargetingAnchor != null)
            {
                straightFrontTargetingAnchor.gameObject.SetActive(false);
            }
            
            // 重置動作模式
            currentActionMode = PlayerActionMode.None;
        }
    }
    
    public void ConfirmTurn()
    {
        CombatCore.Instance.ConfirmAction();
        nowState = CharacterCoreState.ExcutionState;
    }
    
    /// <summary>
    /// 預覽到指定位置的路徑（不執行移動）
    /// </summary>
    /// <param name="destination">目標位置</param>
    public void PreviewPath(Vector3 destination)
    {
        if (movementComponent != null)
        {
            movementComponent.PreviewPath(destination);
        }
    }
    
    /// <summary>
    /// 清除路徑顯示
    /// </summary>
    public void ClearPathDisplay()
    {
        if (movementComponent != null)
        {
            movementComponent.ClearPathDisplay();
        }
    }
    
    
    
    
    
    
    
    
    
    /// <summary>
    /// 處理空白鍵輸入來結束回合
    /// </summary>
    private void HandleSpaceKeyInput()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) 
        {
            return;
        }
        
        // 檢查是否在執行移動或使用技能中，如果是則不允許結束回合
        bool isCurrentlyMoving = movementComponent != null && movementComponent.isMoving;
        if (isCurrentlyMoving || nowState == CharacterCoreState.UsingSkill)
        {
            // 如果正在長壓但現在不允許結束回合，重置長壓狀態
            if (isHoldingSpace)
            {
                isHoldingSpace = false;
                spaceKeyHoldTime = 0f;
                hasTriggeredEndTurn = false;
            }
            return;
        }
        
        // 檢查空白鍵是否被按下
        bool spacePressed = keyboard.spaceKey.isPressed;
        
        if (spacePressed)
        {
            if (!isHoldingSpace)
            {
                // 開始長壓
                isHoldingSpace = true;
                spaceKeyHoldTime = 0f;
                
                // Print當前戰鬥狀態信息
                string currentCombatEntityName = "None";
                if (CombatCore.Instance != null && CombatCore.Instance.currentRoundEntity != null)
                {
                    currentCombatEntityName = CombatCore.Instance.currentRoundEntity.Name;
                }
                
            }
            else if (!hasTriggeredEndTurn)  // 只有在還沒觸發過的情況下才繼續處理
            {
                // 持續長壓，累計時間
                spaceKeyHoldTime += Time.deltaTime;
                
                // 檢查是否達到結束回合的時間
                if (spaceKeyHoldTime >= holdTimeToEndTurn)
                {
                    EndTurnBySpaceKey();
                    hasTriggeredEndTurn = true;  // 標記已經觸發過
                }
            }
        }
        else
        {
            // 空白鍵被釋放
            if (isHoldingSpace)
            {
                isHoldingSpace = false;
                spaceKeyHoldTime = 0f;
                hasTriggeredEndTurn = false;  // 釋放時重置觸發標記
            }
        }
    }
    
    /// <summary>
    /// 通過空白鍵結束回合
    /// </summary>
    private void EndTurnBySpaceKey()
    {
        
        // 重置長壓狀態
        isHoldingSpace = false;
        spaceKeyHoldTime = 0f;
        
        // 結束當前回合
        if (CombatCore.Instance != null)
        {
            CombatCore.Instance.EndCurrentEntityTurn();
        }
    }
    
    /// <summary>
    /// 獲取長壓空白鍵的進度（0-1）
    /// </summary>
    /// <returns>進度值，0表示未開始，1表示完成</returns>
    public float GetSpaceKeyHoldProgress()
    {
        if (!isHoldingSpace) return 0f;
        return Mathf.Clamp01(spaceKeyHoldTime / holdTimeToEndTurn);
    }
    
    /// <summary>
    /// 是否正在長壓空白鍵
    /// </summary>
    /// <returns>是否正在長壓</returns>
    public bool IsHoldingSpaceKey()
    {
        return isHoldingSpace;
    }
    
    /// <summary>
    /// 更新技能瞄準系統
    /// </summary>
    private void UpdateSkillTargeting()
    {
        // 只在 SkillTargeting 模式下運作
        if (currentActionMode != PlayerActionMode.SkillTargeting || straightFrontTargetingAnchor == null)
        {
            // 隱藏瞄準系統
            if (straightFrontTargetingAnchor != null && straightFrontTargetingAnchor.gameObject.activeSelf)
            {
                straightFrontTargetingAnchor.gameObject.SetActive(false);
            }
            return;
        }
        
        // 顯示瞄準系統
        if (!straightFrontTargetingAnchor.gameObject.activeSelf)
        {
            straightFrontTargetingAnchor.gameObject.SetActive(true);
        }
        
        // 取得滑鼠在地面的位置
        if (SLGCoreUI.Instance != null && SLGCoreUI.Instance.IsMouseOverFloor())
        {
            Vector3 mouseWorldPos = SLGCoreUI.Instance.GetMouseFloorPosition();
            
            if (mouseWorldPos != Vector3.zero)
            {
                // 計算方向（用於角色旋轉）
                Vector3 direction = mouseWorldPos - transform.position;
                direction.y = 0; // 保持水平
                
                // 旋轉整個 CharacterCore 面向目標（即時跟隨）
                if (direction != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(direction);
                    transform.rotation = targetRotation;
                }
                
                // 計算 StraightFrontTargetingAnchor 到滑鼠位置的距離（用於縮放）
                Vector3 anchorWorldPos = straightFrontTargetingAnchor.position;
                Vector3 anchorToMouse = mouseWorldPos - anchorWorldPos;
                anchorToMouse.y = 0; // 保持水平
                float anchorDistance = anchorToMouse.magnitude + 0.5f;
                
                // Cube 在本地座標 (0, 0, 0.5)，表示從 Anchor 中心延伸 0.5 個單位
                // 所以縮放倍率 = 實際距離 / Cube 的本地 Z 偏移 * 0.5（修正倍率）
                float cubeLocalOffset = 0.5f; // Cube 在本地座標的 Z 偏移
                float scaleZ = (anchorDistance / cubeLocalOffset) * 0.5f;
                
                // 設定縮放，保持 X 和 Y 不變
                straightFrontTargetingAnchor.localScale = new Vector3(1f, 1f, scaleZ);
                
                // 觸發器系統會自動檢測碰撞，無需手動調用
            }
        }
    }
    
    /// <summary>
    /// 技能瞄準碰撞狀態變化回調（從 SkillTargetingCollisionDetector 調用）
    /// </summary>
    /// <param name="collidingObjects">當前碰撞的物件集合</param>
    public void OnTargetingCollisionChanged(HashSet<Collider> collidingObjects)
    {
        bool hasCollision = collidingObjects.Count > 0;
        bool newTargetValid = !hasCollision;
        
        // 只在狀態改變時更新顏色和輸出 Debug
        if (newTargetValid != isSkillTargetValid)
        {
            isSkillTargetValid = newTargetValid;
            
            if (hasCollision)
            {
                
                // 變更Cube顏色為酒紅色
                if (cubeRenderer != null && cubeRenderer.material != null)
                {
                    cubeRenderer.material.color = blockedColor;
                }
            }
            else
            {
                
                // 恢復Cube的原始顏色
                if (cubeRenderer != null && cubeRenderer.material != null)
                {
                    cubeRenderer.material.color = originalCubeColor;
                }
            }
        }
    }

    
}