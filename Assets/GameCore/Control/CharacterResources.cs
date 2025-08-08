using UnityEngine;

/// <summary>
/// 角色資源管理系統 - 負責AP系統和UI更新
/// </summary>
public class CharacterResources : MonoBehaviour
{
    [Header("資源系統")]
    public float AP = 100;  // Action Points - 統一的資源系統
    public float MaxAP = 100;
    public Vector2 lastPosition;
    
    void Start()
    {
        // 初始化lastPosition為當前位置
        lastPosition = new Vector2(transform.position.x, transform.position.z);
    }
    
    /// <summary>
    /// 消耗AP
    /// </summary>
    /// <param name="amount">消耗數量</param>
    public void ConsumeAP(float amount)
    {
        AP -= amount;
        AP = Mathf.Max(0, AP); // 確保AP不會變負數
        Debug.Log("AP:" + AP);
        
        // 更新UI
        UpdateAPDisplay();
    }
    
    /// <summary>
    /// 恢復AP到最大值
    /// </summary>
    public void RefillAP()
    {
        AP = MaxAP;
        UpdateAPDisplay();
    }
    
    /// <summary>
    /// 檢查是否有足夠的AP
    /// </summary>
    /// <param name="amount">需要的AP數量</param>
    /// <returns>是否有足夠AP</returns>
    public bool HasEnoughAP(float amount)
    {
        return AP >= amount;
    }
    
    /// <summary>
    /// 更新AP顯示UI
    /// </summary>
    public void UpdateAPDisplay()
    {
        if (SLGCoreUI.Instance != null && SLGCoreUI.Instance.apBar != null)
        {
            SLGCoreUI.Instance.apBar.slider.maxValue = MaxAP;
            SLGCoreUI.Instance.apBar.slider.value = AP;
        }
    }
    
    /// <summary>
    /// 顯示預覽AP（用於路徑預覽）
    /// </summary>
    /// <param name="apCost">預覽消耗的AP</param>
    public void ShowPreviewAP(float apCost)
    {
        if (SLGCoreUI.Instance != null && SLGCoreUI.Instance.apBar != null)
        {
            float proxyAP = Mathf.Max(0, AP - apCost);
            SLGCoreUI.Instance.apBar.slider.maxValue = MaxAP;
            SLGCoreUI.Instance.apBar.slider.value = proxyAP;
        }
    }
}