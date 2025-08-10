using UnityEngine;

/// <summary>
/// 扇形 Mesh 測試腳本 - 展示如何動態控制扇形參數
/// </summary>
public class SectorMeshTest : MonoBehaviour
{
    [Header("測試設定")]
    [SerializeField] private SectorMeshGenerator sectorGenerator;
    [SerializeField] private bool animateAngle = false;
    [SerializeField] private bool animateRadius = false;
    [SerializeField] private bool followMouse = false;
    
    [Header("動畫參數")]
    [SerializeField] private float angleSpeed = 30f;
    [SerializeField] private float radiusSpeed = 1f;
    [SerializeField] private float minAngle = 15f;
    [SerializeField] private float maxAngle = 120f;
    [SerializeField] private float minRadius = 2f;
    [SerializeField] private float maxRadius = 10f;
    
    private float currentAngle = 60f;
    private float currentRadius = 5f;
    private bool angleIncreasing = true;
    private bool radiusIncreasing = true;
    
    void Start()
    {
        // 如果沒有指定 SectorGenerator，嘗試從自身獲取
        if (sectorGenerator == null)
        {
            sectorGenerator = GetComponent<SectorMeshGenerator>();
        }
        
        // 如果還是沒有，動態創建一個
        if (sectorGenerator == null)
        {
            GameObject sectorObj = new GameObject("Sector Mesh");
            sectorObj.transform.SetParent(transform);
            sectorObj.transform.localPosition = Vector3.zero;
            sectorObj.transform.localRotation = Quaternion.identity;
            sectorGenerator = sectorObj.AddComponent<SectorMeshGenerator>();
        }
    }
    
    void Update()
    {
        if (sectorGenerator == null) return;
        
        // 動畫測試：角度
        if (animateAngle)
        {
            AnimateAngle();
        }
        
        // 動畫測試：半徑
        if (animateRadius)
        {
            AnimateRadius();
        }
        
        // 跟隨滑鼠測試
        if (followMouse)
        {
            FollowMouse();
        }
        
        // 鍵盤控制測試
        HandleKeyboardInput();
    }
    
    /// <summary>
    /// 角度動畫
    /// </summary>
    private void AnimateAngle()
    {
        if (angleIncreasing)
        {
            currentAngle += angleSpeed * Time.deltaTime;
            if (currentAngle >= maxAngle)
            {
                currentAngle = maxAngle;
                angleIncreasing = false;
            }
        }
        else
        {
            currentAngle -= angleSpeed * Time.deltaTime;
            if (currentAngle <= minAngle)
            {
                currentAngle = minAngle;
                angleIncreasing = true;
            }
        }
        
        sectorGenerator.SetAngle(currentAngle);
    }
    
    /// <summary>
    /// 半徑動畫
    /// </summary>
    private void AnimateRadius()
    {
        if (radiusIncreasing)
        {
            currentRadius += radiusSpeed * Time.deltaTime;
            if (currentRadius >= maxRadius)
            {
                currentRadius = maxRadius;
                radiusIncreasing = false;
            }
        }
        else
        {
            currentRadius -= radiusSpeed * Time.deltaTime;
            if (currentRadius <= minRadius)
            {
                currentRadius = minRadius;
                radiusIncreasing = true;
            }
        }
        
        sectorGenerator.SetRadius(currentRadius);
    }
    
    /// <summary>
    /// 跟隨滑鼠方向
    /// </summary>
    private void FollowMouse()
    {
        // 獲取滑鼠在世界空間的位置（假設在地面上）
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, LayerMask.GetMask("Floor")))
        {
            Vector3 direction = hit.point - transform.position;
            direction.y = 0;
            
            if (direction != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }
        }
    }
    
    /// <summary>
    /// 鍵盤輸入控制
    /// </summary>
    private void HandleKeyboardInput()
    {
        // Q/E 控制角度
        if (Input.GetKey(KeyCode.Q))
        {
            currentAngle = Mathf.Max(minAngle, currentAngle - angleSpeed * Time.deltaTime);
            sectorGenerator.SetAngle(currentAngle);
        }
        if (Input.GetKey(KeyCode.E))
        {
            currentAngle = Mathf.Min(maxAngle, currentAngle + angleSpeed * Time.deltaTime);
            sectorGenerator.SetAngle(currentAngle);
        }
        
        // Z/X 控制半徑
        if (Input.GetKey(KeyCode.Z))
        {
            currentRadius = Mathf.Max(minRadius, currentRadius - radiusSpeed * Time.deltaTime);
            sectorGenerator.SetRadius(currentRadius);
        }
        if (Input.GetKey(KeyCode.X))
        {
            currentRadius = Mathf.Min(maxRadius, currentRadius + radiusSpeed * Time.deltaTime);
            sectorGenerator.SetRadius(currentRadius);
        }
        
        // 空格鍵切換顯示/隱藏
        if (Input.GetKeyDown(KeyCode.Space))
        {
            sectorGenerator.SetVisible(!sectorGenerator.GetComponent<MeshRenderer>().enabled);
        }
        
        // 數字鍵切換顏色
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            sectorGenerator.SetColor(new Color(1f, 0f, 0f, 0.3f)); // 紅色
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            sectorGenerator.SetColor(new Color(0f, 1f, 0f, 0.3f)); // 綠色
        }
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            sectorGenerator.SetColor(new Color(0f, 0f, 1f, 0.3f)); // 藍色
        }
        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            sectorGenerator.SetColor(new Color(1f, 1f, 0f, 0.3f)); // 黃色
        }
    }
}