using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;


public class CharacterCore : MonoBehaviour
{
    // For Control //
    public float moveSpeed = 20f;
    public Transform cameraTransform;
    public float turnRate = 720;

    [Header("資源系統")]
    public float AP = 100;  // Action Points - 統一的資源系統
    public float MaxAP = 100;
    public Vector2 lastPosition;
    
    [Header("預覽系統")]
    private bool isPreviewingPath = false;     // 是否正在預覽路徑
    private float previewAPCost = 0f;     // 預覽的AP消耗量
    
    [Header("回合控制")]
    public float holdTimeToEndTurn = 1.0f;     // 長壓多少秒結束回合
    private float spaceKeyHoldTime = 0f;       // 空白鍵持續按住時間
    private bool isHoldingSpace = false;       // 是否正在按住空白鍵
    
    public float pointSpacing = 0.2f; // 每隔幾公尺新增一點
    public List<Vector3> points = new List<Vector3>();
    private Vector3 lastDrawPoint;
    
    public Queue<CombatAction> RecordedActions = new Queue<CombatAction>();
    
    [Header("NavMesh移動系統")]
    public NavMeshAgent navMeshAgent;
    public bool isMoving = false;
    private Vector3 targetPosition;
    private bool hasValidTarget = false;
    
    [Header("路徑顯示")]
    public LineRenderer pathLineRenderer;          // 主要路徑LineRenderer（綠色部分）
    public LineRenderer invalidPathLineRenderer;   // 無效路徑LineRenderer（紅色部分）
    public Color validPathColor = Color.green;     // 體力足夠時的路徑顏色
    public Color invalidPathColor = Color.red;     // 體力不足時的路徑顏色
    
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
        AddPoint(new Vector3(transform.position.x, 0.1f, transform.position.z));
        
