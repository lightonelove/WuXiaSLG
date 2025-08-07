using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class AnimationMoveScaler3D : MonoBehaviour
{
    [System.Serializable]
    public enum EvaluateType
    {
        ToTarget,       // 朝向指定目標
        FarPoint,       // 朝向最遠的點
        ClickPosition   // 朝向點擊位置
    }

    public Animator animator;
    [Header("評估設定")]
    public EvaluateType evaluateType = EvaluateType.ToTarget;
    public bool isContinuousUpdate = false;
    
    [Header("距離與偏移設定")]
    public float referenceDistance = 5f;        // 參考距離（負值代表後退）
    public float evaluateOffset = -1f;          // 評估偏移量（負值表示在目標前方）
    public float moveMinScale = 0.5f;           // 最小位移倍率
    public float moveMaxScale = 1.5f;           // 最大位移倍率
    
    [Header("目標設定")]
    public Transform evaluateTarget;            // 評估目標
    public List<Transform> evaluatePoints;      // 評估點列表
    public Vector3 clickPosition;               // 點擊位置
    
    [Header("Debug")]
    [SerializeField] private float currentMoveScale = 1f;
    [SerializeField] private float refDistanceRemain = 0f;
    [SerializeField] private Vector3 targetDirection = Vector3.forward;
    
    private bool enabledEvaluateInUpdate = false;
    private float refDir = 1f;

    private void OnEnable()
    {
        enabledEvaluateInUpdate = true;
    }

    private void OnDisable()
    {
        ClearEvaluate();
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;
        
        Gizmos.color = Color.cyan;
        
        // 顯示目標方向
        Vector3 forward = transform.forward;
        Vector3 offsetPos = transform.position + forward * evaluateOffset;
        
        Gizmos.DrawLine(transform.position + Vector3.up * 2, offsetPos + Vector3.up * 2);
        Gizmos.DrawWireSphere(offsetPos + Vector3.up * 2, 0.5f);
        
        // 顯示目標位置
        if (evaluateTarget != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(offsetPos + Vector3.up * 2, evaluateTarget.position + Vector3.up * 2);
        }
        else if (evaluateType == EvaluateType.ClickPosition)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(offsetPos + Vector3.up * 2, clickPosition + Vector3.up * 2);
        }
        
        Handles.Label(offsetPos + Vector3.up * 3, $"Scale: {currentMoveScale:F2}\nRemain: {refDistanceRemain:F2}");
    }
