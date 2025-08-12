using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Events;
#endif

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
    public SectorMeshGenerator standStillTargetingAnchor;
    
    // 瞄準模式狀態追蹤（避免重複生成）
    private SkillTargetingMode? lastTargetingMode = null; // 上次的瞄準模式
    private CombatSkill lastSelectedSkill = null; // 上次選擇的技能
    private System.Collections.Generic.HashSet<Collider> TargetingCollidingObjects = new System.Collections.Generic.HashSet<Collider>();
    
    // Floor 層碰撞檢測（用於 FrontDash 模式）
    private System.Collections.Generic.HashSet<Collider> FloorCollidingObjects = new System.Collections.Generic.HashSet<Collider>();
    
    // 追蹤已被標記為瞄準目標的 TargetedIndicator
    private System.Collections.Generic.HashSet<TargetedIndicator> currentTargets = new System.Collections.Generic.HashSet<TargetedIndicator>();
    
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
                    
                    
                    // 確保初始時是隱藏的
                    straightFrontTargetingAnchor.gameObject.SetActive(false);
                }
            }
        }
        
        // 初始化 StandStill 扇形瞄準器
        if (standStillTargetingAnchor != null)
        {
            // 確保初始時是隱藏的
            standStillTargetingAnchor.SetVisible(false);
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
            
            // 計算考慮距離限制的實際目標位置
            Vector3 adjustedTargetLocation = CalculateAdjustedTargetLocation(targetLocation, skill);
            
            // 讓角色面向目標位置
            Vector3 lookDirection = adjustedTargetLocation - transform.position;
            lookDirection.y = 0;
            if (lookDirection != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(lookDirection);
            }
            
            // 設定AnimationMoveScaler3D的目標位置（用於技能動畫位移縮放）
            if (characterCore != null && characterCore.animationMoveScaler3D != null)
            {
                characterCore.animationMoveScaler3D.SetClickPosition(adjustedTargetLocation);
                Debug.Log($"[CharacterSkill] Animation target set to: {adjustedTargetLocation} (original: {targetLocation})");
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
                
                // 隱藏所有技能瞄準系統
                if (straightFrontTargetingAnchor != null)
                {
                    straightFrontTargetingAnchor.gameObject.SetActive(false);
                }
                if (standStillTargetingAnchor != null)
                {
                    standStillTargetingAnchor.SetVisible(false);
                }
                
                // 重置動作模式
                characterCore.currentActionMode = CharacterCore.PlayerActionMode.None;
            }
        }
    }
    
    // 追蹤上次的動作模式，避免重複清除
    private CharacterCore.PlayerActionMode? lastActionMode = null;
    
    /// <summary>
    /// 更新技能瞄準系統
    /// </summary>
    public void UpdateSkillTargeting()
    {
        if (characterCore == null) return;
        
        // 檢查動作模式是否改變
        bool actionModeChanged = lastActionMode != characterCore.currentActionMode;
        lastActionMode = characterCore.currentActionMode;
        
        // 只在 SkillTargeting 模式下運作
        if (characterCore.currentActionMode != CharacterCore.PlayerActionMode.SkillTargeting)
        {
            // 只在模式改變時才執行清除操作（避免每 frame 重複執行）
            if (actionModeChanged)
            {
                // 隱藏所有瞄準系統
                if (straightFrontTargetingAnchor != null && straightFrontTargetingAnchor.gameObject.activeSelf)
                {
                    straightFrontTargetingAnchor.gameObject.SetActive(false);
                }
                if (standStillTargetingAnchor != null)
                {
                    standStillTargetingAnchor.SetVisible(false);
                }
                
                // 重置狀態追蹤（當退出瞄準模式時）
                lastTargetingMode = null;
                lastSelectedSkill = null;
                
                // 清除所有目標指示器（只在退出瞄準模式時執行一次）
                ClearAllTargetIndicators();
                
                // 清除所有碰撞狀態（包括 Floor 碰撞和目標碰撞）
                ClearAllCollidingObjects();
            }
            return;
        }
        
        // 根據當前選擇的技能瞄準模式來更新
        if (currentSelectedSkill != null)
        {
            switch (currentSelectedSkill.TargetingMode)
            {
                case SkillTargetingMode.FrontDash:
                    UpdateFrontDashTargeting();
                    break;
                case SkillTargetingMode.StandStill:
                    UpdateStandStillTargeting();
                    break;
                default:
                    UpdateFrontDashTargeting(); // 預設使用 FrontDash
                    break;
            }
        }
        else
        {
            // 如果沒有選擇技能，使用預設的 FrontDash 瞄準
            UpdateFrontDashTargeting();
        }
    }
    
    /// <summary>
    /// 更新 FrontDash 瞄準模式（前方衝刺）
    /// </summary>
    private void UpdateFrontDashTargeting()
    {
        // 檢查是否需要清空碰撞狀態（模式或技能改變時）
        bool targetingModeChanged = lastTargetingMode != SkillTargetingMode.FrontDash;
        bool skillChanged = lastSelectedSkill != currentSelectedSkill;
        bool needsClearCollision = targetingModeChanged || skillChanged;
        
        // 更新狀態追蹤
        lastTargetingMode = SkillTargetingMode.FrontDash;
        lastSelectedSkill = currentSelectedSkill;
        
        // 如果模式或技能改變，清空之前的碰撞狀態
        if (needsClearCollision)
        {
            ClearAllCollidingObjects();
        }
        
        // 隱藏 StandStill 瞄準器
        if (standStillTargetingAnchor != null)
        {
            standStillTargetingAnchor.SetVisible(false);
        }
        
        // 顯示 FrontDash 瞄準系統
        if (!straightFrontTargetingAnchor.gameObject.activeSelf)
        {
            straightFrontTargetingAnchor.gameObject.SetActive(true);
        }
        
        // 如果是固定距離模式，只在模式或技能改變時設定一次 Collider 大小
        if (currentSelectedSkill != null && currentSelectedSkill.IsFixedRange && needsClearCollision)
        {
            float targetDistance = currentSelectedSkill.SkillRange;
            float cubeLocalOffset = 0.5f; // Cube 在本地座標的 Z 偏移
            float scaleZ = (targetDistance / cubeLocalOffset) * 0.5f;
            
            // 設定固定縮放
            straightFrontTargetingAnchor.localScale = new Vector3(1f, 1f, scaleZ);
            Debug.Log($"[CharacterSkill] Fixed range mode set: {targetDistance:F2}");
        }
        
        // 取得滑鼠在地面的位置
        if (SLGCoreUI.Instance != null && SLGCoreUI.Instance.IsMouseOverFloor())
        {
            Vector3 mouseWorldPos = SLGCoreUI.Instance.GetMouseFloorPosition();
            if (mouseWorldPos != Vector3.zero)
            {
                // 計算方向（用於角色旋轉）
                Vector3 direction = mouseWorldPos - characterCore.transform.position;
                direction.y = 0; // 保持水平
                // 旋轉整個 CharacterCore 面向目標（即時跟隨）
                if (direction != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(direction);
                    characterCore.transform.rotation = targetRotation;
                }
                
                // 只有在非固定距離模式下才更新 Collider 大小
                if (currentSelectedSkill == null || !currentSelectedSkill.IsFixedRange)
                {
                    // 跟隨滑鼠模式：計算滑鼠距離並限制最大值
                    Vector3 characterPos = characterCore.transform.position;
                    Vector3 toMouse = mouseWorldPos - characterPos;
                    toMouse.y = 0; // 保持水平
                    float mouseDistance = toMouse.magnitude;
                    
                    float maxRange = (currentSelectedSkill != null) ? currentSelectedSkill.SkillRange : 5f;
                    float targetDistance = Mathf.Min(mouseDistance, maxRange);
                    
                    // 計算 Anchor 的縮放（Cube 在本地座標 (0, 0, 0.5)）
                    float cubeLocalOffset = 0.5f; // Cube 在本地座標的 Z 偏移
                    float scaleZ = (targetDistance / cubeLocalOffset) * 0.5f;
                    
                    // 設定縮放，保持 X 和 Y 不變
                    straightFrontTargetingAnchor.localScale = new Vector3(1f, 1f, scaleZ);
                }
                // 觸發器系統會自動檢測碰撞，無需手動調用
            }
        }
    }
    
    /// <summary>
    /// 更新 StandStill 瞄準模式（原地施放）
    /// </summary>
    private void UpdateStandStillTargeting()
    {
        // 隱藏 FrontDash 瞄準器
        if (straightFrontTargetingAnchor != null && straightFrontTargetingAnchor.gameObject.activeSelf)
        {
            straightFrontTargetingAnchor.gameObject.SetActive(false);
        }
        
        // 檢查是否需要重新生成扇形（只在模式或技能改變時才生成）
        bool targetingModeChanged = lastTargetingMode != SkillTargetingMode.StandStill;
        bool skillChanged = lastSelectedSkill != currentSelectedSkill;
        bool needsRegeneration = targetingModeChanged || skillChanged;
        
        // 如果模式或技能改變，清空之前的碰撞狀態
        if (needsRegeneration)
        {
            ClearAllCollidingObjects();
        }
        
        // 更新狀態追蹤
        lastTargetingMode = SkillTargetingMode.StandStill;
        lastSelectedSkill = currentSelectedSkill;
        
        // 顯示 StandStill 扇形瞄準器
        if (standStillTargetingAnchor != null)
        {
            standStillTargetingAnchor.SetVisible(true);
            
            // 只在需要重新生成時才設定角度和距離參數（避免每 frame 重新生成 mesh）
            if (needsRegeneration && currentSelectedSkill != null)
            {
                standStillTargetingAnchor.SetAngle(currentSelectedSkill.SkillAngle);
                standStillTargetingAnchor.SetRadius(currentSelectedSkill.SkillRange);
            }
            
            // 取得滑鼠在地面的位置，用於旋轉扇形朝向（這部分仍需要每 frame 更新）
            if (SLGCoreUI.Instance != null && SLGCoreUI.Instance.IsMouseOverFloor())
            {
                Vector3 mouseWorldPos = SLGCoreUI.Instance.GetMouseFloorPosition();
                if (mouseWorldPos != Vector3.zero)
                {
                    // 計算方向（用於角色和扇形旋轉）
                    Vector3 direction = mouseWorldPos - characterCore.transform.position;
                    direction.y = 0; // 保持水平
                    
                    // 旋轉整個 CharacterCore 面向目標（即時跟隨）
                    if (direction != Vector3.zero)
                    {
                        Quaternion targetRotation = Quaternion.LookRotation(direction);
                        characterCore.transform.rotation = targetRotation;
                    }
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
    
    /// <summary>
    /// 技能碰撞檢測：當有物件進入 Trigger 時
    /// </summary>
    /// <param name="other">進入的物件</param>
    public void OnTargetingTriggerEnter(Collider other)
    {
        // 檢查是否碰撞到自己（通過 CombatEntity 判斷）
        if (IsSelfEntity(other))
        {
            Debug.Log($"[CharacterSkill] Ignoring self collision: {other.name}");
            return;
        }
        
        // 檢查是否為 Floor 層物件
        bool isFloorObject = IsInLayerMask(other.gameObject, floorLayerMask);
        
        if (isFloorObject)
        {
            // Floor 層碰撞處理（影響 FrontDash 模式的有效性）
            FloorCollidingObjects.Add(other);
            UpdateSkillTargetValidity();
            Debug.Log($"[CharacterSkill] Floor object entered: {other.name}");
        }
        else
        {
            // 一般目標碰撞處理（StandStill 模式）
            TargetingCollidingObjects.Add(other);
            
            // 尋找 TargetedIndicator 組件並檢查陣營
            TargetedIndicator targetIndicator = other.GetComponentInParent<TargetedIndicator>();
            if (targetIndicator != null)
            {
                // 檢查技能是否可以瞄準此陣營
                if (CanTargetEntity(targetIndicator.GetCombatEntity()))
                {
                    targetIndicator.OnTargeted();
                    currentTargets.Add(targetIndicator);
                    Debug.Log($"[CharacterSkill] TargetedIndicator marked on: {targetIndicator.gameObject.name}");
                }
                else
                {
                    Debug.Log($"[CharacterSkill] TargetedIndicator skipped (faction not targetable): {targetIndicator.gameObject.name}");
                }
            }
            
            // 尋找有 CombatEntity 組件的父物件
            CombatEntity combatEntity = other.GetComponentInParent<CombatEntity>();
            if (combatEntity != null)
            {
                if (CanTargetEntity(combatEntity))
                {
                    Debug.Log($"[CharacterSkill] CombatEntity detected: {combatEntity.gameObject.name} (Faction: {combatEntity.Faction})");
                }
                else
                {
                    Debug.Log($"[CharacterSkill] CombatEntity ignored (faction not targetable): {combatEntity.gameObject.name} (Faction: {combatEntity.Faction})");
                }
            }
            else
            {
                Debug.Log($"[CharacterSkill] Object entered (no CombatEntity): {other.name}");
            }
        }
    }
    
    /// <summary>
    /// 技能碰撞檢測：當有物件停留在 Trigger 時
    /// </summary>
    /// <param name="other">停留的物件</param>
    public void OnTargetingTriggerStay(Collider other)
    {
        // 檢查是否碰撞到自己（通過 CombatEntity 判斷）
        if (IsSelfEntity(other))
        {
            return;
        }
        
        // 檢查是否為 Floor 層物件
        bool isFloorObject = IsInLayerMask(other.gameObject, floorLayerMask);
        
        if (isFloorObject)
        {
            // Floor 層碰撞處理
            if (!FloorCollidingObjects.Contains(other))
            {
                FloorCollidingObjects.Add(other);
                UpdateSkillTargetValidity();
            }
        }
        else
        {
            // 一般目標碰撞處理（StandStill 模式）
            if (!TargetingCollidingObjects.Contains(other))
            {
                TargetingCollidingObjects.Add(other);
                
                // 尋找 TargetedIndicator 組件並檢查陣營（如果還沒被標記）
                TargetedIndicator targetIndicator = other.GetComponentInParent<TargetedIndicator>();
                if (targetIndicator != null && !currentTargets.Contains(targetIndicator))
                {
                    // 檢查技能是否可以瞄準此陣營
                    if (CanTargetEntity(targetIndicator.GetCombatEntity()))
                    {
                        targetIndicator.OnTargeted();
                        currentTargets.Add(targetIndicator);
                        Debug.Log($"[CharacterSkill] TargetedIndicator marked on (stay): {targetIndicator.gameObject.name}");
                    }
                    else
                    {
                        Debug.Log($"[CharacterSkill] TargetedIndicator skipped (stay, faction not targetable): {targetIndicator.gameObject.name}");
                    }
                }
                
                // 尋找有 CombatEntity 組件的父物件
                CombatEntity combatEntity = other.GetComponentInParent<CombatEntity>();
                if (combatEntity != null)
                {
                    if (CanTargetEntity(combatEntity))
                    {
                        Debug.Log($"[CharacterSkill] CombatEntity staying: {combatEntity.gameObject.name} (Faction: {combatEntity.Faction})");
                    }
                    else
                    {
                        Debug.Log($"[CharacterSkill] CombatEntity staying but ignored (faction not targetable): {combatEntity.gameObject.name} (Faction: {combatEntity.Faction})");
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// 技能碰撞檢測：當有物件離開 Trigger 時
    /// </summary>
    /// <param name="other">離開的物件</param>
    public void OnTargetingTriggerExit(Collider other)
    {
        // 檢查是否碰撞到自己（通過 CombatEntity 判斷）
        if (IsSelfEntity(other))
        {
            return;
        }
        
        // 檢查是否為 Floor 層物件
        bool isFloorObject = IsInLayerMask(other.gameObject, floorLayerMask);
        
        if (isFloorObject)
        {
            // Floor 層碰撞處理
            FloorCollidingObjects.Remove(other);
            UpdateSkillTargetValidity();
            Debug.Log($"[CharacterSkill] Floor object exited: {other.name}");
        }
        else
        {
            // 一般目標碰撞處理（StandStill 模式）
            TargetingCollidingObjects.Remove(other);
            
            // 尋找 TargetedIndicator 組件並取消瞄準標記
            TargetedIndicator targetIndicator = other.GetComponentInParent<TargetedIndicator>();
            if (targetIndicator != null && currentTargets.Contains(targetIndicator))
            {
                targetIndicator.OnUntargeted();
                currentTargets.Remove(targetIndicator);
                Debug.Log($"[CharacterSkill] TargetedIndicator unmarked on: {targetIndicator.gameObject.name}");
            }
            
            // 尋找有 CombatEntity 組件的父物件
            CombatEntity combatEntity = other.GetComponentInParent<CombatEntity>();
            if (combatEntity != null)
            {
                Debug.Log($"[CharacterSkill] CombatEntity exited: {combatEntity.gameObject.name}");
            }
            else
            {
                Debug.Log($"[CharacterSkill] Object exited (no CombatEntity): {other.name}");
            }
        }
    }
    
    /// <summary>
    /// 獲取當前在 StandStill 技能扇形範圍內的所有碰撞物件
    /// </summary>
    /// <returns>碰撞物件集合</returns>
    public System.Collections.Generic.HashSet<Collider> GetStandStillCollidingObjects()
    {
        // 清理已被銷毀的物件
        TargetingCollidingObjects.RemoveWhere(c => c == null);
        return new System.Collections.Generic.HashSet<Collider>(TargetingCollidingObjects);
    }
    
    /// <summary>
    /// 清空所有碰撞物件列表
    /// </summary>
    public void ClearAllCollidingObjects()
    {
        TargetingCollidingObjects.Clear();
        FloorCollidingObjects.Clear();
        UpdateSkillTargetValidity();
        ClearAllTargetIndicators();
    }
    
    /// <summary>
    /// 清空所有目標指示器
    /// </summary>
    private void ClearAllTargetIndicators()
    {
        int clearedCount = currentTargets.Count;
        foreach (TargetedIndicator target in currentTargets)
        {
            if (target != null)
            {
                target.OnUntargeted();
            }
        }
        currentTargets.Clear();
        Debug.Log($"[CharacterSkill] Cleared {clearedCount} target indicators");
    }
    
    /// <summary>
    /// 檢查是否可以瞄準指定的 CombatEntity
    /// </summary>
    /// <param name="entity">要檢查的 CombatEntity</param>
    /// <returns>是否可以瞄準</returns>
    private bool CanTargetEntity(CombatEntity entity)
    {
        if (entity == null || currentSelectedSkill == null)
            return false;
            
        return currentSelectedSkill.CanTargetFaction(entity.Faction);
    }
    
    /// <summary>
    /// 檢查碰撞物件是否為自己（通過比較 CombatEntity）
    /// </summary>
    /// <param name="other">碰撞的 Collider</param>
    /// <returns>是否為自己</returns>
    private bool IsSelfEntity(Collider other)
    {
        if (other == null) return false;
        
        // 取得自己的 CombatEntity
        CombatEntity selfEntity = null;
        if (characterCore != null)
        {
            selfEntity = characterCore.GetComponent<CombatEntity>();
        }
        
        // 如果自己沒有 CombatEntity，則回退到檢查 Transform 層級
        if (selfEntity == null)
        {
            return IsChildOfSelf(other.transform);
        }
        
        // 取得碰撞物件的 CombatEntity
        CombatEntity otherEntity = other.GetComponentInParent<CombatEntity>();
        
        // 比較是否為同一個 CombatEntity
        return selfEntity == otherEntity;
    }
    
    /// <summary>
    /// 檢查指定的 Transform 是否為自己或自己的子物件（備用方法）
    /// </summary>
    /// <param name="target">要檢查的 Transform</param>
    /// <returns>是否為自己的子物件</returns>
    private bool IsChildOfSelf(Transform target)
    {
        if (target == null) return false;
        
        // 檢查是否為 CharacterCore 的子物件
        if (characterCore != null)
        {
            Transform current = target;
            while (current != null)
            {
                if (current == characterCore.transform)
                {
                    return true;
                }
                current = current.parent;
            }
        }
        
        // 也檢查是否為 CharacterSkills 本身的子物件
        Transform currentCheck = target;
        while (currentCheck != null)
        {
            if (currentCheck == transform)
            {
                return true;
            }
            currentCheck = currentCheck.parent;
        }
        
        return false;
    }
    
    /// <summary>
    /// 更新技能目標有效性（基於 Floor 層碰撞）
    /// </summary>
    private void UpdateSkillTargetValidity()
    {
        bool hasFloorCollision = FloorCollidingObjects.Count > 0;
        bool newTargetValid = !hasFloorCollision;
        
        // 只在狀態改變時更新顏色和輸出 Debug
        if (newTargetValid != isSkillTargetValid)
        {
            isSkillTargetValid = newTargetValid;
            
            if (hasFloorCollision)
            {
                // 變更Cube顏色為酒紅色
                if (cubeRenderer != null && cubeRenderer.material != null)
                {
                    cubeRenderer.material.color = blockedColor;
                }
                Debug.Log($"[CharacterSkill] Skill target blocked by Floor collision");
            }
            else
            {
                // 恢復Cube的原始顏色
                if (cubeRenderer != null && cubeRenderer.material != null)
                {
                    cubeRenderer.material.color = originalCubeColor;
                }
                Debug.Log($"[CharacterSkill] Skill target is now valid");
            }
        }
    }
    
    /// <summary>
    /// 檢查物件是否在指定的Layer Mask中
    /// </summary>
    /// <param name="obj">要檢查的物件</param>
    /// <param name="layerMask">Layer Mask</param>
    /// <returns>是否在Layer Mask中</returns>
    private bool IsInLayerMask(GameObject obj, LayerMask layerMask)
    {
        return ((layerMask.value & (1 << obj.layer)) > 0);
    }
    
    /// <summary>
    /// 計算考慮技能距離限制的調整後目標位置
    /// </summary>
    /// <param name="originalTarget">原始目標位置</param>
    /// <param name="skill">技能資料</param>
    /// <returns>調整後的目標位置</returns>
    private Vector3 CalculateAdjustedTargetLocation(Vector3 originalTarget, CombatSkill skill)
    {
        if (skill == null)
        {
            return originalTarget;
        }
        
        Vector3 characterPos = transform.position;
        Vector3 direction = originalTarget - characterPos;
        direction.y = 0; // 保持水平
        
        float originalDistance = direction.magnitude;
        float maxRange = skill.SkillRange;
        
        // 如果是 FrontDash 模式且需要限制距離
        if (skill.TargetingMode == SkillTargetingMode.FrontDash)
        {
            if (skill.IsFixedRange)
            {
                // 固定距離模式：始終使用技能設定的距離
                Vector3 adjustedTarget = characterPos + direction.normalized * maxRange;
                Debug.Log($"[CharacterSkill] Fixed range adjustment - Original: {originalDistance:F2}, Fixed: {maxRange:F2}");
                return adjustedTarget;
            }
            else if (originalDistance > maxRange)
            {
                // 跟隨滑鼠模式但超過最大距離：限制到最大距離
                Vector3 adjustedTarget = characterPos + direction.normalized * maxRange;
                Debug.Log($"[CharacterSkill] Range limited adjustment - Original: {originalDistance:F2}, Limited: {maxRange:F2}");
                return adjustedTarget;
            }
        }
        
        // 其他情況使用原始目標位置
        Debug.Log($"[CharacterSkill] No range adjustment needed - Distance: {originalDistance:F2}, Max: {maxRange:F2}");
        return originalTarget;
    }
    
#if UNITY_EDITOR
    /// <summary>
    /// 自動設定所有子物件的 ColliderEventReceiver - 在 Unity Editor 中自動調用
    /// </summary>
    [ContextMenu("Auto Setup ColliderEventReceivers")]
    public void AutoSetupColliderEventReceivers()
    {
        // 搜尋所有子物件中的 ColliderEventReceiver
        ColliderEventReceiver[] receivers = GetComponentsInChildren<ColliderEventReceiver>(true);
        
        int connectedCount = 0;
        
        foreach (ColliderEventReceiver receiver in receivers)
        {
            
            // 清除舊的 UnityEvent 連接（包括持久和運行時監聽者）
            receiver.OnTriggerEnterEvent.RemoveAllListeners();
            receiver.OnTriggerStayEvent.RemoveAllListeners();
            receiver.OnTriggerExitEvent.RemoveAllListeners();
            
            // 清除舊的持久監聽者
            for (int i = receiver.OnTriggerEnterEvent.GetPersistentEventCount() - 1; i >= 0; i--)
                UnityEventTools.RemovePersistentListener(receiver.OnTriggerEnterEvent, i);
            for (int i = receiver.OnTriggerStayEvent.GetPersistentEventCount() - 1; i >= 0; i--)
                UnityEventTools.RemovePersistentListener(receiver.OnTriggerStayEvent, i);
            for (int i = receiver.OnTriggerExitEvent.GetPersistentEventCount() - 1; i >= 0; i--)
                UnityEventTools.RemovePersistentListener(receiver.OnTriggerExitEvent, i);
            
            // 添加新的持久監聽者（會被保存到場景文件中）
            UnityEventTools.AddPersistentListener(receiver.OnTriggerEnterEvent, OnTargetingTriggerEnter);
            UnityEventTools.AddPersistentListener(receiver.OnTriggerStayEvent, OnTargetingTriggerStay);
            UnityEventTools.AddPersistentListener(receiver.OnTriggerExitEvent, OnTargetingTriggerExit);
            
            // 啟用 Trigger 事件，關閉 Collision 事件（通常技能系統只需要 Trigger）
            receiver.SetTriggerEventsEnabled(true);
            receiver.SetCollisionEventsEnabled(false);
            
            connectedCount++;
            
            Debug.Log($"[CharacterSkills] Connected ColliderEventReceiver on {receiver.gameObject.name}");
        }
        
        Debug.Log($"[CharacterSkills] Auto setup completed. Connected {connectedCount} ColliderEventReceiver(s).");
        
        // 標記場景為已修改
        if (!Application.isPlaying)
        {
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        }
    }
    
    /// <summary>
    /// Unity Editor 中當物件被修改時自動調用
    /// </summary>
    void OnValidate()
    {
        // 只在非播放模式下執行，避免運行時干擾
        if (!Application.isPlaying && this != null && gameObject != null)
        {
            // 延遲執行以避免在 OnValidate 中直接修改物件
            UnityEditor.EditorApplication.delayCall += AutoSetupColliderEventReceivers;
        }
    }
#endif
}