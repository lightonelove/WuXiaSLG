using UnityEngine;
using UnityEngine.InputSystem;

public class FloorMouseIndicator : MonoBehaviour
{
    [Header("指示器設定")]
    public GameObject indicatorPrefab; // 指示器prefab (FloorIndicator)
    public LayerMask floorLayerMask = -1; // Floor圖層遮罩
    public float indicatorOffset = 0.01f; // 指示器與地面的距離偏移
    public bool showIndicator = true; // 是否顯示指示器
    
    [Header("顏色設定")]
    public Color normalColor = new Color(0f, 1f, 0.2f, 0.8f); // 正常模式顏色（半透明綠色）
    public Color targetingColor = new Color(1f, 0.2f, 0f, 0.8f); // 技能目標模式顏色（半透明紅色）
    public Color noneColor = new Color(0.5f, 0.5f, 0.5f, 0.8f); // None模式顏色（半透明灰色）
    
    [Header("相機設定")]
    public Camera targetCamera; // 用於Raycast的相機
    
    private GameObject currentIndicator; // 當前的指示器物件實例
    private bool isMouseOverFloor = false;
    
    void Start()
    {
        // 如果沒有指定相機，使用主相機
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
            if (targetCamera == null)
            {
                targetCamera = FindObjectOfType<Camera>();
            }
        }
        