#endif

    /// <summary>
    /// 動畫移動時調用，返回當前的位移倍率
    /// </summary>
    /// <param name="deltaMovement">當前幀的位移量</param>
    /// <returns>位移倍率</returns>
    public float AnimatorMoved(Vector3 deltaMovement)
    {
        if (enabledEvaluateInUpdate) // 第一次調用
        {
            EvaluatePosition();
            enabledEvaluateInUpdate = false;
        }
        else if (isContinuousUpdate)
        {
            EvaluateUpdate();
            
            // 減掉已經移動的部分（只計算在目標方向上的移動）
            float movedInTargetDirection = Vector3.Dot(deltaMovement, targetDirection);
            refDistanceRemain -= Mathf.Abs(movedInTargetDirection);
            if (refDistanceRemain < 0)
                refDistanceRemain = 0;
        }
        
        return currentMoveScale;
    }
    
    /// <summary>
    /// 設定點擊位置作為目標
    /// </summary>
    /// <param name="clickPos">點擊的世界位置</param>
    public void SetClickPosition(Vector3 clickPos)
    {
        clickPosition = clickPos;
        evaluateType = EvaluateType.ClickPosition;
        enabledEvaluateInUpdate = true;
    }
    
    /// <summary>
    /// 設定目標Transform
    /// </summary>
    /// <param name="target">目標Transform</param>
    public void SetTarget(Transform target)
    {
        evaluateTarget = target;
        evaluateType = EvaluateType.ToTarget;
        enabledEvaluateInUpdate = true;
    }

    /// <summary>
    /// 評估並計算初始位置
    /// </summary>
    public void EvaluatePosition()
    {
        refDistanceRemain = Mathf.Abs(referenceDistance * transform.lossyScale.magnitude);
        refDir = Mathf.Sign(referenceDistance);
        
        EvaluateUpdate();
    }

    /// <summary>
    /// 更新位移倍率計算
    /// </summary>
    private float EvaluateUpdate()
    {
        Vector3 targetPosition = GetTargetPosition();
        
        if (targetPosition == Vector3.zero)
        {
            currentMoveScale = 1f;
            return currentMoveScale;
        }
        
        // 計算面向方向（使用transform.forward）
        Vector3 forward = transform.forward;
        targetDirection = forward;
        
        // 計算偏移後的位置（我想要攻擊落點的位置）
        Vector3 offsetPos = transform.position + forward * evaluateOffset * transform.lossyScale.magnitude;
        
        // 計算需要移動的距離向量
        Vector3 shouldMoveVector = targetPosition - offsetPos;
        
        // 投影到前進方向上，得到需要在前進方向上移動的距離
        float shouldMoveDistance = Vector3.Dot(shouldMoveVector, forward);
        
        // 應用方向修正
        shouldMoveDistance *= refDir;
        
        // 根據剩餘參考距離計算倍率
        if (refDistanceRemain > 0.001f)
        {
            currentMoveScale = shouldMoveDistance / refDistanceRemain;
        }
        else
        {
            currentMoveScale = 0f;
        }
        
        // 限制倍率範圍
        currentMoveScale = Mathf.Clamp(currentMoveScale, moveMinScale, moveMaxScale);
        
        Debug.Log($"[AnimationMoveScaler3D] 目標: {targetPosition}, 需要移動: {shouldMoveDistance:F2}, 剩餘距離: {refDistanceRemain:F2}, 倍率: {currentMoveScale:F2}");
        
        return currentMoveScale;
    }

    /// <summary>
    /// 根據評估類型獲取目標位置
    /// </summary>
    /// <returns>目標位置</returns>
    private Vector3 GetTargetPosition()
    {
        switch (evaluateType)
        {
            case EvaluateType.ToTarget:
                return evaluateTarget != null ? evaluateTarget.position : Vector3.zero;
                
            case EvaluateType.FarPoint:
                Transform farPoint = GetFarPoint();
                return farPoint != null ? farPoint.position : Vector3.zero;
                
            case EvaluateType.ClickPosition:
                return clickPosition;
                
            default:
                return Vector3.zero;
        }
    }

    /// <summary>
    /// 獲取最遠的評估點
    /// </summary>
    /// <returns>最遠的Transform</returns>
    private Transform GetFarPoint()
    {
        if (evaluatePoints == null || evaluatePoints.Count == 0)
            return null;
            
        Transform result = evaluatePoints[0];
        float farDistance = Vector3.Distance(transform.position, evaluatePoints[0].position);

        foreach (var point in evaluatePoints)
        {
            if (point == null) continue;
            
            float newDistance = Vector3.Distance(transform.position, point.position);
            if (newDistance > farDistance)
            {
                farDistance = newDistance;
                result = point;
            }
        }

        return result;
    }

    /// <summary>
    /// 清除評估狀態
    /// </summary>
    public void ClearEvaluate()
    {
        evaluateTarget = null;
        currentMoveScale = 1f;
        refDistanceRemain = 0f;
        enabledEvaluateInUpdate = false;
    }

    /// <summary>
    /// 獲取當前的位移倍率
    /// </summary>
    /// <returns>當前倍率</returns>
    public float GetCurrentScale()
    {
        return currentMoveScale;
    }

    /// <summary>
    /// 檢查是否準備好進行評估
    /// </summary>
    /// <returns>是否準備好</returns>
    public bool IsReadyForEvaluation()
    {
        return GetTargetPosition() != Vector3.zero;
    }
    
    void OnAnimatorMove()
    {
        Vector3 deltaPosition = animator.deltaPosition;
        float scale = AnimatorMoved(deltaPosition);

        // 應用縮放後的位移
        transform.position += deltaPosition * scale;
    }

}