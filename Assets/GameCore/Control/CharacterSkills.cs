using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Events;
#endif

/// <summary>
/// 角色技能系統 - 負責技能管理、瞄準系統、技能執行
/// </summary>
namespace Wuxia.GameCore
{
    public class CharacterSkills : MonoBehaviour
    {
        [Header("技能配置")] [SerializeField] private List<CombatSkill> skills = new List<CombatSkill>(4);

        /// <summary>
        /// 獲取指定索引的技能
        /// </summary>
        /// <param name="index">技能索引 (0-3)</param>
        /// <returns>技能，如果索引無效則返回 null</returns>
        public CombatSkill GetSkill(int index)
        {
            return (index >= 0 && index < skills.Count) ? skills[index] : null;
        }

        /// <summary>
        /// 設定指定索引的技能
        /// </summary>
        /// <param name="index">技能索引</param>
        /// <param name="skill">要設定的技能</param>
        public void SetSkill(int index, CombatSkill skill)
        {
            // 確保 List 有足夠的空間
            while (skills.Count <= index)
            {
                skills.Add(null);
            }

            if (index >= 0 && index < skills.Count)
            {
                skills[index] = skill;
            }
        }

        /// <summary>
        /// 獲取所有技能
        /// </summary>
        public List<CombatSkill> Skills => skills;

        [Header("當前選擇的技能")] public CombatSkill currentSelectedSkill; // 當前選擇要執行的技能

        [Header("技能瞄準系統")] public Transform straightFrontTargetingAnchor; // 技能瞄準錨點
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

        private System.Collections.Generic.HashSet<Collider> TargetingCollidingObjects =
            new System.Collections.Generic.HashSet<Collider>();

        // Floor 層碰撞檢測（用於 FrontDash 模式）
        private System.Collections.Generic.HashSet<Collider> FloorCollidingObjects =
            new System.Collections.Generic.HashSet<Collider>();

        // 追蹤已被標記為瞄準目標的 TargetedIndicator
        private System.Collections.Generic.HashSet<TargetedIndicator> currentTargets =
            new System.Collections.Generic.HashSet<TargetedIndicator>();

        // SingleTarget 模式相關變數
        private CombatEntity currentHoveredTarget = null; // 當前滑鼠指向的目標
        private TargetedIndicator currentHoveredIndicator = null; // 當前目標的指示器
        public LayerMask selectableLayerMask = (1 << 12); // Selectable 圖層遮罩 (Layer 12)
        
        [Header("瞄準線設定")]
        private LineRenderer sightLineRenderer; // 瞄準線 LineRenderer
        private LayerMask lineOfSightBlockingLayers; // 阻擋視線的圖層遮罩
        public Color clearSightLineColor = Color.green; // 視線暢通時的線條顏色
        public Color blockedSightLineColor = Color.red; // 視線被阻擋時的線條顏色
        public float sightLineWidth = 0.05f; // 瞄準線寬度
        private Material clearSightMaterial; // 視線暢通時的材質
        private Material blockedSightMaterial; // 視線被阻擋時的材質

        // 對其他組件的引用
        public CharacterCore characterCore;
        public CharacterResources characterResources;

        // 碰撞類型枚舉
        private enum CollisionType
        {
            Enter,
            Exit
        }

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
            
            // 初始化瞄準線 LineRenderer
            InitializeSightLineRenderer();
            
            // 設定視線阻擋圖層（包含 Floor 和 DamageReceiver 相關圖層）
            lineOfSightBlockingLayers = floorLayerMask | LayerMask.GetMask("Default");
        }

        /// <summary>
        /// 初始化瞄準線 LineRenderer
        /// </summary>
        private void InitializeSightLineRenderer()
        {
            // 創建 LineRenderer 子物件
            GameObject sightLineObject = new GameObject("SightLine");
            sightLineObject.transform.SetParent(transform);
            sightLineObject.transform.localPosition = Vector3.zero;
            
            sightLineRenderer = sightLineObject.AddComponent<LineRenderer>();
            
            // 創建兩種不同顏色的材質
            clearSightMaterial = new Material(Shader.Find("Sprites/Default"));
            clearSightMaterial.color = clearSightLineColor;
            
            blockedSightMaterial = new Material(Shader.Find("Sprites/Default"));
            blockedSightMaterial.color = blockedSightLineColor;
            
            // 設定 LineRenderer 屬性
            sightLineRenderer.material = clearSightMaterial; // 預設使用綠色材質
            sightLineRenderer.startWidth = sightLineWidth;
            sightLineRenderer.endWidth = sightLineWidth;
            sightLineRenderer.positionCount = 2;
            sightLineRenderer.useWorldSpace = true;
            sightLineRenderer.enabled = false; // 初始時隱藏
        }

