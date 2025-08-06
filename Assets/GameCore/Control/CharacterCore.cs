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
    public float Stamina = 100;
    public float MaxStamina = 100;
    public float SP = 100;
    public float MaxSP = 100;
    public Vector2 lastPosition;
    
    [Header("預覽系統")]
    private bool isPreviewingPath = false;     // 是否正在預覽路徑
    private float previewStaminaCost = 0f;     // 預覽的體力消耗量
    
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
    public Animator CharacterExecuteAnimator;
    
    [Header("動畫根運動控制")]
    public AnimationRelativePos executorAnimationRelativePos;
    public AnimationRelativePos controllerAnimationRelativePos;
    
    public GameObject characterExecutor;
    
    [Header("技能配置")]
    public CombatSkill skillA;
    public CombatSkill skillB;
    public CombatSkill skillC;
    public CombatSkill skillD;

    public enum CharacterCoreState{ ControlState, ExcutionState, UsingSkill, ExecutingSkill, TurnComplete }

    public CharacterCoreState nowState = CharacterCoreState.ControlState;
    
    void Start()
    {
        AddPoint(new Vector3(transform.position.x, 0.1f, transform.position.z));
        
        // 初始化NavMeshAgent
        InitializeNavMeshAgent();
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
                

                Stamina -= distance * 5.0f;
                Stamina = Mathf.Max(0, Stamina); // 確保體力不會變負數
                
                // 如果體力耗盡，立即停止移動
                if (Stamina <= 0)
                {
                    Debug.Log("體力耗盡，停止移動！");
                    StopMovement();
                    return;
                }
                
                // 更新UI（只在非預覽狀態下更新）
                if (!isPreviewingPath && SLGCoreUI.Instance != null && SLGCoreUI.Instance.apBar != null)
                {
                    SLGCoreUI.Instance.apBar.slider.maxValue = MaxStamina;
                    SLGCoreUI.Instance.apBar.slider.value = Stamina;
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
        
        // 檢查是否有足夠體力
        if (Stamina <= 0)
        {
            Debug.Log("體力不足，無法移動！");
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
                
                // 檢查體力是否足夠完成這段路徑
                if (!HasEnoughStaminaForPath(path))
                {
                    // 體力不足，計算最遠能到達的位置
                    actualDestination = CalculateMaxReachablePosition(path);
                    
                    if (Vector3.Distance(transform.position, actualDestination) < 0.5f)
                    {
                        // 如果最遠距離太近，就不移動
                        Debug.Log("體力不足，無法進行有效移動！");
                        return;
                    }
                    
                    Debug.Log($"體力不足到達目標，移動到最遠可達位置");
                }
                else
                {
                    // 體力足夠，移動到目標位置
                    actualDestination = hit.position;
                    Debug.Log("體力足夠，移動到目標位置");
                }
                
                targetPosition = actualDestination;
                hasValidTarget = true;
                
                // 重置預覽狀態，回到顯示實際Stamina值
                if (isPreviewingPath)
                {
                    isPreviewingPath = false;
                    previewStaminaCost = 0f;
                    
                    // 恢復顯示實際的Stamina值
                    if (SLGCoreUI.Instance != null && SLGCoreUI.Instance.apBar != null)
                    {
                        SLGCoreUI.Instance.apBar.slider.value = Stamina;
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
        if (navMeshAgent == null || Stamina <= 0) return false;
        
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
        Stamina = MaxStamina;
        SP = MaxSP;
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
            
            // 滑鼠操作邏輯將在這裡實作
            // 技能使用和確認都將改為滑鼠控制
        }
        else if (nowState == CharacterCoreState.ExcutionState)
        {
            ExecutorUpdate();
            ReFillAP();
        }
        else if (nowState == CharacterCoreState.ExecutingSkill)
        {
            AnimatorStateInfo stateInfo = CharacterExecuteAnimator.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.normalizedTime >= 1.0f)
            {
                // CharacterControllerForExecutor已移除
                
                // 技能執行完成，禁用碰撞防護
                if (executorAnimationRelativePos != null)
                {
                    executorAnimationRelativePos.SetCollisionProtection(false);
                }
                
                nowState = CharacterCoreState.TurnComplete;
            }
        }
        else if (nowState == CharacterCoreState.UsingSkill)
        {
            AnimatorStateInfo stateInfo = CharacterControlAnimator.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.normalizedTime >= 1.0f)
            {
                // 技能動畫完成，禁用控制階段的碰撞防護
                if (controllerAnimationRelativePos != null)
                {
                    controllerAnimationRelativePos.SetCollisionProtection(false);
                }
                
                nowState = CharacterCoreState.ControlState;
            }
        }
    }
    
    public void ExecutorUpdate()
    {
        if (RecordedActions.Count != 0)
        {
            
            CombatAction tempAction = RecordedActions.Dequeue();
            if (tempAction.type == CombatAction.ActionType.Move)
            {
                Vector3 pos = tempAction.Position;
                Quaternion rot = tempAction.rotation;
                characterExecutor.transform.position = pos;
                characterExecutor.transform.rotation = rot;
            }
            else if (tempAction.type == CombatAction.ActionType.SkillA)
            {
                Debug.Log("SkillA");
                // CharacterControllerForExecutor已移除
                
                // 啟用動畫根運動碰撞防護
                if (executorAnimationRelativePos != null)
                {
                    executorAnimationRelativePos.SetCollisionProtection(true);
                }
                
                if (skillA != null)
                    CharacterExecuteAnimator.Play(skillA.AnimationName);
                else
                    CharacterExecuteAnimator.Play("SkillA");
                nowState = CharacterCoreState.ExecutingSkill;
            }
            else if (tempAction.type == CombatAction.ActionType.SkillB)
            {
                Debug.Log("SkillB");
                // CharacterControllerForExecutor已移除
                
                // 啟用動畫根運動碰撞防護
                if (executorAnimationRelativePos != null)
                {
                    executorAnimationRelativePos.SetCollisionProtection(true);
                }
                
                if (skillB != null)
                    CharacterExecuteAnimator.Play(skillB.AnimationName);
                else
                    CharacterExecuteAnimator.Play("SkillB");
                nowState = CharacterCoreState.ExecutingSkill;
            }
            else if (tempAction.type == CombatAction.ActionType.SkillC)
            {
                Debug.Log("SkillC");
                // CharacterControllerForExecutor已移除
                
                // 啟用動畫根運動碰撞防護
                if (executorAnimationRelativePos != null)
                {
                    executorAnimationRelativePos.SetCollisionProtection(true);
                }
                
                if (skillC != null)
                    CharacterExecuteAnimator.Play(skillC.AnimationName);
                else
                    CharacterExecuteAnimator.Play("SkillC");
                nowState = CharacterCoreState.ExecutingSkill;
            }
            else if (tempAction.type == CombatAction.ActionType.SkillD)
            {
                Debug.Log("SkillD");
                // CharacterControllerForExecutor已移除
                
                // 啟用動畫根運動碰撞防護
                if (executorAnimationRelativePos != null)
                {
                    executorAnimationRelativePos.SetCollisionProtection(true);
                }
                
                if (skillD != null)
                    CharacterExecuteAnimator.Play(skillD.AnimationName);
                else
                    CharacterExecuteAnimator.Play("SkillD");
                nowState = CharacterCoreState.ExecutingSkill;
            }
        }
        else
        {
            // 執行完成，標記回合結束
            nowState = CharacterCore.CharacterCoreState.TurnComplete;
        }
    }
    
    /// <summary>
    /// 獲取玩家的真實位置（考慮 ExecutionState 時的 CharacterExecutor 位置）
    /// </summary>
    /// <returns>玩家的真實世界位置</returns>
    public Vector3 GetRealPosition()
    {
        // 如果在執行狀態且有 characterExecutor，使用 executor 的位置
        if ((nowState == CharacterCoreState.ExcutionState || nowState == CharacterCoreState.ExecutingSkill) 
            && characterExecutor != null)
        {
            return characterExecutor.transform.position;
        }
        
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
        if ((nowState == CharacterCoreState.ExcutionState || nowState == CharacterCoreState.ExecutingSkill) 
            && characterExecutor != null)
        {
            return characterExecutor.transform;
        }
        
        // 否則使用主物件的 transform
        return transform;
    }
    
    /// <summary>
    /// 在回合結束時同步位置（將主物件位置更新為 CharacterExecutor 的位置）
    /// </summary>
    public void SyncPositionAfterExecution()
    {
        if (characterExecutor != null)
        {
            // 計算位置差異
            Vector3 executorPos = characterExecutor.transform.position;
            Vector3 mainPos = transform.position;
            
            Debug.Log($"同步位置: Main({mainPos}) -> Executor({executorPos})");
            
            // 更新主物件的位置
            transform.position = executorPos;
            transform.rotation = characterExecutor.transform.rotation;
        }
    }

    // 技能檢查方法將改為滑鼠/UI控制
    public bool CanUseSkillA()
    {
        return skillA != null && SP >= skillA.SPCost;
    }
    
    public bool CanUseSkillB()
    {
        return skillB != null && SP >= skillB.SPCost;
    }
    
    public bool CanUseSkillC()
    {
        return skillC != null && SP >= skillC.SPCost;
    }
    
    public bool CanUseSkillD()
    {
        return skillD != null && SP >= skillD.SPCost;
    }
    
    public void UseSkill(CombatSkill skill, CombatAction.ActionType actionType)
    {
        if (skill != null && SP >= skill.SPCost)
        {
            CombatAction tempActionMove = new CombatAction();
            nowState = CharacterCoreState.UsingSkill;
            tempActionMove.Position = transform.position;
            tempActionMove.rotation = transform.rotation;
            tempActionMove.type = CombatAction.ActionType.Move;
            RecordedActions.Enqueue(tempActionMove);
            
            CombatAction tempActionSkill = new CombatAction();
            tempActionSkill.type = actionType;
            
            // 啟用控制階段的根運動碰撞防護
            if (controllerAnimationRelativePos != null)
            {
                controllerAnimationRelativePos.SetCollisionProtection(true);
            }
            
            CharacterControlAnimator.Play(skill.AnimationName);
            SP -= skill.SPCost;
            
            RecordedActions.Enqueue(tempActionSkill);
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
                // 檢查體力是否足夠
                bool hasEnoughStamina = HasEnoughStaminaForPath(path);
                
                // 繪製分段路徑（綠色+紅色）
                DrawPath(path, true);
                
                // 顯示路徑長度和體力消耗的Debug訊息
                float pathLength = CalculatePathLength(path);
                float staminaCost = CalculateStaminaCost(pathLength);
                Debug.Log($"路徑長度: {pathLength:F2}, 體力消耗: {staminaCost:F2}, 當前體力: {Stamina:F2}, 體力{(hasEnoughStamina ? "足夠" : "不足")}");
                
                // 設定預覽狀態
                isPreviewingPath = true;
                previewStaminaCost = staminaCost;
                
                // 使用Proxy值更新UI（顯示預期的剩餘體力）
                if (SLGCoreUI.Instance != null && SLGCoreUI.Instance.apBar != null)
                {
                    float proxyStamina = Mathf.Max(0, Stamina - staminaCost);
                    SLGCoreUI.Instance.apBar.slider.maxValue = MaxStamina;
                    SLGCoreUI.Instance.apBar.slider.value = proxyStamina;
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
            previewStaminaCost = 0f;
            
            // 恢復顯示實際的Stamina值
            if (SLGCoreUI.Instance != null && SLGCoreUI.Instance.apBar != null)
            {
                SLGCoreUI.Instance.apBar.slider.value = Stamina;
            }
        }
    }
    
    /// <summary>
    /// 繪製指定的NavMesh路徑（分段顯示綠色和紅色）
    /// </summary>
    /// <param name="path">要繪製的路徑</param>
    /// <param name="useStaminaColor">是否根據體力設定顏色（預設為false）</param>
    private void DrawPath(NavMeshPath path, bool useStaminaColor = false)
    {
        if (pathLineRenderer == null) return;
        
        Vector3[] corners = path.corners;
        
        if (corners.Length > 0)
        {
            if (useStaminaColor)
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
                // 不使用體力顏色時，使用原有邏輯
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
            // 使用新的DrawPath方法，並設定為使用體力顏色（實際移動時路徑應該是綠色，因為已經通過體力檢查）
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
    /// 計算移動到指定距離所需的體力消耗
    /// </summary>
    /// <param name="distance">移動距離</param>
    /// <returns>所需體力</returns>
    private float CalculateStaminaCost(float distance)
    {
        // 根據UpdateMovement中的邏輯，每單位距離消耗5點體力
        return distance * 5.0f;
    }
    
    /// <summary>
    /// 檢查是否有足夠體力到達目標位置
    /// </summary>
    /// <param name="path">要檢查的路徑</param>
    /// <returns>是否有足夠體力</returns>
    private bool HasEnoughStaminaForPath(NavMeshPath path)
    {
        float pathLength = CalculatePathLength(path);
        float requiredStamina = CalculateStaminaCost(pathLength);
        return Stamina >= requiredStamina;
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
        
        float maxWalkableDistance = Stamina / 5.0f; // 當前體力能走的最大距離
        float accumulatedDistance = 0f;
        Vector3 currentPos = transform.position;
        
        // 如果體力為0，整條路徑都是紅色
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
                // 這個點還在體力範圍內，加入綠色路徑
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
        
        // 如果整條路徑都在體力範圍內，invalidPath會是空的
        // 如果體力完全不夠，validPath可能只有起始點附近的部分
    }
    
    /// <summary>
    /// 計算當前體力能到達的最遠位置
    /// </summary>
    /// <param name="path">完整路徑</param>
    /// <returns>最遠可達位置</returns>
    private Vector3 CalculateMaxReachablePosition(NavMeshPath path)
    {
        Vector3[] corners = path.corners;
        if (corners.Length == 0) return transform.position;
        
        float maxWalkableDistance = Stamina / 5.0f; // 當前體力能走的最大距離
        float accumulatedDistance = 0f;
        Vector3 currentPos = transform.position;
        
        // 如果體力為0，返回當前位置
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
                // 這個點還在體力範圍內
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

    
}