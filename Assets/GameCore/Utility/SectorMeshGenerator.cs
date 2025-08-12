using UnityEngine;

/// <summary>
/// 扇形 Mesh 生成器 - 可在 Runtime 動態生成自訂角度與長度的扇形平面與Collider
/// </summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class SectorMeshGenerator : MonoBehaviour
{
    [Header("扇形參數")]
    [SerializeField, Range(1f, 360f)] private float angle = 60f; // 扇形角度
    [SerializeField, Range(0.1f, 50f)] private float radius = 5f; // 扇形半徑
    [SerializeField, Range(3, 100)] private int segments = 20; // 扇形細分段數（越多越圓滑）
    [SerializeField] private float colliderHeight = 1f; // Collider的高度
    
    [Header("材質設定")]
    [SerializeField] private Material sectorMaterial; // 扇形材質
    [SerializeField] private Color sectorColor = new Color(1f, 1f, 0f, 0.3f); // 扇形顏色（預設半透明黃色）
    
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private MeshCollider meshCollider;
    private Mesh sectorMesh;
    private Mesh colliderMesh;
    
    [Header("碰撞檢測")]
    [SerializeField] public LayerMask targetLayerMask = -1; // 要檢測碰撞的圖層遮罩
    private System.Collections.Generic.HashSet<Collider> collidingObjects = new System.Collections.Generic.HashSet<Collider>();
    
    void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();
        meshCollider = GetComponent<MeshCollider>();
        
        // 設定 MeshCollider 為 Trigger
        if (meshCollider != null)
        {
            meshCollider.convex = true;
            meshCollider.isTrigger = true;
        }
        
        // 如果沒有指定材質，創建一個預設的半透明材質
        if (sectorMaterial == null)
        {
            CreateDefaultMaterial();
        }
        else
        {
            meshRenderer.material = sectorMaterial;
        }
        
        // 生成扇形
        GenerateSector();
    }
    
    /// <summary>
    /// 創建預設的半透明材質
    /// </summary>
    private void CreateDefaultMaterial()
    {
        // 使用 Unity 的標準 Shader 創建半透明材質
        sectorMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        sectorMaterial.SetFloat("_Surface", 1); // 設為透明
        sectorMaterial.SetFloat("_Blend", 0); // Alpha 混合
        sectorMaterial.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        sectorMaterial.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        sectorMaterial.SetFloat("_ZWrite", 0);
        sectorMaterial.EnableKeyword("_ALPHABLEND_ON");
        sectorMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        sectorMaterial.renderQueue = 3000;
        sectorMaterial.color = sectorColor;
        meshRenderer.material = sectorMaterial;
    }
    
    /// <summary>
    /// 生成扇形 Mesh
    /// </summary>
    public void GenerateSector()
    {
        GenerateSector(angle, radius, segments);
    }
    
    /// <summary>
    /// 生成扇形 Mesh（可指定參數）
    /// </summary>
    /// <param name="newAngle">扇形角度</param>
    /// <param name="newRadius">扇形半徑</param>
    /// <param name="newSegments">細分段數</param>
    public void GenerateSector(float newAngle, float newRadius, int newSegments)
    {
        angle = Mathf.Clamp(newAngle, 1f, 360f);
        radius = Mathf.Clamp(newRadius, 0.1f, 50f);
        segments = Mathf.Clamp(newSegments, 3, 100);
        
        // 對於360度的情況，使用更多的段數以形成完整的圓
        if (angle >= 359.9f)
        {
            segments = Mathf.Max(segments, 20);
        }
        
        // 創建或清空 Mesh
        if (sectorMesh == null)
        {
            sectorMesh = new Mesh();
            sectorMesh.name = "Sector Mesh";
        }
        else
        {
            sectorMesh.Clear();
        }
        
        System.Collections.Generic.List<Vector3> vertices = new System.Collections.Generic.List<Vector3>();
        System.Collections.Generic.List<Vector2> uvs = new System.Collections.Generic.List<Vector2>();
        System.Collections.Generic.List<int> triangles = new System.Collections.Generic.List<int>();
        
        // 設定中心點
        vertices.Add(Vector3.zero);
        uvs.Add(new Vector2(0.5f, 0.5f));
        
        // 計算每段的角度
        float angleStep = angle / segments;
        float currentAngle = -angle / 2f; // 從負半角開始，使扇形對稱
        
        // 生成扇形邊緣頂點
        for (int i = 0; i <= segments; i++)
        {
            float rad = currentAngle * Mathf.Deg2Rad;
            float x = Mathf.Sin(rad) * radius;
            float z = Mathf.Cos(rad) * radius;
            
            vertices.Add(new Vector3(x, 0, z));
            
            // UV 座標
            float u = (x / radius + 1f) * 0.5f;
            float v = (z / radius + 1f) * 0.5f;
            uvs.Add(new Vector2(u, v));
            
            currentAngle += angleStep;
        }
        
        // 生成三角形索引 - 所有三角形都從中心點(0)輻射
        for (int i = 0; i < segments; i++)
        {
            triangles.Add(0); // 中心點
            triangles.Add(i + 1); // 當前邊緣點
            triangles.Add(i + 2); // 下一個邊緣點
        }
        
        // 設定 Mesh 數據
        sectorMesh.vertices = vertices.ToArray();
        sectorMesh.triangles = triangles.ToArray();
        sectorMesh.uv = uvs.ToArray();
        sectorMesh.RecalculateNormals();
        sectorMesh.RecalculateBounds();
        
        // 應用 Mesh
        meshFilter.mesh = sectorMesh;
        
        // 生成並應用 Collider Mesh
        GenerateColliderMesh();
    }
    
    /// <summary>
    /// 生成3D扇形 Collider Mesh（有高度的立體扇形）
    /// </summary>
    private void GenerateColliderMesh()
    {
        // 創建或清空 Collider Mesh
        if (colliderMesh == null)
        {
            colliderMesh = new Mesh();
            colliderMesh.name = "Sector Collider Mesh";
        }
        else
        {
            colliderMesh.Clear();
        }
        
        System.Collections.Generic.List<Vector3> verticesList = new System.Collections.Generic.List<Vector3>();
        System.Collections.Generic.List<int> trianglesList = new System.Collections.Generic.List<int>();
        
        // 新算法：以中心點為頂點，逐段構建扇形
        // 這樣可以確保任何角度的扇形都能正確生成
        
        float angleStep = angle / segments;
        float currentAngle = -angle / 2f;
        
        // 如果是360度，特殊處理為圓形
        bool isFullCircle = angle >= 359.9f;
        
        if (isFullCircle)
        {
            // 360度圓形的處理
            // 底部中心點
            verticesList.Add(Vector3.zero);
            
            // 底部圓周頂點
            for (int i = 0; i <= segments; i++)
            {
                float rad = (i * 360f / segments) * Mathf.Deg2Rad;
                float x = Mathf.Sin(rad) * radius;
                float z = Mathf.Cos(rad) * radius;
                verticesList.Add(new Vector3(x, 0, z));
            }
            
            int bottomCount = verticesList.Count;
            
            // 頂部頂點（複製底部但改變高度）
            for (int i = 0; i < bottomCount; i++)
            {
                Vector3 v = verticesList[i];
                verticesList.Add(new Vector3(v.x, colliderHeight, v.z));
            }
            
            // 底部三角形
            for (int i = 0; i < segments; i++)
            {
                trianglesList.Add(0);
                trianglesList.Add((i + 1) % segments + 1);
                trianglesList.Add(i + 1);
            }
            
            // 頂部三角形
            for (int i = 0; i < segments; i++)
            {
                trianglesList.Add(bottomCount);
                trianglesList.Add(bottomCount + i + 1);
                trianglesList.Add(bottomCount + (i + 1) % segments + 1);
            }
            
            // 側面
            for (int i = 0; i < segments; i++)
            {
                int b1 = i + 1;
                int b2 = (i + 1) % segments + 1;
                int t1 = b1 + bottomCount;
                int t2 = b2 + bottomCount;
                
                trianglesList.Add(b1);
                trianglesList.Add(t1);
                trianglesList.Add(t2);
                
                trianglesList.Add(b1);
                trianglesList.Add(t2);
                trianglesList.Add(b2);
            }
        }
        else
        {
            // 非360度扇形的處理
            // 底部頂點
            verticesList.Add(Vector3.zero); // 0: 底部中心
            
            // 底部弧形邊緣頂點
            for (int i = 0; i <= segments; i++)
            {
                float rad = currentAngle * Mathf.Deg2Rad;
                float x = Mathf.Sin(rad) * radius;
                float z = Mathf.Cos(rad) * radius;
                verticesList.Add(new Vector3(x, 0, z));
                currentAngle += angleStep;
            }
            
            int bottomVertexCount = verticesList.Count;
            
            // 頂部頂點（複製底部但改變高度）
            for (int i = 0; i < bottomVertexCount; i++)
            {
                Vector3 v = verticesList[i];
                verticesList.Add(new Vector3(v.x, colliderHeight, v.z));
            }
            
            // 生成底部扇形三角形（從中心點向外輻射）
            for (int i = 0; i < segments; i++)
            {
                trianglesList.Add(0); // 中心點
                trianglesList.Add(i + 1); // 當前弧上的點
                trianglesList.Add(i + 2); // 下一個弧上的點
            }
            
            // 生成頂部扇形三角形
            int topCenterIndex = bottomVertexCount;
            for (int i = 0; i < segments; i++)
            {
                trianglesList.Add(topCenterIndex); // 頂部中心點
                trianglesList.Add(topCenterIndex + i + 1); // 當前弧上的點
                trianglesList.Add(topCenterIndex + i + 2); // 下一個弧上的點
            }
            
            // 生成弧形外圍側面（連接上下弧形邊緣）
            for (int i = 1; i <= segments; i++)
            {
                int bottomLeft = i;
                int bottomRight = i + 1;
                int topLeft = bottomLeft + bottomVertexCount;
                int topRight = bottomRight + bottomVertexCount;
                
                // 第一個三角形
                trianglesList.Add(bottomLeft);
                trianglesList.Add(topLeft);
                trianglesList.Add(topRight);
                
                // 第二個三角形
                trianglesList.Add(bottomLeft);
                trianglesList.Add(topRight);
                trianglesList.Add(bottomRight);
            }
            
            // 生成兩個徑向側面（從中心到邊緣）
            // 左側面（起始邊）
            int leftEdgeBottom = 1;
            int leftEdgeTop = leftEdgeBottom + bottomVertexCount;
            
            trianglesList.Add(0); // 底部中心
            trianglesList.Add(topCenterIndex); // 頂部中心
            trianglesList.Add(leftEdgeBottom); // 底部邊緣
            
            trianglesList.Add(topCenterIndex); // 頂部中心
            trianglesList.Add(leftEdgeTop); // 頂部邊緣
            trianglesList.Add(leftEdgeBottom); // 底部邊緣
            
            // 右側面（結束邊）
            int rightEdgeBottom = segments + 1;
            int rightEdgeTop = rightEdgeBottom + bottomVertexCount;
            
            trianglesList.Add(0); // 底部中心
            trianglesList.Add(rightEdgeBottom); // 底部邊緣
            trianglesList.Add(topCenterIndex); // 頂部中心
            
            trianglesList.Add(topCenterIndex); // 頂部中心
            trianglesList.Add(rightEdgeBottom); // 底部邊緣
            trianglesList.Add(rightEdgeTop); // 頂部邊緣
        }
        
        // 設定 Collider Mesh 數據
        colliderMesh.vertices = verticesList.ToArray();
        colliderMesh.triangles = trianglesList.ToArray();
        colliderMesh.RecalculateNormals();
        colliderMesh.RecalculateBounds();
        
        // 應用到 MeshCollider
        if (meshCollider != null)
        {
            meshCollider.sharedMesh = colliderMesh;
        }
    }
    
    /// <summary>
    /// 更新扇形角度
    /// </summary>
    /// <param name="newAngle">新的角度值</param>
    public void SetAngle(float newAngle)
    {
        angle = Mathf.Clamp(newAngle, 1f, 360f);
        GenerateSector();
    }
    
    /// <summary>
    /// 更新扇形半徑
    /// </summary>
    /// <param name="newRadius">新的半徑值</param>
    public void SetRadius(float newRadius)
    {
        radius = Mathf.Clamp(newRadius, 0.1f, 50f);
        GenerateSector();
    }
    
    /// <summary>
    /// 更新扇形顏色
    /// </summary>
    /// <param name="newColor">新的顏色</param>
    public void SetColor(Color newColor)
    {
        sectorColor = newColor;
        if (meshRenderer != null && meshRenderer.material != null)
        {
            meshRenderer.material.color = sectorColor;
        }
    }
    
    /// <summary>
    /// 顯示或隱藏扇形
    /// </summary>
    /// <param name="visible">是否顯示</param>
    public void SetVisible(bool visible)
    {
        if (meshRenderer != null)
        {
            meshRenderer.enabled = visible;
        }
        
        // 同時控制 Collider 的啟用狀態
        if (meshCollider != null)
        {
            meshCollider.enabled = visible;
        }
    }
    
    /// <summary>
    /// 在編輯器中實時預覽
    /// </summary>
    void OnValidate()
    {
        if (meshFilter != null && Application.isPlaying)
        {
            GenerateSector();
        }
    }
    
    /// <summary>
    /// 繪製 Gizmos 以便在場景視圖中預覽
    /// </summary>
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
        
        // 繪製扇形輪廓
        float currentAngle = -angle / 2f;
        float angleStep = angle / 20;
        Vector3 previousPoint = transform.position + Quaternion.Euler(0, currentAngle, 0) * Vector3.forward * radius;
        
        for (int i = 1; i <= 20; i++)
        {
            currentAngle += angleStep;
            Vector3 nextPoint = transform.position + Quaternion.Euler(0, currentAngle, 0) * Vector3.forward * radius;
            Gizmos.DrawLine(previousPoint, nextPoint);
            previousPoint = nextPoint;
        }
        
        // 繪製扇形邊界
        Vector3 leftEdge = transform.position + Quaternion.Euler(0, -angle / 2f, 0) * Vector3.forward * radius;
        Vector3 rightEdge = transform.position + Quaternion.Euler(0, angle / 2f, 0) * Vector3.forward * radius;
        Gizmos.DrawLine(transform.position, leftEdge);
        Gizmos.DrawLine(transform.position, rightEdge);
    }
    
    /// <summary>
    /// 當有物件進入 Trigger 時
    /// </summary>
    void OnTriggerEnter(Collider other)
    {
        // 使用 LayerMask 檢測是否為目標圖層
        if (((1 << other.gameObject.layer) & targetLayerMask) != 0)
        {
            collidingObjects.Add(other);
            
            // 尋找有 CombatEntity 組件的父物件
            CombatEntity combatEntity = other.GetComponentInParent<CombatEntity>();
            if (combatEntity != null)
            {
                Debug.Log($"[SectorMesh] CombatEntity detected: {combatEntity.gameObject.name}");
            }
            else
            {
                Debug.Log($"[SectorMesh] Object entered (no CombatEntity): {other.name}");
            }
        }
    }
    
    /// <summary>
    /// 當有物件停留在 Trigger 時
    /// </summary>
    void OnTriggerStay(Collider other)
    {
        // 使用 LayerMask 檢測是否為目標圖層
        if (((1 << other.gameObject.layer) & targetLayerMask) != 0)
        {
            if (!collidingObjects.Contains(other))
            {
                collidingObjects.Add(other);
                
                // 尋找有 CombatEntity 組件的父物件
                CombatEntity combatEntity = other.GetComponentInParent<CombatEntity>();
                if (combatEntity != null)
                {
                    Debug.Log($"[SectorMesh] CombatEntity staying: {combatEntity.gameObject.name}");
                }
            }
        }
    }
    
    /// <summary>
    /// 當有物件離開 Trigger 時
    /// </summary>
    void OnTriggerExit(Collider other)
    {
        // 使用 LayerMask 檢測是否為目標圖層
        if (((1 << other.gameObject.layer) & targetLayerMask) != 0)
        {
            collidingObjects.Remove(other);
            
            // 尋找有 CombatEntity 組件的父物件
            CombatEntity combatEntity = other.GetComponentInParent<CombatEntity>();
            if (combatEntity != null)
            {
                Debug.Log($"[SectorMesh] CombatEntity exited: {combatEntity.gameObject.name}");
            }
            else
            {
                Debug.Log($"[SectorMesh] Object exited (no CombatEntity): {other.name}");
            }
        }
    }
    
    /// <summary>
    /// 獲取當前在扇形範圍內的所有碰撞物件
    /// </summary>
    /// <returns>碰撞物件集合</returns>
    public System.Collections.Generic.HashSet<Collider> GetCollidingObjects()
    {
        // 清理已被銷毀的物件
        collidingObjects.RemoveWhere(c => c == null);
        return new System.Collections.Generic.HashSet<Collider>(collidingObjects);
    }
    
    /// <summary>
    /// 清空碰撞物件列表
    /// </summary>
    public void ClearCollidingObjects()
    {
        collidingObjects.Clear();
    }
}