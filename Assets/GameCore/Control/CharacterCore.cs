using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
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
    public LineRenderer pathLineRenderer;
    
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
            if (!navMeshAgent.pathPending && navMeshAgent.remainingDistance < 0.5f)
            {
                // 到達目標，停止移動
                StopMovement();
            }
            else
            {
                // 正在移動，消耗體力
                Vector2 nowPosition = new Vector2(transform.position.x, transform.position.z);
                float distance = Vector2.Distance(lastPosition, nowPosition);
                
                if (distance > 0.01f) // 避免微小移動消耗體力
                {
                    Stamina -= distance * 5.0f;
                    Stamina = Mathf.Max(0, Stamina); // 確保體力不會變負數
                    
                    // 更新UI
                    if (SLGCoreUI.Instance != null && SLGCoreUI.Instance.apBar != null)
                    {
                        SLGCoreUI.Instance.apBar.slider.maxValue = MaxStamina;
                        SLGCoreUI.Instance.apBar.slider.value = Stamina;
                    }
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
            targetPosition = hit.position;
            hasValidTarget = true;
            
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
        
        // 清除路徑顯示
        if (pathLineRenderer != null)
        {
            pathLineRenderer.positionCount = 0;
            pathLineRenderer.enabled = false;
        }
        
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
                // 繪製路徑
                DrawPath(path);
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
        if (pathLineRenderer != null)
        {
            pathLineRenderer.positionCount = 0;
            pathLineRenderer.enabled = false;
        }
    }
    
    /// <summary>
    /// 繪製指定的NavMesh路徑
    /// </summary>
    /// <param name="path">要繪製的路徑</param>
    private void DrawPath(NavMeshPath path)
    {
        if (pathLineRenderer == null) return;
        
        Vector3[] corners = path.corners;
        
        if (corners.Length > 0)
        {
            // 設定LineRenderer的點數
            pathLineRenderer.positionCount = corners.Length;
            
            // 設定所有路徑點，稍微提高Y座標避免與地面重疊
            for (int i = 0; i < corners.Length; i++)
            {
                Vector3 cornerPosition = corners[i];
                cornerPosition.y += 0.1f; // 稍微提高避免與地面重疊
                pathLineRenderer.SetPosition(i, cornerPosition);
            }
            
            // 確保LineRenderer已啟用
            pathLineRenderer.enabled = true;
        }
        else
        {
            ClearPathDisplay();
        }
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
            // 使用新的DrawPath方法
            DrawPath(navMeshAgent.path);
        }
        else
        {
            ClearPathDisplay();
        }
    }

    
}