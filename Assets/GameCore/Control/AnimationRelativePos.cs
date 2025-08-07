using UnityEngine;
using System.Collections.Generic;

public class AnimationRelativePos : MonoBehaviour
{
    [Header("動畫根運動設定")]
    public Animator animator;
    
    [Header("碰撞器設定")]
    [SerializeField] private float colliderRadius = 0.5f; // 碰撞檢測半徑
    [SerializeField] private float colliderHeight = 2f; // 碰撞檢測高度
    
    [Header("碰撞防護設定")]
    [SerializeField] private LayerMask wallLayerMask = -1; // 牆壁圖層遮罩
    [SerializeField] private float collisionCheckDistance = 2f; // 碰撞檢查距離
    [SerializeField] private bool enableCollisionProtection = true; // 啟用碰撞防護
    
    [Header("調試設定")]
    [SerializeField] private bool enableDebugVisualization = false; // 啟用調試視覺化
    [SerializeField] private Color debugCollisionColor = Color.red; // 碰撞點顏色
    [SerializeField] private float debugCubeSize = 0.3f; // 調試方塊大小
    
    [Header("標籤忽略設定")]
    [SerializeField] private List<string> ignoreTags = new List<string> { "Player", "Enemy" }; // 要忽略的標籤列表
    
    private Vector3 lastSafePosition;
    private bool rootMotionBlocked = false;
    private Vector3 lastCollisionPoint; // 最後碰撞點
    private string lastCollisionObjectName = ""; // 最後碰撞物體名稱
    private bool hasCollisionDebugInfo = false; // 是否有碰撞調試信息
    
    void Start()
    {
        lastSafePosition = transform.position;
        Debug.Log("[AnimationRelativePos] 使用Transform根運動模式");
    }

    void OnAnimatorMove()
    {
        if (animator == null) return;
            
        Vector3 deltaPosition = animator.deltaPosition;
        Quaternion deltaRotation = animator.deltaRotation;
        
        // 如果啟用碰撞防護，檢查移動是否安全
        if (enableCollisionProtection && deltaPosition.magnitude > 0.001f)
        {
            if (CheckWallCollision(deltaPosition))
            {
                // 碰撞到牆壁，阻止位移但允許旋轉
                rootMotionBlocked = true;
                hasCollisionDebugInfo = true;
                Debug.Log($"根運動被牆壁阻擋，停止位移。碰撞物體: {lastCollisionObjectName}，碰撞點: {lastCollisionPoint}");
                
                // 只應用旋轉
                transform.rotation = deltaRotation * transform.rotation;
                return;
            }
            else
            {
                rootMotionBlocked = false;
                hasCollisionDebugInfo = false;
            }
        }
        
        // 直接操作Transform應用根運動位移
        transform.position += deltaPosition;
        
        // 應用旋轉
        transform.rotation = deltaRotation * transform.rotation;
        
        // 更新安全位置
        lastSafePosition = transform.position;
    }
    
