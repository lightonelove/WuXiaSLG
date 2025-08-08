using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 角色技能系統 - 負責技能管理、瞄準系統、技能執行
/// </summary>
public class CharacterSkills : MonoBehaviour
{
    [Header("技能配置")]
    public CombatSkill skillA;
    public CombatSkill skillB;
    public CombatSkill skillC;
    public CombatSkill skillD;
    
    [Header("當前選擇的技能")]
    public CombatSkill currentSelectedSkill; // 當前選擇要執行的技能
    
    [Header("技能瞄準系統")]
    public Transform straightFrontTargetingAnchor; // 技能瞄準錨點
    private BoxCollider targetingCollider; // 瞄準用的BoxCollider
    private bool isSkillTargetValid = true; // 技能目標是否有效（沒有被Floor阻擋）
    public LayerMask floorLayerMask; // Floor圖層遮罩
    private Renderer cubeRenderer; // Cube的Renderer組件
    private Color originalCubeColor; // Cube的原始顏色
    private readonly Color blockedColor = new Color(0.5f, 0f, 0f, 1f); // 酒紅色
    public SkillTargetingCollisionDetector collisionDetector; // 碰撞檢測器
    
    // 對其他組件的引用
    public CharacterCore characterCore;
    public CharacterResources characterResources;
    
    void Start()
    {
        // 初始化技能瞄準系統
        InitializeSkillTargeting();
    }
    
    /// <summary>
    /// 初始化技能瞄準系統
    /// </summary>
    private void InitializeSkillTargeting()
    {
        // 尋找 StraightFrontTargetingAnchor
        
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
                        collisionDetector.Initialize(characterCore, floorLayerMask);
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
    
    /// <summary>
    /// 檢查是否可以使用技能A
    /// </summary>
    /// <returns>是否可以使用</returns>
    public bool CanUseSkillA()
    {
        return skillA != null && characterResources != null && characterResources.HasEnoughAP(skillA.SPCost);
    }
    
    /// <summary>
    /// 檢查是否可以使用技能B
    /// </summary>
    /// <returns>是否可以使用</returns>
    public bool CanUseSkillB()
    {
        return skillB != null && characterResources != null && characterResources.HasEnoughAP(skillB.SPCost);
    }
    
    /// <summary>
    /// 檢查是否可以使用技能C
    /// </summary>
    /// <returns>是否可以使用</returns>
    public bool CanUseSkillC()
    {
        return skillC != null && characterResources != null && characterResources.HasEnoughAP(skillC.SPCost);
    }
    
    /// <summary>
    /// 檢查是否可以使用技能D
    /// </summary>
    /// <returns>是否可以使用</returns>
    public bool CanUseSkillD()
    {
        return skillD != null && characterResources != null && characterResources.HasEnoughAP(skillD.SPCost);
    }
    
    /// <summary>
    /// 使用技能
    /// </summary>
    /// <param name="skill">要使用的技能</param>
    public void UseSkill(CombatSkill skill)
    {
        if (skill != null && characterResources != null && characterResources.HasEnoughAP(skill.SPCost))
        {
            if (characterCore != null)
            {
                characterCore.nowState = CharacterCore.CharacterCoreState.UsingSkill;
                
                if (characterCore.CharacterControlAnimator != null)
                {
                    characterCore.CharacterControlAnimator.Play(skill.AnimationName);
                }
                
                characterResources.ConsumeAP(skill.SPCost);
            }
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
            if (characterCore != null && characterCore.animationMoveScaler3D != null)
            {
                characterCore.animationMoveScaler3D.SetClickPosition(targetLocation);
            }
            
            if (characterCore != null)
            {
                characterCore.nowState = CharacterCore.CharacterCoreState.UsingSkill;
                
                // 播放技能動畫
                if (characterCore.CharacterControlAnimator != null)
                {
                    characterCore.CharacterControlAnimator.Play(skill.AnimationName);
                }
                characterResources.ConsumeAP(skill.SPCost);
                
                // 隱藏技能瞄準系統
                if (straightFrontTargetingAnchor != null)
                {
                    straightFrontTargetingAnchor.gameObject.SetActive(false);
                }
                
                // 重置動作模式
                characterCore.currentActionMode = CharacterCore.PlayerActionMode.None;
            }
        }
    }
    
    /// <summary>
    /// 更新技能瞄準系統
    /// </summary>
    public void UpdateSkillTargeting()
    {
        Debug.Log("Targeting!");
        if (characterCore == null) return;
        
        // 只在 SkillTargeting 模式下運作
        Debug.Log("Targeting0");
        if (characterCore.currentActionMode != CharacterCore.PlayerActionMode.SkillTargeting)
        {
            // 隱藏瞄準系統
            if (straightFrontTargetingAnchor.gameObject.activeSelf)
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
        Debug.Log("Targeting1");
        if (SLGCoreUI.Instance != null && SLGCoreUI.Instance.IsMouseOverFloor())
        {
            Vector3 mouseWorldPos = SLGCoreUI.Instance.GetMouseFloorPosition();
            Debug.Log("Targeting2");
            if (mouseWorldPos != Vector3.zero)
            {
                // 計算方向（用於角色旋轉）
                Vector3 direction = mouseWorldPos - characterCore.transform.position;
                direction.y = 0; // 保持水平
                Debug.Log("Targeting3");
                // 旋轉整個 CharacterCore 面向目標（即時跟隨）
                if (direction != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(direction);
                    characterCore.transform.rotation = targetRotation;
                    Debug.Log("Targeting4");
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
                Debug.Log("Targeting5");
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
    
    /// <summary>
    /// 獲取技能目標是否有效
    /// </summary>
    /// <returns>技能目標是否有效</returns>
    public bool IsSkillTargetValid()
    {
        return isSkillTargetValid;
    }
}