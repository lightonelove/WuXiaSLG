using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 角色移動系統 - 負責NavMesh移動、路徑預覽、AP消耗計算
/// </summary>
public class CharacterMovement : MonoBehaviour
{
    [Header("移動設定")]
    public float moveSpeed = 20f;
    public float turnRate = 720f;
    
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
    
    [Header("預覽系統")]
    private bool isPreviewingPath = false;     // 是否正在預覽路徑
    private float previewAPCost = 0f;          // 預覽的AP消耗量
    
    // 對其他組件的引用
    private CharacterCore characterCore;
    private CharacterResources characterResources;
    
    void Awake()
    {
        // 獲取同一GameObject上的其他組件
        characterCore = GetComponent<CharacterCore>();
        characterResources = characterCore.characterResources;
    }
    
    void Start()
    {
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
            Debug.LogError("CharacterMovement需要NavMeshAgent元件！");
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
    
    /// <summary>
    /// 更新移動狀態（從CharacterCore的Update調用）
    /// </summary>
    public void UpdateMovement()
    {
        if (navMeshAgent == null || characterResources == null) return;
        
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
                float distance = Vector2.Distance(characterResources.lastPosition, nowPosition);
                
                characterResources.ConsumeAP(distance * 5.0f);
                
                // 如果AP耗盡，立即停止移動
                if (characterResources.AP <= 0)
                {
                    Debug.Log("AP耗盡，停止移動");
                    StopMovement();
                    return;
                }
                
                // 更新lastPosition為當前位置，以便下一幀計算
                characterResources.lastPosition = nowPosition;
                
                // 更新動畫
                if (characterCore != null && characterCore.CharacterControlAnimator != null)
                {
                    characterCore.CharacterControlAnimator.SetBool("isMoving", true);
                }
            }
        }
        else
        {
            // 不在移動，停止動畫
            if (characterCore != null && characterCore.CharacterControlAnimator != null)
            {
                characterCore.CharacterControlAnimator.SetBool("isMoving", false);
            }
        }
    }
    
    /// <summary>
    /// 移動到指定位置
    /// </summary>
    /// <param name="destination">目標位置</param>
    public void MoveTo(Vector3 destination)
    {
        if (navMeshAgent == null || characterResources == null) return;
        
        // 檢查是否有足夠AP
        if (characterResources.AP <= 0)
        {
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
                    
                    if (Vector3.Distance(transform.position, actualDestination) < 0.3f)
                    {
                        // 如果最遠距離太近，就不移動
                        return;
                    }
                }
                else
                {
                    // AP足夠，移動到目標位置
                    actualDestination = hit.position;
                }
                
                targetPosition = actualDestination;
                hasValidTarget = true;
                
                // 重置預覽狀態，回到顯示實際AP值
                if (isPreviewingPath)
                {
                    isPreviewingPath = false;
                    previewAPCost = 0f;
                    characterResources.UpdateAPDisplay();
                }
                
                // 設定NavMeshAgent目標
                navMeshAgent.isStopped = false;
                navMeshAgent.SetDestination(targetPosition);
                
                // 繪製導航路徑
                StartCoroutine(DrawNavMeshPath());
                
                // 初始化lastPosition為當前位置，用於計算移動消耗的AP
                characterResources.lastPosition = new Vector2(transform.position.x, transform.position.z);
                
                isMoving = true;
            }
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
        if (characterCore != null && characterCore.CharacterControlAnimator != null)
        {
            characterCore.CharacterControlAnimator.SetBool("isMoving", false);
        }
        
        // 清除路徑顯示（包括兩個LineRenderer）
        ClearPathDisplay();
    }
    
    /// <summary>
    /// 檢查是否可以移動到指定位置
    /// </summary>
    /// <param name="destination">目標位置</param>
    /// <returns>是否可以移動</returns>
    public bool CanMoveTo(Vector3 destination)
    {
        if (navMeshAgent == null || characterResources == null || characterResources.AP <= 0) return false;
        
        NavMeshHit hit;
        return NavMesh.SamplePosition(destination, out hit, 2f, NavMesh.AllAreas);
    }
    
    /// <summary>
    /// 預覽到指定位置的路徑（不執行移動）
    /// </summary>
    /// <param name="destination">目標位置</param>
    public void PreviewPath(Vector3 destination)
    {
        
        if (navMeshAgent == null || pathLineRenderer == null || characterResources == null) return;
        
        Debug.Log("PreviewPathing");
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
                
                // 設定預覽狀態
                isPreviewingPath = true;
                previewAPCost = apCost;
                
                // 使用Proxy值更新UI（顯示預期的剩餘AP）
                characterResources.ShowPreviewAP(apCost);
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
            
            if (characterResources != null)
            {
                characterResources.UpdateAPDisplay();
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
        if (characterResources == null) return false;
        
        float pathLength = CalculatePathLength(path);
        float requiredAP = CalculateAPCost(pathLength);
        return characterResources.AP >= requiredAP;
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
        if (corners.Length == 0 || characterResources == null) return;
        
        float maxWalkableDistance = characterResources.AP / 5.0f; // 當前AP能走的最大距離
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
    }
    
    /// <summary>
    /// 計算當前AP能到達的最遠位置
    /// </summary>
    /// <param name="path">完整路徑</param>
    /// <returns>最遠可達位置</returns>
    private Vector3 CalculateMaxReachablePosition(NavMeshPath path)
    {
        Vector3[] corners = path.corners;
        if (corners.Length == 0 || characterResources == null) return transform.position;
        
        float maxWalkableDistance = characterResources.AP / 5.0f; // 當前AP能走的最大距離
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
}