    /// <summary>
    /// 檢查指定移動方向是否會碰撞到牆壁
    /// </summary>
    /// <param name="movement">預期的移動向量</param>
    /// <returns>如果會碰撞到牆壁則返回true</returns>
    private bool CheckWallCollision(Vector3 movement)
    {
        Vector3 rayDirection = movement.normalized;
        float radius = colliderRadius;
        float height = colliderHeight;
        
        // 計算射線起始位置，往移動方向外推避免射到自己
        float pushOutDistance = radius + 0.1f;
        Vector3 rayStart = transform.position + Vector3.up * (height * 0.2f) + rayDirection * pushOutDistance;
        
        // 調整射線檢測距離，扣除已經外推的距離
        float rayDistance = movement.magnitude + collisionCheckDistance;
        
        // 執行射線檢測
        RaycastHit hit;
        if (Physics.Raycast(rayStart, rayDirection, out hit, rayDistance, wallLayerMask))
        {
            // 記錄碰撞信息用於調試
            lastCollisionPoint = hit.point;
            lastCollisionObjectName = hit.collider.name;
            
            // 檢查碰撞物體是否為自己
            if (hit.collider.transform == transform || hit.collider.transform.IsChildOf(transform))
            {
                Debug.Log($"忽略碰撞（自己）: {hit.collider.name}");
                return false;
            }
            
            // 檢查碰撞物體的標籤，確保不在忽略列表中
            if (!ShouldIgnoreCollider(hit.collider))
            {
                Debug.Log($"射線檢測到碰撞: {hit.collider.name}, 距離: {hit.distance}, 點: {hit.point}");
                
                // 進一步檢查：使用膠囊檢測來更精確地判斷碰撞
                Vector3 capsuleBottom = transform.position + Vector3.up * radius;
                Vector3 capsuleTop = transform.position + Vector3.up * (height - radius);
                
                // 檢查移動後的位置是否會碰撞
                Collider[] overlapping = Physics.OverlapCapsule(capsuleBottom + movement, capsuleTop + movement, 
                    radius + 0.3f, wallLayerMask);
                
                foreach (Collider col in overlapping)
                {
                    // 忽略自己的碰撞器
                    if (col.transform == transform || col.transform.IsChildOf(transform))
                        continue;
                        
                    // 忽略標籤列表中的物體
                    if (ShouldIgnoreCollider(col))
                        continue;
                        
                    Debug.Log($"膠囊檢測確認碰撞: {col.name}");
                    return true;
                }
                
                Debug.Log($"膠囊檢測未確認碰撞，允許移動");
            }
            else
            {
                Debug.Log($"忽略碰撞（標籤在忽略列表中）: {hit.collider.name}");
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// 強制重置到安全位置（外部調用）
    /// </summary>
    public void ResetToSafePosition()
    {
        transform.position = lastSafePosition;
        rootMotionBlocked = false;
        Debug.Log("角色位置已重置到安全位置");
    }
    
    /// <summary>
    /// 設定碰撞防護開關
    /// </summary>
    /// <param name="enabled">是否啟用碰撞防護</param>
    public void SetCollisionProtection(bool enabled)
    {
        enableCollisionProtection = enabled;
        if (!enabled)
        {
            rootMotionBlocked = false;
        }
    }
    
    /// <summary>
    /// 檢查根運動是否被阻擋
    /// </summary>
    /// <returns>如果根運動被阻擋則返回true</returns>
    public bool IsRootMotionBlocked()
    {
        return rootMotionBlocked;
    }
    
    /// <summary>
    /// 檢查碰撞器是否應該被忽略
    /// </summary>
    /// <param name="collider">要檢查的碰撞器</param>
    /// <returns>如果應該忽略則返回true</returns>
    private bool ShouldIgnoreCollider(Collider collider)
    {
        if (collider == null) return true;
        
        // 檢查所有忽略標籤
        foreach (string tag in ignoreTags)
        {
            if (collider.CompareTag(tag))
            {
                return true;
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// 添加要忽略的標籤
    /// </summary>
    /// <param name="tag">要添加的標籤</param>
    public void AddIgnoreTag(string tag)
    {
        if (!string.IsNullOrEmpty(tag) && !ignoreTags.Contains(tag))
        {
            ignoreTags.Add(tag);
        }
    }
    
    /// <summary>
    /// 移除要忽略的標籤
    /// </summary>
    /// <param name="tag">要移除的標籤</param>
    public void RemoveIgnoreTag(string tag)
    {
        if (!string.IsNullOrEmpty(tag))
        {
            ignoreTags.Remove(tag);
        }
    }
    
    /// <summary>
    /// 清空所有忽略標籤
    /// </summary>
    public void ClearIgnoreTags()
    {
        ignoreTags.Clear();
    }
    
    /// <summary>
    /// 獲取當前忽略標籤列表的副本
    /// </summary>
    /// <returns>忽略標籤列表的副本</returns>
    public List<string> GetIgnoreTags()
    {
        return new List<string>(ignoreTags);
    }
    
    /// <summary>
    /// 設定碰撞器參數
    /// </summary>
    /// <param name="radius">碰撞器半徑</param>
    /// <param name="height">碰撞器高度</param>
    public void SetColliderParams(float radius, float height)
    {
        colliderRadius = radius;
        colliderHeight = height;
        Debug.Log($"[AnimationRelativePos] 設定碰撞器參數: 半徑={radius}, 高度={height}");
    }

    void Update()
    {
        // 視覺化調試
        if (enableDebugVisualization)
        {
            // 使用設定的碰撞器參數
            float radius = colliderRadius;
            float height = colliderHeight;
            
            // 顯示修正後的射線起點和路徑
            float pushOutDistance = radius + 0.1f;
            Vector3 rayStart = transform.position + Vector3.up * (height * 0.5f) + transform.forward * pushOutDistance;
            Vector3 rayEnd = rayStart + transform.forward * collisionCheckDistance;
            Debug.DrawLine(rayStart, rayEnd, rootMotionBlocked ? Color.red : Color.green);
            
            // 繪製從角色中心到射線起點的連線（顯示外推距離）
            Vector3 centerPoint = transform.position + Vector3.up * (height * 0.5f);
            Debug.DrawLine(centerPoint, rayStart, Color.yellow);
            
            // 繪製角色的膠囊體範圍
            Vector3 capsuleBottom = transform.position + Vector3.up * radius;
            Vector3 capsuleTop = transform.position + Vector3.up * (height - radius);
            Debug.DrawLine(capsuleBottom, capsuleTop, Color.blue);
            
            // 繪製角色的半徑範圍（圓形）
            for (int i = 0; i < 16; i++)
            {
                float angle1 = (i / 16.0f) * 2 * Mathf.PI;
                float angle2 = ((i + 1) / 16.0f) * 2 * Mathf.PI;
                Vector3 point1 = transform.position + new Vector3(Mathf.Cos(angle1), 0, Mathf.Sin(angle1)) * radius;
                Vector3 point2 = transform.position + new Vector3(Mathf.Cos(angle2), 0, Mathf.Sin(angle2)) * radius;
                Debug.DrawLine(point1, point2, Color.cyan);
            }
            
            // 如果有碰撞信息，繪製碰撞點的方塊
            if (hasCollisionDebugInfo)
            {
                DrawDebugCube(lastCollisionPoint, debugCubeSize, debugCollisionColor);
            }
        }
    }
    
    /// <summary>
    /// 繪製調試方塊
    /// </summary>
    /// <param name="center">方塊中心位置</param>
    /// <param name="size">方塊大小</param>
    /// <param name="color">方塊顏色</param>
    private void DrawDebugCube(Vector3 center, float size, Color color)
    {
        float halfSize = size * 0.5f;
        
        // 方塊的8個頂點
        Vector3[] vertices = new Vector3[8];
        vertices[0] = center + new Vector3(-halfSize, -halfSize, -halfSize); // 左下後
        vertices[1] = center + new Vector3(halfSize, -halfSize, -halfSize);  // 右下後
        vertices[2] = center + new Vector3(halfSize, -halfSize, halfSize);   // 右下前
        vertices[3] = center + new Vector3(-halfSize, -halfSize, halfSize);  // 左下前
        vertices[4] = center + new Vector3(-halfSize, halfSize, -halfSize);  // 左上後
        vertices[5] = center + new Vector3(halfSize, halfSize, -halfSize);   // 右上後
        vertices[6] = center + new Vector3(halfSize, halfSize, halfSize);    // 右上前
        vertices[7] = center + new Vector3(-halfSize, halfSize, halfSize);   // 左上前
        
        // 繪製方塊的12條邊
        // 底面
        Debug.DrawLine(vertices[0], vertices[1], color);
        Debug.DrawLine(vertices[1], vertices[2], color);
        Debug.DrawLine(vertices[2], vertices[3], color);
        Debug.DrawLine(vertices[3], vertices[0], color);
        
        // 頂面
        Debug.DrawLine(vertices[4], vertices[5], color);
        Debug.DrawLine(vertices[5], vertices[6], color);
        Debug.DrawLine(vertices[6], vertices[7], color);
        Debug.DrawLine(vertices[7], vertices[4], color);
        
        // 垂直邊
        Debug.DrawLine(vertices[0], vertices[4], color);
        Debug.DrawLine(vertices[1], vertices[5], color);
        Debug.DrawLine(vertices[2], vertices[6], color);
        Debug.DrawLine(vertices[3], vertices[7], color);
    }
}