        // 初始化NavMeshAgent
        InitializeNavMeshAgent();
        
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
                        Debug.Log($"[CharacterCore] 已儲存Cube原始顏色: {originalCubeColor}");
                    }
                    
                    // 初始化碰撞檢測器（已在 prefab 中設定引用）
                    if (collisionDetector != null)
                    {
                        collisionDetector.Initialize(this, floorLayerMask);
                    }
                    else
                    {
                        Debug.LogWarning("[CharacterCore] 碰撞檢測器引用未在 prefab 中設定！");
                    }
                    
                    // 確保初始時是隱藏的
                    straightFrontTargetingAnchor.gameObject.SetActive(false);
                    Debug.Log("[CharacterCore] 找到技能瞄準系統 StraightFrontTargetingAnchor 和 BoxCollider");
                }
                else
                {
                    Debug.LogWarning("[CharacterCore] 找不到 Cube 的 BoxCollider 組件");
                }
            }
            else
            {
                Debug.LogWarning("[CharacterCore] 找不到 StraightFrontTargetingAnchor 下的 Cube 物件");
            }
        }
        else
        {
            Debug.LogWarning("[CharacterCore] 找不到 StraightFrontTargetingAnchor 物件");
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
    
    private void InitializeNavMeshAgent()
    {
        // 取得NavMeshAgent元件
        if (navMeshAgent == null)
        {
            navMeshAgent = GetComponent<NavMeshAgent>();
        }
        
        if (navMeshAgent == null)
        {
            Debug.LogError("CharacterCore需要NavMeshAgent元件！");
            return;
        }
        
        // 設定NavMeshAgent參數
        navMeshAgent.speed = moveSpeed;
        navMeshAgent.angularSpeed = turnRate;
        navMeshAgent.acceleration = 50f;
        navMeshAgent.stoppingDistance = 0.1f;
        
        // 初始狀態下停止NavMeshAgent
        navMeshAgent.isStopped = true;
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
            
            
            Debug.Log("[CharacterCore] 已初始化AnimationRelativePos，使用Transform根運動模式");
        }
        else
        {
            Debug.LogWarning("[CharacterCore] 沒有找到AnimationRelativePos組件，技能root motion位移將無法正常工作");
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
            
            Debug.Log("[CharacterCore] 已初始化AnimationMoveScaler3D，技能位移縮放系統已就緒");
        }
        else
        {
            Debug.LogWarning("[CharacterCore] 沒有找到AnimationMoveScaler3D組件，技能位移縮放將無法正常工作");
        }
    }
    
    public void AddPoint(Vector3 flatPos)
    {
        points.Add(flatPos);
        lastDrawPoint = flatPos;
    }
    
    public void ControlUpdate()
    {
        // 更新移動狀態
        UpdateMovement();
        
        // 更新路徑追蹤
        UpdatePathTracking();
    }
    
    private void UpdateMovement()
    {
        if (navMeshAgent == null) return;
        
        // 檢查是否正在移動
        if (isMoving && navMeshAgent.hasPath)
        {
            // 檢查是否到達目標
            if (!navMeshAgent.pathPending && navMeshAgent.remainingDistance < 0.3f)
            {
                // 到達目標，停止移動
                StopMovement();
            }
            else
            {
                // 正在移動，消耗體力
                Vector2 nowPosition = new Vector2(transform.position.x, transform.position.z);
                float distance = Vector2.Distance(lastPosition, nowPosition);
                

                AP -= distance * 5.0f;
                AP = Mathf.Max(0, AP); // 確保AP不會變負數
                
                // 如果AP耗盡，立即停止移動
                if (AP <= 0)
                {
                    Debug.Log("AP耗盡，停止移動！");
                    StopMovement();
                    return;
                }
                
                // 更新UI（只在非預覽狀態下更新）
                if (!isPreviewingPath && SLGCoreUI.Instance != null && SLGCoreUI.Instance.apBar != null)
                {
                    SLGCoreUI.Instance.apBar.slider.maxValue = MaxAP;
                    SLGCoreUI.Instance.apBar.slider.value = AP;
                }
                
                
                // 更新動畫
                CharacterControlAnimator.SetBool("isMoving", true);
            }
        }
        else
        {
            // 不在移動，停止動畫
            CharacterControlAnimator.SetBool("isMoving", false);
        }
    }
    
    private void UpdatePathTracking()
    {
        lastPosition = new Vector2(transform.position.x, transform.position.z);
        Vector3 flatCurrentPos = new Vector3(transform.position.x, 0.1f, transform.position.z);

        if (Vector3.Distance(flatCurrentPos, lastDrawPoint) >= pointSpacing)
        {
            AddPoint(flatCurrentPos);
        }
    }
    /// <summary>
    /// 移動到指定位置
    /// </summary>
    /// <param name="destination">目標位置</param>
    public void MoveTo(Vector3 destination)
    {
        if (navMeshAgent == null) return;
        
        // 檢查是否有足夠AP
        if (AP <= 0)
        {
            Debug.Log("AP不足，無法移動！");
            return;
        }
        
        // 檢查目標位置是否在NavMesh上
        NavMeshHit hit;
        if (NavMesh.SamplePosition(destination, out hit, 2f, NavMesh.AllAreas))
        {
            // 先計算路徑來檢查體力是否足夠
            NavMeshPath path = new NavMeshPath();
            if (navMeshAgent.CalculatePath(hit.position, path))
            {
                Vector3 actualDestination;
                
                // 檢查AP是否足夠完成這段路徑
                if (!HasEnoughAPForPath(path))
                {
                    // AP不足，計算最遠能到達的位置
                    actualDestination = CalculateMaxReachablePosition(path);
                    
                    if (Vector3.Distance(transform.position, actualDestination) < 0.5f)
                    {
                        // 如果最遠距離太近，就不移動
                        Debug.Log("AP不足，無法進行有效移動！");
                        return;
                    }
                    
                    Debug.Log($"AP不足到達目標，移動到最遠可達位置");
                }
                else
                {
                    // AP足夠，移動到目標位置
                    actualDestination = hit.position;
                    Debug.Log("AP足夠，移動到目標位置");
                }
                
                targetPosition = actualDestination;
                hasValidTarget = true;
                
                // 重置預覽狀態，回到顯示實際AP值
                if (isPreviewingPath)
                {
                    isPreviewingPath = false;
                    previewAPCost = 0f;
                    
                    // 恢復顯示實際的AP值
                    if (SLGCoreUI.Instance != null && SLGCoreUI.Instance.apBar != null)
                    {
                        SLGCoreUI.Instance.apBar.slider.value = AP;
                    }
                }
                
                // 設定NavMeshAgent目標
                navMeshAgent.isStopped = false;
                navMeshAgent.SetDestination(targetPosition);
                
                // 繪製導航路徑
                StartCoroutine(DrawNavMeshPath());
                
                isMoving = true;
                
                // 記錄移動動作
                RecordMovementAction();
                
                Debug.Log($"開始移動到: {targetPosition}");
            }
            else
            {
                Debug.Log("無法計算到目標位置的路徑！");
            }
        }
        else
        {
            Debug.Log("目標位置無法到達！");
        }
    }
    
    /// <summary>
    /// 停止移動
    /// </summary>
    public void StopMovement()
    {
        if (navMeshAgent == null) return;
        
        navMeshAgent.isStopped = true;
        isMoving = false;
        hasValidTarget = false;
        
        // 停止動畫
        CharacterControlAnimator.SetBool("isMoving", false);
        
        // 清除路徑顯示（包括兩個LineRenderer）
        ClearPathDisplay();
        
        Debug.Log("停止移動");
    }
    
    /// <summary>
    /// 記錄移動動作用於重播
    /// </summary>
    private void RecordMovementAction()
    {
        CombatAction moveAction = new CombatAction();
        moveAction.Position = transform.position;
        moveAction.rotation = transform.rotation;
        moveAction.type = CombatAction.ActionType.Move;
        RecordedActions.Enqueue(moveAction);
    }
    
    /// <summary>
    /// 檢查是否可以移動到指定位置
    /// </summary>
    /// <param name="destination">目標位置</param>
    /// <returns>是否可以移動</returns>
    public bool CanMoveTo(Vector3 destination)
    {
        if (navMeshAgent == null || AP <= 0) return false;
        
        NavMeshHit hit;
        return NavMesh.SamplePosition(destination, out hit, 2f, NavMesh.AllAreas);
    }

    public bool CheckConfirm()
    {
        // 確認按鍵將改為滑鼠點擊
        return false;
    }
    
    public void ReFillAP()
    {
        AP = MaxAP;
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
        else if (nowState == CharacterCoreState.UsingSkill)
        {
            AnimatorStateInfo stateInfo = CharacterControlAnimator.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.normalizedTime >= 1.0f)
            {
                // 技能動畫完成，清除AnimationMoveScaler3D狀態
                if (animationMoveScaler3D != null)
                {
                    animationMoveScaler3D.ClearEvaluate();
                    Debug.Log("[CharacterCore] 技能動畫完成，已清除AnimationMoveScaler3D狀態");
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
        return skillA != null && AP >= skillA.SPCost;
    }
    
    public bool CanUseSkillB()
    {
        return skillB != null && AP >= skillB.SPCost;
    }
    
    public bool CanUseSkillC()
    {
        return skillC != null && AP >= skillC.SPCost;
    }
    
    public bool CanUseSkillD()
    {
        return skillD != null && AP >= skillD.SPCost;
    }
    
    public void UseSkill(CombatSkill skill, CombatAction.ActionType actionType)
    {
        if (skill != null && AP >= skill.SPCost)
        {
            CombatAction tempActionMove = new CombatAction();
            nowState = CharacterCoreState.UsingSkill;
            tempActionMove.Position = transform.position;
            tempActionMove.rotation = transform.rotation;
            tempActionMove.type = CombatAction.ActionType.Move;
            RecordedActions.Enqueue(tempActionMove);
            
            CombatAction tempActionSkill = new CombatAction();
            tempActionSkill.type = actionType;
            
            
            CharacterControlAnimator.Play(skill.AnimationName);
            AP -= skill.SPCost;
            
            RecordedActions.Enqueue(tempActionSkill);
        }
    }
    
    /// <summary>
    /// 在指定位置執行技能
    /// </summary>
    /// <param name="targetLocation">技能目標位置</param>
    /// <param name="skill">要執行的技能</param>
    /// <param name="actionType">技能動作類型</param>
    public void ExecuteSkillAtLocation(Vector3 targetLocation, CombatSkill skill, CombatAction.ActionType actionType)
    {
        if (skill != null && AP >= skill.SPCost)
        {
            // 檢查技能路徑是否有效（沒有被 Floor 層阻擋）
            if (!isSkillTargetValid)
            {
                Debug.Log($"[CharacterCore] 技能 {skill.SkillName} 無法使用：路徑被 Floor 層阻擋！");
                return;
            }
            
            Debug.Log($"執行技能 {skill.SkillName} 於位置: {targetLocation}");
            
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
                Debug.Log($"[CharacterCore] 已設定AnimationMoveScaler3D目標位置: {targetLocation}");
            }
            
            // 記錄移動到當前位置
            CombatAction tempActionMove = new CombatAction();
            nowState = CharacterCoreState.UsingSkill;
            tempActionMove.Position = transform.position;
            tempActionMove.rotation = transform.rotation;
            tempActionMove.type = CombatAction.ActionType.Move;
            RecordedActions.Enqueue(tempActionMove);
            
            // 記錄技能動作
            CombatAction tempActionSkill = new CombatAction();
            tempActionSkill.type = actionType;
            tempActionSkill.targetPosition = targetLocation; // 儲存目標位置
            
            
            // 播放技能動畫
            CharacterControlAnimator.Play(skill.AnimationName);
            AP -= skill.SPCost;
            
            RecordedActions.Enqueue(tempActionSkill);
            
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
        if (navMeshAgent == null || pathLineRenderer == null) return;
        
        // 檢查目標位置是否在NavMesh上
        NavMeshHit hit;
        if (NavMesh.SamplePosition(destination, out hit, 2f, NavMesh.AllAreas))
        {
            // 計算路徑但不移動
            NavMeshPath path = new NavMeshPath();
            if (navMeshAgent.CalculatePath(hit.position, path))
            {
                // 檢查AP是否足夠
                bool hasEnoughAP = HasEnoughAPForPath(path);
                
                // 繪製分段路徑（綠色+紅色）
                DrawPath(path, true);
                
                // 顯示路徑長度和AP消耗的Debug訊息
                float pathLength = CalculatePathLength(path);
                float apCost = CalculateAPCost(pathLength);
                Debug.Log($"路徑長度: {pathLength:F2}, AP消耗: {apCost:F2}, 當前AP: {AP:F2}, AP{(hasEnoughAP ? "足夠" : "不足")}");
                
                // 設定預覽狀態
                isPreviewingPath = true;
                previewAPCost = apCost;
                
                // 使用Proxy值更新UI（顯示預期的剩餘AP）
                if (SLGCoreUI.Instance != null && SLGCoreUI.Instance.apBar != null)
                {
                    float proxyAP = Mathf.Max(0, AP - apCost);
                    SLGCoreUI.Instance.apBar.slider.maxValue = MaxAP;
                    SLGCoreUI.Instance.apBar.slider.value = proxyAP;
                }
            }
            else
            {
                // 無法到達，清除路徑顯示
                ClearPathDisplay();
            }
        }
        else
        {
            // 目標位置不在NavMesh上，清除路徑顯示
            ClearPathDisplay();
        }
    }
    
    /// <summary>
    /// 清除路徑顯示
    /// </summary>
    public void ClearPathDisplay()
    {
        // 清除主要路徑LineRenderer（綠色部分）
        if (pathLineRenderer != null)
        {
            pathLineRenderer.positionCount = 0;
            pathLineRenderer.enabled = false;
        }
        
        // 清除無效路徑LineRenderer（紅色部分）
        if (invalidPathLineRenderer != null)
        {
            invalidPathLineRenderer.positionCount = 0;
            invalidPathLineRenderer.enabled = false;
        }
        
        // 重置預覽狀態
        if (isPreviewingPath)
        {
            isPreviewingPath = false;
            previewAPCost = 0f;
            
            // 恢復顯示實際的AP值
            if (SLGCoreUI.Instance != null && SLGCoreUI.Instance.apBar != null)
            {
                SLGCoreUI.Instance.apBar.slider.value = AP;
            }
        }
    }
    
    /// <summary>
    /// 繪製指定的NavMesh路徑（分段顯示綠色和紅色）
    /// </summary>
    /// <param name="path">要繪製的路徑</param>
    /// <param name="useAPColor">是否根據AP設定顏色（預設為false）</param>
    private void DrawPath(NavMeshPath path, bool useAPColor = false)
    {
        if (pathLineRenderer == null) return;
        
        Vector3[] corners = path.corners;
        
        if (corners.Length > 0)
        {
            if (useAPColor)
            {
                // 計算路徑分段
                List<Vector3> validPath, invalidPath;
                CalculatePathSegments(path, out validPath, out invalidPath);
                
                // 繪製綠色路徑段（體力足夠的部分）
                DrawPathSegment(pathLineRenderer, validPath, validPathColor);
                
                // 繪製紅色路徑段（體力不足的部分）
                if (invalidPathLineRenderer != null)
                {
                    DrawPathSegment(invalidPathLineRenderer, invalidPath, invalidPathColor);
                }
            }
            else
            {
                // 不使用AP顏色時，使用原有邏輯
                DrawPathSegment(pathLineRenderer, new List<Vector3>(corners), validPathColor);
                
                // 清除紅色路徑
                if (invalidPathLineRenderer != null)
                {
                    invalidPathLineRenderer.enabled = false;
                    invalidPathLineRenderer.positionCount = 0;
                }
            }
        }
        else
        {
            ClearPathDisplay();
        }
    }
    
    /// <summary>
    /// 繪製單一路徑段
    /// </summary>
    /// <param name="lineRenderer">要使用的LineRenderer</param>
    /// <param name="pathPoints">路徑點列表</param>
    /// <param name="color">路徑顏色</param>
    private void DrawPathSegment(LineRenderer lineRenderer, List<Vector3> pathPoints, Color color)
    {
        if (lineRenderer == null || pathPoints.Count == 0)
        {
            if (lineRenderer != null)
            {
                lineRenderer.enabled = false;
                lineRenderer.positionCount = 0;
            }
            return;
        }
        
        // 設定LineRenderer的點數
        lineRenderer.positionCount = pathPoints.Count;
        
        // 設定顏色
        if (lineRenderer.material != null)
        {
            lineRenderer.material.color = color;
        }
        else
        {
            lineRenderer.startColor = color;
            lineRenderer.endColor = color;
        }
        
        // 設定所有路徑點，稍微提高Y座標避免與地面重疊
        for (int i = 0; i < pathPoints.Count; i++)
        {
            Vector3 position = pathPoints[i];
            position.y += 0.1f; // 稍微提高避免與地面重疊
            lineRenderer.SetPosition(i, position);
        }
        
        // 確保LineRenderer已啟用
        lineRenderer.enabled = true;
    }
    
    /// <summary>
    /// 繪製NavMesh路徑（用於實際移動時）
    /// </summary>
    private IEnumerator DrawNavMeshPath()
    {
        // 等待路徑計算完成
        yield return new WaitForSeconds(0.1f);
        
        if (navMeshAgent == null || pathLineRenderer == null) yield break;
        
        // 等待路徑計算完成
        while (navMeshAgent.pathPending)
        {
            yield return null;
        }
        
        // 確認有有效路徑
        if (navMeshAgent.hasPath)
        {
            // 使用新的DrawPath方法，並設定為使用AP顏色（實際移動時路徑應該是綠色，因為已經通過AP檢查）
            DrawPath(navMeshAgent.path, true);
        }
        else
        {
            ClearPathDisplay();
        }
    }
    
    /// <summary>
    /// 計算NavMeshPath的總長度
    /// </summary>
    /// <param name="path">要計算的路徑</param>
    /// <returns>路徑總長度</returns>
    private float CalculatePathLength(NavMeshPath path)
    {
        float totalDistance = 0f;
        Vector3[] corners = path.corners;
        
        if (corners.Length < 2) return 0f;
        
        // 從當前位置到第一個點的距離
        totalDistance += Vector3.Distance(transform.position, corners[0]);
        
        // 計算路徑中每兩個點之間的距離
        for (int i = 0; i < corners.Length - 1; i++)
        {
            totalDistance += Vector3.Distance(corners[i], corners[i + 1]);
        }
        
        return totalDistance;
    }
    
    /// <summary>
    /// 計算移動到指定距離所需的AP消耗
    /// </summary>
    /// <param name="distance">移動距離</param>
    /// <returns>所需AP</returns>
    private float CalculateAPCost(float distance)
    {
        // 根據UpdateMovement中的邏輯，每單位距離消耗5點AP
        return distance * 5.0f;
    }
    
    /// <summary>
    /// 檢查是否有足夠AP到達目標位置
    /// </summary>
    /// <param name="path">要檢查的路徑</param>
    /// <returns>是否有足夠AP</returns>
    private bool HasEnoughAPForPath(NavMeshPath path)
    {
        float pathLength = CalculatePathLength(path);
        float requiredAP = CalculateAPCost(pathLength);
        return AP >= requiredAP;
    }
    
    /// <summary>
    /// 計算路徑分段點，返回綠色和紅色兩段路徑
    /// </summary>
    /// <param name="path">完整路徑</param>
    /// <param name="validPath">體力足夠的綠色路徑段</param>
    /// <param name="invalidPath">體力不足的紅色路徑段</param>
    private void CalculatePathSegments(NavMeshPath path, out List<Vector3> validPath, out List<Vector3> invalidPath)
    {
        validPath = new List<Vector3>();
        invalidPath = new List<Vector3>();
        
        Vector3[] corners = path.corners;
        if (corners.Length == 0) return;
        
        float maxWalkableDistance = AP / 5.0f; // 當前AP能走的最大距離
        float accumulatedDistance = 0f;
        Vector3 currentPos = transform.position;
        
        // 如果AP為0，整條路徑都是紅色
        if (maxWalkableDistance <= 0)
        {
            invalidPath.AddRange(corners);
            return;
        }
        
        bool foundSplitPoint = false;
        
        for (int i = 0; i < corners.Length; i++)
        {
            Vector3 corner = corners[i];
            float segmentDistance = Vector3.Distance(currentPos, corner);
            
            if (!foundSplitPoint && accumulatedDistance + segmentDistance <= maxWalkableDistance)
            {
                // 這個點還在AP範圍內，加入綠色路徑
                validPath.Add(corner);
                accumulatedDistance += segmentDistance;
            }
            else if (!foundSplitPoint)
            {
                // 找到了切分點，需要在這個線段中間分割
                foundSplitPoint = true;
                float remainingDistance = maxWalkableDistance - accumulatedDistance;
                
                if (remainingDistance > 0)
                {
                    // 計算切分點的位置
                    Vector3 direction = (corner - currentPos).normalized;
                    Vector3 splitPoint = currentPos + direction * remainingDistance;
                    
                    // 添加切分點到綠色路徑
                    validPath.Add(splitPoint);
                    
                    // 從切分點開始紅色路徑
                    invalidPath.Add(splitPoint);
                }
                
                // 添加當前點到紅色路徑
                invalidPath.Add(corner);
            }
            else
            {
                // 已經找到切分點，後續所有點都是紅色
                invalidPath.Add(corner);
            }
            
            currentPos = corner;
        }
        
        // 如果整條路徑都在AP範圍內，invalidPath會是空的
        // 如果AP完全不夠，validPath可能只有起始點附近的部分
    }
    
    /// <summary>
    /// 計算當前AP能到達的最遠位置
    /// </summary>
    /// <param name="path">完整路徑</param>
    /// <returns>最遠可達位置</returns>
    private Vector3 CalculateMaxReachablePosition(NavMeshPath path)
    {
        Vector3[] corners = path.corners;
        if (corners.Length == 0) return transform.position;
        
        float maxWalkableDistance = AP / 5.0f; // 當前AP能走的最大距離
        float accumulatedDistance = 0f;
        Vector3 currentPos = transform.position;
        
        // 如果AP為0，返回當前位置
        if (maxWalkableDistance <= 0)
        {
            return transform.position;
        }
        
        for (int i = 0; i < corners.Length; i++)
        {
            Vector3 corner = corners[i];
            float segmentDistance = Vector3.Distance(currentPos, corner);
            
            if (accumulatedDistance + segmentDistance <= maxWalkableDistance)
            {
                // 這個點還在AP範圍內
                accumulatedDistance += segmentDistance;
                currentPos = corner;
            }
            else
            {
                // 找到了切分點，需要在這個線段中間分割
                float remainingDistance = maxWalkableDistance - accumulatedDistance;
                
                if (remainingDistance > 0)
                {
                    // 計算最遠可達點的位置
                    Vector3 direction = (corner - currentPos).normalized;
                    Vector3 maxReachablePoint = currentPos + direction * remainingDistance;
                    return maxReachablePoint;
                }
                else
                {
                    // 沒有剩餘距離，返回上一個位置
                    return currentPos;
                }
            }
        }
        
        // 如果整條路徑都在體力範圍內，返回最後一個點
        return currentPos;
    }
    
    /// <summary>
    /// 處理空白鍵輸入來結束回合
    /// </summary>
    private void HandleSpaceKeyInput()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;
        
        // 檢查空白鍵是否被按下
        if (keyboard.spaceKey.isPressed)
        {
            if (!isHoldingSpace)
            {
                // 開始長壓
                isHoldingSpace = true;
                spaceKeyHoldTime = 0f;
                Debug.Log("開始長壓空白鍵...");
            }
            else
            {
                // 持續長壓，累計時間
                spaceKeyHoldTime += Time.deltaTime;
                
                // 檢查是否達到結束回合的時間
                if (spaceKeyHoldTime >= holdTimeToEndTurn)
                {
                    EndTurnBySpaceKey();
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
                Debug.Log("停止長壓空白鍵");
            }
        }
    }
    
    /// <summary>
    /// 通過空白鍵結束回合
    /// </summary>
    private void EndTurnBySpaceKey()
    {
        Debug.Log("空白鍵長壓結束回合！");
        
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
                // 計算方向和距離
                Vector3 direction = mouseWorldPos - transform.position;
                direction.y = 0; // 保持水平
                float distance = direction.magnitude;
                
                // 旋轉整個 CharacterCore 面向目標
                if (direction != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(direction);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
                }
                
                // 縮放 StraightFrontTargetingAnchor 讓 Collider 延伸到滑鼠位置
                // Cube 在本地座標 (0, 0, 0.5)，所以縮放 Z 軸
                // StraightFrontTargetingAnchor 在 (0, 1, 0.43)
                float baseDistance = 0.93f; // 0.43 + 0.5 = 0.93 (Anchor的z + Cube的z)
                float scaleZ = distance / baseDistance;
                
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
                Debug.Log($"[CharacterCore] 技能路徑被 Floor 層阻擋！碰撞物件數量: {collidingObjects.Count}");
                
                // 變更Cube顏色為酒紅色
                if (cubeRenderer != null && cubeRenderer.material != null)
                {
                    cubeRenderer.material.color = blockedColor;
                }
            }
            else
            {
                Debug.Log("[CharacterCore] 技能路徑暢通，可以使用技能");
                
                // 恢復Cube的原始顏色
                if (cubeRenderer != null && cubeRenderer.material != null)
                {
                    cubeRenderer.material.color = originalCubeColor;
                }
            }
        }
    }

    
}