        /// <summary>
        /// 檢查從角色到目標的視線是否被阻擋
        /// </summary>
        /// <param name="targetPosition">目標位置</param>
        /// <returns>視線檢查結果：(isBlocked, hitPoint)</returns>
        private (bool isBlocked, Vector3 hitPoint) CheckLineOfSight(Vector3 targetPosition)
        {
            if (characterCore == null) return (false, targetPosition);
            
            Vector3 startPosition = characterCore.transform.position + Vector3.up * 0.5f; // 稍微抬高起點
            Vector3 endPosition = targetPosition + Vector3.up * 0.5f; // 稍微抬高終點
            Vector3 direction = (endPosition - startPosition).normalized;
            float distance = Vector3.Distance(startPosition, endPosition);
            
            // 發射射線檢查障礙物
            RaycastHit hit;
            if (Physics.Raycast(startPosition, direction, out hit, distance, lineOfSightBlockingLayers))
            {
                // 檢查擊中的是否為目標本身
                CombatEntity hitEntity = hit.collider.GetComponentInParent<CombatEntity>();
                if (hitEntity != null && hitEntity == currentHoveredTarget)
                {
                    // 擊中的是目標本身，視線暢通
                    return (false, targetPosition);
                }
                
                // 視線被其他物體阻擋
                return (true, hit.point);
            }
            
            // 視線暢通
            return (false, targetPosition);
        }

        /// <summary>
        /// 更新瞄準線顯示
        /// </summary>
        /// <param name="targetPosition">目標位置</param>
        /// <param name="isBlocked">視線是否被阻擋</param>
        /// <param name="hitPoint">射線擊中點</param>
        private void UpdateSightLine(Vector3 targetPosition, bool isBlocked, Vector3 hitPoint)
        {
            if (sightLineRenderer == null || characterCore == null) return;
            
            Vector3 startPosition = characterCore.transform.position + Vector3.up * 0.5f;
            
            if (isBlocked)
            {
                // 視線被阻擋：使用紅色材質，顯示線條到阻擋點
                if (blockedSightMaterial != null)
                    sightLineRenderer.material = blockedSightMaterial;
                sightLineRenderer.SetPosition(0, startPosition);
                sightLineRenderer.SetPosition(1, hitPoint);
            }
            else
            {
                // 視線暢通：使用綠色材質，顯示線條到目標
                if (clearSightMaterial != null)
                    sightLineRenderer.material = clearSightMaterial;
                sightLineRenderer.SetPosition(0, startPosition);
                sightLineRenderer.SetPosition(1, targetPosition + Vector3.up * 0.5f);
            }
            
            sightLineRenderer.enabled = true;
        }

        /// <summary>
        /// 隱藏瞄準線
        /// </summary>
        private void HideSightLine()
        {
            if (sightLineRenderer != null)
            {
                sightLineRenderer.enabled = false;
            }
        }

        /// <summary>
        /// 檢查是否可以使用指定技能
        /// </summary>
        /// <param name="skill">要檢查的技能</param>
        /// <returns>是否可以使用</returns>
        public bool CanUseSkill(CombatSkill skill)
        {
            return skill != null && characterResources != null && characterResources.HasEnoughAP(skill.SPCost);
        }

        /// <summary>
        /// 檢查指定索引的技能是否可以使用
        /// </summary>
        /// <param name="index">技能索引 (0-3)</param>
        /// <returns>是否可以使用</returns>
        public bool CanUseSkillByIndex(int index) => CanUseSkill(GetSkill(index));


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
                    
