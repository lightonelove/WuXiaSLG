using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class CharacterCore : MonoBehaviour
{
    [Header("組件引用")]
    public CharacterMovement movementComponent;
    public CharacterResources characterResources;
    public CharacterSkills skillsComponent;
    
    [Header("回合控制")]
    public float holdTimeToEndTurn = 1.0f;     // 長壓多少秒結束回合
    private float spaceKeyHoldTime = 0f;       // 空白鍵持續按住時間
    private bool isHoldingSpace = false;       // 是否正在按住空白鍵
    private bool hasTriggeredEndTurn = false;  // 是否已經觸發過結束回合
    
    public Animator CharacterControlAnimator;
    
    [Header("動畫根運動控制")]
    public AnimationMoveScaler3D animationMoveScaler3D;

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
    
    void Start()
    {
        // 獲取組件引用
        if (movementComponent == null)
            movementComponent = GetComponent<CharacterMovement>();
        if (characterResources == null)
            characterResources = GetComponent<CharacterResources>();
        if (skillsComponent == null)
            skillsComponent = GetComponent<CharacterSkills>();
        
        // 初始化AnimationMoveScaler3D設定
        InitializeAnimationMoveScaler3D();
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
            if (skillsComponent != null)
            {
                skillsComponent.UpdateSkillTargeting();
            }
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
    }
    
    /// <summary>
    /// 獲取玩家的真實位置（考慮 ExecutionState 時的 CharacterExecutor 位置）
    /// </summary>
    /// <returns>玩家的真實世界位置</returns>
    public Vector3 GetRealPosition()
    {
        return transform.position;
    }
    
    /// <summary>
    /// 獲取玩家的真實 Transform（用於敵人追蹤）
    /// </summary>
    /// <returns>玩家的真實 Transform</returns>
    public Transform GetRealTransform()
    {
        return transform;
    }
    
    // 技能檢查方法 - 委託給 CharacterSkills 組件
    public bool CanUseSkillA()
    {
        return skillsComponent != null && skillsComponent.CanUseSkillA();
    }
    
    public bool CanUseSkillB()
    {
        return skillsComponent != null && skillsComponent.CanUseSkillB();
    }
    
    public bool CanUseSkillC()
    {
        return skillsComponent != null && skillsComponent.CanUseSkillC();
    }
    
    public bool CanUseSkillD()
    {
        return skillsComponent != null && skillsComponent.CanUseSkillD();
    }
    
    /// <summary>
    /// 在指定位置執行技能
    /// </summary>
    /// <param name="targetLocation">技能目標位置</param>
    /// <param name="skill">要執行的技能</param>
    public void ExecuteSkillAtLocation(Vector3 targetLocation, CombatSkill skill)
    {
        if (skillsComponent != null)
        {
            skillsComponent.ExecuteSkillAtLocation(targetLocation, skill);
        }
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
    /// 技能瞄準碰撞狀態變化回調（委託給 CharacterSkills 組件處理）
    /// </summary>
    /// <param name="collidingObjects">當前碰撞的物件集合</param>
    public void OnTargetingCollisionChanged(HashSet<Collider> collidingObjects)
    {
        if (skillsComponent != null)
        {
            skillsComponent.OnTargetingCollisionChanged(collidingObjects);
        }
    }
}