        // 創建指示器實例
        CreateIndicator();
    }
    
    void Update()
    {
        if (showIndicator && targetCamera != null)
        {
            UpdateIndicatorPosition();
        }
    }
    
    private void CreateIndicator()
    {
        if (indicatorPrefab != null)
        {
            // 使用提供的prefab創建指示器
            currentIndicator = Instantiate(indicatorPrefab);
            currentIndicator.name = "FloorMouseIndicator_Instance";
        }
        else
        {
            // 如果沒有提供prefab，創建一個簡單的預設指示器
            CreateDefaultIndicator();
        }
        
        // 初始隱藏指示器
        if (currentIndicator != null)
        {
            currentIndicator.SetActive(false);
        }
    }
    
    private void CreateDefaultIndicator()
    {
        // 創建一個簡單的圓形指示器
        currentIndicator = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        currentIndicator.name = "FloorMouseIndicator_Default";
        
        // 設定大小 - 扁平的圓盤形狀
        currentIndicator.transform.localScale = new Vector3(1f, 0.05f, 1f);
        
        // 移除碰撞器，避免干擾遊戲邏輯
        Collider indicatorCollider = currentIndicator.GetComponent<Collider>();
        if (indicatorCollider != null)
        {
            DestroyImmediate(indicatorCollider);
        }
        
        // 設定材質
        Renderer renderer = currentIndicator.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material indicatorMaterial = new Material(Shader.Find("Standard"));
            indicatorMaterial.color = normalColor; // 使用正常模式顏色
            
            // 設定透明渲染
            indicatorMaterial.SetFloat("_Mode", 3); // Transparent mode
            indicatorMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            indicatorMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            indicatorMaterial.SetInt("_ZWrite", 0);
            indicatorMaterial.DisableKeyword("_ALPHATEST_ON");
            indicatorMaterial.EnableKeyword("_ALPHABLEND_ON");
            indicatorMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            indicatorMaterial.renderQueue = 3000;
            
            renderer.material = indicatorMaterial;
        }
    }
    
    private void UpdateIndicatorPosition()
    {
        // 獲取鼠標位置
        Vector2 mousePosition = Mouse.current.position.ReadValue();
        
        // 從相機射出射線
        Ray ray = targetCamera.ScreenPointToRay(mousePosition);
        
        // 執行射線檢測
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, Mathf.Infinity, floorLayerMask))
        {
            // 命中Floor層，顯示指示器
            if (!isMouseOverFloor)
            {
                currentIndicator.SetActive(true);
                isMouseOverFloor = true;
            }
            
            // 更新指示器位置
            Vector3 indicatorPosition = hit.point + hit.normal * indicatorOffset;
            currentIndicator.transform.position = indicatorPosition;
            
            // 讓指示器朝向法線方向
            currentIndicator.transform.up = hit.normal;
        }
        else
        {
            // 沒有命中Floor層，隱藏指示器
            if (isMouseOverFloor)
            {
                currentIndicator.SetActive(false);
                isMouseOverFloor = false;
            }
        }
    }
    
    /// <summary>
    /// 設定指示器顯示狀態
    /// </summary>
    /// <param name="show">是否顯示指示器</param>
    public void SetIndicatorVisibility(bool show)
    {
        showIndicator = show;
        
        if (!show && currentIndicator != null)
        {
            currentIndicator.SetActive(false);
            isMouseOverFloor = false;
        }
    }
    
    /// <summary>
    /// 設定指示器的prefab
    /// </summary>
    /// <param name="newPrefab">新的指示器prefab</param>
    public void SetIndicatorPrefab(GameObject newPrefab)
    {
        // 銷毀舊的指示器
        if (currentIndicator != null)
        {
            DestroyImmediate(currentIndicator);
        }
        
        // 設定新的prefab並創建
        indicatorPrefab = newPrefab;
        CreateIndicator();
    }
    
    /// <summary>
    /// 獲取當前鼠標在Floor上的位置
    /// </summary>
    /// <returns>Floor位置，如果沒有命中則返回Vector3.zero</returns>
    public Vector3 GetMouseFloorPosition()
    {
        if (targetCamera == null) return Vector3.zero;
        
        Vector2 mousePosition = Mouse.current.position.ReadValue();
        Ray ray = targetCamera.ScreenPointToRay(mousePosition);
        
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, Mathf.Infinity, floorLayerMask))
        {
            return hit.point;
        }
        
        return Vector3.zero;
    }
    
    /// <summary>
    /// 檢查鼠標是否指向Floor
    /// </summary>
    /// <returns>如果指向Floor則返回true</returns>
    public bool IsMouseOverFloor()
    {
        return isMouseOverFloor;
    }
    
    /// <summary>
    /// 設定Floor圖層遮罩
    /// </summary>
    /// <param name="layerMask">圖層遮罩</param>
    public void SetFloorLayerMask(LayerMask layerMask)
    {
        floorLayerMask = layerMask;
    }
    
    /// <summary>
    /// 設定指示器顏色模式
    /// </summary>
    /// <param name="mode">顏色模式：0=灰色(None), 1=綠色(正常), 2=紅色(技能目標)</param>
    public void SetIndicatorColorMode(int mode)
    {
        if (currentIndicator != null)
        {
            Renderer renderer = currentIndicator.GetComponent<Renderer>();
            if (renderer != null && renderer.material != null)
            {
                Color newColor;
                switch (mode)
                {
                    case 0: // None模式 - 灰色
                        newColor = noneColor;
                        break;
                    case 1: // Move模式 - 綠色
                        newColor = normalColor;
                        break;
                    case 2: // 技能目標模式 - 紅色
                        newColor = targetingColor;
                        break;
                    default:
                        newColor = noneColor;
                        break;
                }
                renderer.material.color = newColor;
            }
        }
    }
    
    /// <summary>
    /// 設定指示器顏色模式（向後相容）
    /// </summary>
    /// <param name="useTargetingColor">是否使用技能目標模式顏色</param>
    public void SetIndicatorTargetingMode(bool useTargetingColor)
    {
        SetIndicatorColorMode(useTargetingColor ? 2 : 1);
    }
    
    void OnDestroy()
    {
        // 清理指示器物件
        if (currentIndicator != null)
        {
            DestroyImmediate(currentIndicator);
        }
    }
    
    void OnDrawGizmosSelected()
    {
        // 在Scene視圖中顯示射線調試資訊
        if (targetCamera != null && Application.isPlaying)
        {
            Vector2 mousePosition = Mouse.current.position.ReadValue();
            Ray ray = targetCamera.ScreenPointToRay(mousePosition);
            
            Gizmos.color = isMouseOverFloor ? Color.green : Color.red;
            Gizmos.DrawRay(ray.origin, ray.direction * 100f);
            
            if (isMouseOverFloor && currentIndicator != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(currentIndicator.transform.position, 0.5f);
            }
        }
    }
}