                    // 清除 SingleTarget 狀態
                    ClearSingleTargetState();
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
                    case SkillTargetingMode.SingleTarget:
                        UpdateSingleTargetTargeting();
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
        /// 更新 SingleTarget 瞄準模式（滑鼠直接點擊目標）
        /// </summary>
        private void UpdateSingleTargetTargeting()
        {
            // 隱藏其他瞄準器
            if (straightFrontTargetingAnchor != null && straightFrontTargetingAnchor.gameObject.activeSelf)
            {
                straightFrontTargetingAnchor.gameObject.SetActive(false);
            }

            if (standStillTargetingAnchor != null)
            {
                standStillTargetingAnchor.SetVisible(false);
            }

            // 檢查是否需要清除狀態（模式或技能改變時）
            bool targetingModeChanged = lastTargetingMode != SkillTargetingMode.SingleTarget;
            bool skillChanged = lastSelectedSkill != currentSelectedSkill;
            bool needsClear = targetingModeChanged || skillChanged;

            // 更新狀態追蹤
            lastTargetingMode = SkillTargetingMode.SingleTarget;
            lastSelectedSkill = currentSelectedSkill;

            // 如果模式或技能改變，清除之前的狀態
            if (needsClear)
            {
                ClearSingleTargetState();
            }

            // 讓角色面向滑鼠方向（即使沒有有效目標）
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
                }
            }

            // 使用 Raycast 檢測滑鼠指向的 CombatEntity
            CombatEntity hoveredEntity = GetCombatEntityUnderMouse();

            // 檢查目標是否改變
            if (hoveredEntity != currentHoveredTarget)
            {
                // 清除舊目標的指示器（包括正常狀態和無效選擇狀態）
                if (currentHoveredIndicator != null)
                {
                    currentHoveredIndicator.OnUntargeted();
                    currentHoveredIndicator.ClearInvalidSelection();
                    currentHoveredIndicator = null;
                }

                currentHoveredTarget = hoveredEntity;

                // 設定新目標的指示器
                if (currentHoveredTarget != null)
                {
                    TargetedIndicator indicator = currentHoveredTarget.GetComponent<TargetedIndicator>();
                    if (indicator != null)
                    {
                        // 檢查視線並獲取結果用於顯示瞄準線
                        var (isBlocked, hitPoint) = CheckLineOfSight(currentHoveredTarget.transform.position);
                        
                        if (IsValidSingleTarget(currentHoveredTarget))
                        {
                            // 有效目標：顯示正常的瞄準效果和綠色瞄準線
                            indicator.OnTargeted();
                            currentHoveredIndicator = indicator;
                            isSkillTargetValid = true;
                            UpdateSightLine(currentHoveredTarget.transform.position, false, Vector3.zero);
                        }
                        else
                        {
                            // 無效目標：顯示紅色的無效選擇效果和紅色瞄準線（如果被阻擋）
                            indicator.OnInvalidSelection();
                            currentHoveredIndicator = indicator;
                            isSkillTargetValid = false;
                            UpdateSightLine(currentHoveredTarget.transform.position, isBlocked, hitPoint);
                        }
                    }
                }
                else
                {
                    // 沒有目標，隱藏瞄準線
                    isSkillTargetValid = false;
                    HideSightLine();
                }
            }
            else if (currentHoveredTarget != null)
            {
                // 目標沒有改變，但每幀更新瞄準線（為了動態顯示視線變化）
                var (isBlocked, hitPoint) = CheckLineOfSight(currentHoveredTarget.transform.position);
                bool isValid = IsValidSingleTarget(currentHoveredTarget);
                
                if (isValid)
                {
                    UpdateSightLine(currentHoveredTarget.transform.position, false, Vector3.zero);
                }
                else
                {
                    UpdateSightLine(currentHoveredTarget.transform.position, isBlocked, hitPoint);
                }
            }
        }

        /// <summary>
        /// 清除 SingleTarget 狀態
        /// </summary>
        private void ClearSingleTargetState()
        {
            if (currentHoveredIndicator != null)
            {
                // 清除所有狀態（包括正常瞄準和無效選擇狀態）
                currentHoveredIndicator.ClearAllTargeting();
                currentHoveredIndicator = null;
            }
            currentHoveredTarget = null;
            isSkillTargetValid = false;
            
            // 隱藏瞄準線
            HideSightLine();
        }

        /// <summary>
        /// 使用 Raycast 檢測滑鼠指向的 CombatEntity（優先檢測 Selectable Layer）
        /// </summary>
        /// <returns>滑鼠指向的 CombatEntity，如果沒有則返回 null</returns>
        private CombatEntity GetCombatEntityUnderMouse()
        {
            if (SLGCoreUI.Instance == null) return null;

            // 從滑鼠位置發射射線
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit[] hits = Physics.RaycastAll(ray, Mathf.Infinity);

            // 首先查找 Selectable Layer 中最近的 CombatEntity
            CombatEntity selectableEntity = null;
            float selectableDistance = Mathf.Infinity;

            // 然後查找其他層中最近的 CombatEntity (備用)
            CombatEntity fallbackEntity = null;
            float fallbackDistance = Mathf.Infinity;

            foreach (RaycastHit hit in hits)
            {
                // 跳過 Floor 層
                if (IsInLayerMask(hit.collider.gameObject, floorLayerMask))
                    continue;

                // 檢查是否為自己
                if (IsSelfEntity(hit.collider))
                    continue;

                // 檢查是否為 Selectable Layer
                bool isSelectableLayer = IsInLayerMask(hit.collider.gameObject, selectableLayerMask);

                if (isSelectableLayer)
                {
                    // 在 Selectable Layer 中尋找 CombatEntity
                    CombatEntity entity = hit.collider.GetComponentInParent<CombatEntity>();
                    if (entity != null && hit.distance < selectableDistance)
                    {
                        selectableEntity = entity;
                        selectableDistance = hit.distance;
                    }
                }
                else
                {
                    // 在其他層中尋找 CombatEntity (備用)
                    CombatEntity entity = hit.collider.GetComponentInParent<CombatEntity>();
                    if (entity != null && hit.distance < fallbackDistance)
                    {
                        fallbackEntity = entity;
                        fallbackDistance = hit.distance;
                    }
                }
            }

            // 優先返回 Selectable Layer 中的實體，如果沒有則返回備用實體
            return selectableEntity != null ? selectableEntity : fallbackEntity;
        }

        /// <summary>
        /// 檢查指定的 CombatEntity 是否為有效的單一目標
        /// </summary>
        /// <param name="entity">要檢查的 CombatEntity</param>
        /// <returns>是否為有效目標</returns>
        private bool IsValidSingleTarget(CombatEntity entity)
        {
            if (entity == null || currentSelectedSkill == null || characterCore == null)
                return false;

            // 檢查陣營是否可瞄準
            if (!currentSelectedSkill.CanTargetFaction(entity.Faction))
                return false;

            // 檢查距離是否在技能範圍內
            float distance = Vector3.Distance(characterCore.transform.position, entity.transform.position);
            if (distance > currentSelectedSkill.SkillRange)
                return false;

            // 檢查視線是否被阻擋
            var (isBlocked, hitPoint) = CheckLineOfSight(entity.transform.position);
            if (isBlocked)
                return false;

            return true;
        }

        /// <summary>
        /// 獲取當前滑鼠指向的有效目標
        /// </summary>
        /// <returns>當前有效目標，如果沒有則返回 null</returns>
        public CombatEntity GetCurrentSingleTarget()
        {
            if (currentSelectedSkill != null && 
                currentSelectedSkill.TargetingMode == SkillTargetingMode.SingleTarget &&
                isSkillTargetValid)
            {
                return currentHoveredTarget;
            }
            return null;
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
        /// 統一的碰撞處理入口方法
        /// </summary>
        /// <param name="other">碰撞的 Collider</param>
        /// <param name="type">碰撞類型</param>
        private void HandleTargetCollision(Collider other, CollisionType type)
        {
            // 檢查是否為自己的碰撞器，如果是則忽略
            if (IsSelfEntity(other))
            {
                return;
            }

            if (IsInLayerMask(other.gameObject, floorLayerMask))
            {
                HandleFloorCollision(other, type);
            }
            else
            {
                HandleEntityCollision(other, type);
            }
        }

        /// <summary>
        /// 處理 Floor 層的碰撞
        /// </summary>
        /// <param name="other">碰撞的 Collider</param>
        /// <param name="type">碰撞類型</param>
        private void HandleFloorCollision(Collider other, CollisionType type)
        {
            switch (type)
            {
                case CollisionType.Enter:
                    FloorCollidingObjects.Add(other);
                    UpdateSkillTargetValidity();
                    break;
                case CollisionType.Exit:
                    FloorCollidingObjects.Remove(other);
                    UpdateSkillTargetValidity();
                    break;
            }
        }

        /// <summary>
        /// 處理一般實體的碰撞
        /// </summary>
        /// <param name="other">碰撞的 Collider</param>
        /// <param name="type">碰撞類型</param>
        private void HandleEntityCollision(Collider other, CollisionType type)
        {
            switch (type)
            {
                case CollisionType.Enter:
                    TargetingCollidingObjects.Add(other);
                    ProcessTargetIndicator(other, true);
                    break;
                case CollisionType.Exit:
                    TargetingCollidingObjects.Remove(other);
                    ProcessTargetIndicator(other, false);
                    break;
            }
        }

        /// <summary>
        /// 處理 TargetedIndicator 的標記和取消標記
        /// </summary>
        /// <param name="other">碰撞的 Collider</param>
        /// <param name="isEntering">是否為進入碰撞</param>
        private void ProcessTargetIndicator(Collider other, bool isEntering)
        {
            TargetedIndicator targetIndicator = other.GetComponentInParent<TargetedIndicator>();
            if (targetIndicator != null)
            {
                if (isEntering)
                {
                    // 檢查技能是否可以瞄準此陣營
                    if (CanTargetEntity(targetIndicator.GetCombatEntity()))
                    {
                        targetIndicator.OnTargeted();
                        currentTargets.Add(targetIndicator);
                    }
                }
                else if (currentTargets.Contains(targetIndicator))
                {
                    targetIndicator.OnUntargeted();
                    currentTargets.Remove(targetIndicator);
                }
            }
        }


        /// <summary>
        /// 技能碰撞檢測：當有物件進入 Trigger 時
        /// </summary>
        /// <param name="other">進入的物件</param>
        public void OnTargetingTriggerEnter(Collider other)
        {
            HandleTargetCollision(other, CollisionType.Enter);
        }

        /// <summary>
        /// 技能碰撞檢測：當有物件離開 Trigger 時
        /// </summary>
        /// <param name="other">離開的物件</param>
        public void OnTargetingTriggerExit(Collider other)
        {
            HandleTargetCollision(other, CollisionType.Exit);
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

            // 扇型瞄準模式（StandStill）不受 Floor 碰撞影響
            bool newTargetValid;
            if (currentSelectedSkill != null && currentSelectedSkill.TargetingMode == SkillTargetingMode.StandStill)
            {
                newTargetValid = true; // 扇型模式始終有效
            }
            else
            {
                newTargetValid = !hasFloorCollision; // 其他模式需要檢查 Floor 碰撞
            }

            // 只在狀態改變時更新顏色和輸出 Debug
            if (newTargetValid != isSkillTargetValid)
            {
                isSkillTargetValid = newTargetValid;

                if (hasFloorCollision && currentSelectedSkill != null &&
                    currentSelectedSkill.TargetingMode != SkillTargetingMode.StandStill)
                {
                    // 變更Cube顏色為酒紅色（僅對非扇型模式）
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
                    return adjustedTarget;
                }
                else if (originalDistance > maxRange)
                {
                    // 跟隨滑鼠模式但超過最大距離：限制到最大距離
                    Vector3 adjustedTarget = characterPos + direction.normalized * maxRange;
                    return adjustedTarget;
                }
            }

            // 其他情況使用原始目標位置
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
                receiver.OnTriggerExitEvent.RemoveAllListeners();

                // 清除舊的持久監聽者
                for (int i = receiver.OnTriggerEnterEvent.GetPersistentEventCount() - 1; i >= 0; i--)
                    UnityEventTools.RemovePersistentListener(receiver.OnTriggerEnterEvent, i);
                for (int i = receiver.OnTriggerExitEvent.GetPersistentEventCount() - 1; i >= 0; i--)
                    UnityEventTools.RemovePersistentListener(receiver.OnTriggerExitEvent, i);

                // 添加新的持久監聽者（會被保存到場景文件中）
                UnityEventTools.AddPersistentListener(receiver.OnTriggerEnterEvent, OnTargetingTriggerEnter);
                UnityEventTools.AddPersistentListener(receiver.OnTriggerExitEvent, OnTargetingTriggerExit);

                // 啟用 Trigger 事件，關閉 Collision 事件（通常技能系統只需要 Trigger）
                receiver.SetTriggerEventsEnabled(true);
                receiver.SetCollisionEventsEnabled(false);

                connectedCount++;

            }
        }

#endif
    }
}