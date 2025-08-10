using UnityEngine;

/// <summary>
/// 扇形 Mesh 生成器 - 可在 Runtime 動態生成自訂角度與長度的扇形平面
/// </summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class SectorMeshGenerator : MonoBehaviour
{
    [Header("扇形參數")]
    [SerializeField, Range(1f, 360f)] private float angle = 60f; // 扇形角度
    [SerializeField, Range(0.1f, 50f)] private float radius = 5f; // 扇形半徑
    [SerializeField, Range(3, 100)] private int segments = 20; // 扇形細分段數（越多越圓滑）
    
    [Header("材質設定")]
    [SerializeField] private Material sectorMaterial; // 扇形材質
    [SerializeField] private Color sectorColor = new Color(1f, 1f, 0f, 0.3f); // 扇形顏色（預設半透明黃色）
    
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private Mesh sectorMesh;
    
    void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();
        
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
        
        // 計算頂點數量：中心點 + 扇形邊緣點
        int vertexCount = segments + 2;
        Vector3[] vertices = new Vector3[vertexCount];
        Vector2[] uvs = new Vector2[vertexCount];
        
        // 設定中心點
        vertices[0] = Vector3.zero;
        uvs[0] = new Vector2(0.5f, 0f);
        
        // 計算每段的角度
        float angleStep = angle / segments;
        float currentAngle = -angle / 2f; // 從負半角開始，使扇形對稱
        
        // 生成扇形邊緣頂點
        for (int i = 0; i <= segments; i++)
        {
            float rad = currentAngle * Mathf.Deg2Rad;
            float x = Mathf.Sin(rad) * radius;
            float z = Mathf.Cos(rad) * radius;
            
            vertices[i + 1] = new Vector3(x, 0, z);
            
            // UV 座標
            float u = (x / radius + 1f) * 0.5f;
            float v = (z / radius + 1f) * 0.5f;
            uvs[i + 1] = new Vector2(u, v);
            
            currentAngle += angleStep;
        }
        
        // 生成三角形索引
        int triangleCount = segments * 3;
        int[] triangles = new int[triangleCount];
        
        for (int i = 0; i < segments; i++)
        {
            int triangleIndex = i * 3;
            triangles[triangleIndex] = 0; // 中心點
            triangles[triangleIndex + 1] = i + 1;
            triangles[triangleIndex + 2] = i + 2;
        }
        
        // 設定 Mesh 數據
        sectorMesh.vertices = vertices;
        sectorMesh.triangles = triangles;
        sectorMesh.uv = uvs;
        sectorMesh.RecalculateNormals();
        sectorMesh.RecalculateBounds();
        
        // 應用 Mesh
        meshFilter.mesh = sectorMesh;
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
}