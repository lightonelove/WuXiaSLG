using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 目標指示器 - 當物件被技能瞄準時提供視覺回饋
/// 會將 MeshRenderer 的材質顏色變成白色來指示被瞄準狀態
/// </summary>
public class TargetedIndicator : MonoBehaviour
{
    [Header("指示器設定")]
    [SerializeField] private Color targetedColor = Color.white; // 被瞄準時的顏色
    [SerializeField] private bool affectChildMeshRenderers = true; // 是否影響子物件的 MeshRenderer
    
    [Header("Debug 設定")]
    [SerializeField] private bool enableDebugLog = false; // 是否啟用 Debug 輸出
    
    // 原始材質和顏色的記錄
    private struct MaterialColorInfo
    {
        public MeshRenderer renderer;
        public Material[] originalMaterials;
        public Color[] originalColors;
        public Material[] modifiedMaterials;
    }
    
    private List<MaterialColorInfo> materialInfos = new List<MaterialColorInfo>();
    private bool isTargeted = false;
    private int targeterCount = 0; // 記錄有多少個技能瞄準者
    
    void Awake()
    {
        // 初始化時記錄所有 MeshRenderer 的原始材質和顏色
        InitializeMaterialInfo();
    }
    
    /// <summary>
    /// 初始化材質資訊
    /// </summary>
    private void InitializeMaterialInfo()
    {
        materialInfos.Clear();
        
        // 獲取本身的 MeshRenderer
        MeshRenderer selfRenderer = GetComponent<MeshRenderer>();
        if (selfRenderer != null)
        {
            AddMeshRendererInfo(selfRenderer);
        }
        
        // 如果需要，獲取所有子物件的 MeshRenderer
        if (affectChildMeshRenderers)
        {
            MeshRenderer[] childRenderers = GetComponentsInChildren<MeshRenderer>();
            foreach (MeshRenderer renderer in childRenderers)
            {
                if (renderer != selfRenderer) // 避免重複添加
                {
                    AddMeshRendererInfo(renderer);
                }
            }
        }
        
        if (enableDebugLog)
        {
            Debug.Log($"[TargetedIndicator] Initialized {materialInfos.Count} MeshRenderer(s) on {gameObject.name}");
        }
    }
    
    /// <summary>
    /// 添加 MeshRenderer 資訊到記錄中
    /// </summary>
    /// <param name="renderer">要添加的 MeshRenderer</param>
    private void AddMeshRendererInfo(MeshRenderer renderer)
    {
        if (renderer == null || renderer.materials == null || renderer.materials.Length == 0)
            return;
            
        MaterialColorInfo info = new MaterialColorInfo();
        info.renderer = renderer;
        info.originalMaterials = renderer.materials;
        info.originalColors = new Color[info.originalMaterials.Length];
        info.modifiedMaterials = new Material[info.originalMaterials.Length];
        
        // 記錄原始顏色並創建修改用的材質副本
        for (int i = 0; i < info.originalMaterials.Length; i++)
        {
            Material originalMat = info.originalMaterials[i];
            if (originalMat != null)
            {
                info.originalColors[i] = originalMat.color;
                
                // 創建材質副本供修改使用
                info.modifiedMaterials[i] = new Material(originalMat);
            }
        }
        
        materialInfos.Add(info);
    }
    
    /// <summary>
    /// 當被技能瞄準時調用
    /// </summary>
    public void OnTargeted()
    {
        targeterCount++;
        
        if (!isTargeted)
        {
            isTargeted = true;
            ApplyTargetedEffect();
            
            if (enableDebugLog)
            {
                Debug.Log($"[TargetedIndicator] {gameObject.name} is now TARGETED (Count: {targeterCount})");
            }
        }
        else if (enableDebugLog)
        {
            Debug.Log($"[TargetedIndicator] {gameObject.name} targeted count increased to {targeterCount}");
        }
    }
    
    /// <summary>
    /// 當不再被技能瞄準時調用
    /// </summary>
    public void OnUntargeted()
    {
        targeterCount = Mathf.Max(0, targeterCount - 1);
        
        if (targeterCount == 0 && isTargeted)
        {
            isTargeted = false;
            RemoveTargetedEffect();
            
            if (enableDebugLog)
            {
                Debug.Log($"[TargetedIndicator] {gameObject.name} is no longer targeted");
            }
        }
        else if (enableDebugLog && targeterCount > 0)
        {
            Debug.Log($"[TargetedIndicator] {gameObject.name} targeted count decreased to {targeterCount}");
        }
    }
    
    /// <summary>
    /// 強制清除所有瞄準狀態
    /// </summary>
    public void ClearAllTargeting()
    {
        targeterCount = 0;
        if (isTargeted)
        {
            isTargeted = false;
            RemoveTargetedEffect();
            
            if (enableDebugLog)
            {
                Debug.Log($"[TargetedIndicator] {gameObject.name} all targeting cleared");
            }
        }
    }
    
    /// <summary>
    /// 應用被瞄準的視覺效果
    /// </summary>
    private void ApplyTargetedEffect()
    {
        foreach (MaterialColorInfo info in materialInfos)
        {
            if (info.renderer == null) continue;
            
            // 將修改用的材質設定為目標顏色
            for (int i = 0; i < info.modifiedMaterials.Length; i++)
            {
                if (info.modifiedMaterials[i] != null)
                {
                    info.modifiedMaterials[i].color = targetedColor;
                }
            }
            
            // 應用修改後的材質
            info.renderer.materials = info.modifiedMaterials;
        }
    }
    
    /// <summary>
    /// 移除被瞄準的視覺效果
    /// </summary>
    private void RemoveTargetedEffect()
    {
        foreach (MaterialColorInfo info in materialInfos)
        {
            if (info.renderer == null) continue;
            
            // 恢復原始材質
            info.renderer.materials = info.originalMaterials;
        }
    }
    
    /// <summary>
    /// 獲取當前是否被瞄準
    /// </summary>
    /// <returns>是否被瞄準</returns>
    public bool IsTargeted()
    {
        return isTargeted;
    }
    
    /// <summary>
    /// 獲取當前瞄準者數量
    /// </summary>
    /// <returns>瞄準者數量</returns>
    public int GetTargeterCount()
    {
        return targeterCount;
    }
    
    /// <summary>
    /// 設定目標顏色
    /// </summary>
    /// <param name="color">新的目標顏色</param>
    public void SetTargetedColor(Color color)
    {
        targetedColor = color;
        
        // 如果當前正在被瞄準，立即更新顏色
        if (isTargeted)
        {
            ApplyTargetedEffect();
        }
    }
    
    /// <summary>
    /// 重新初始化材質資訊（當 MeshRenderer 發生變化時）
    /// </summary>
    public void RefreshMaterialInfo()
    {
        bool wasTargeted = isTargeted;
        
        // 先移除效果
        if (isTargeted)
        {
            RemoveTargetedEffect();
        }
        
        // 重新初始化
        InitializeMaterialInfo();
        
        // 如果之前被瞄準，重新應用效果
        if (wasTargeted)
        {
            ApplyTargetedEffect();
        }
    }
    
    void OnDestroy()
    {
        // 清理創建的材質副本
        foreach (MaterialColorInfo info in materialInfos)
        {
            if (info.modifiedMaterials != null)
            {
                for (int i = 0; i < info.modifiedMaterials.Length; i++)
                {
                    if (info.modifiedMaterials[i] != null)
                    {
                        DestroyImmediate(info.modifiedMaterials[i]);
                    }
                }
            }
        }
        materialInfos.Clear();
    }
    
    /// <summary>
    /// 在編輯器中實時預覽
    /// </summary>
    void OnValidate()
    {
        if (Application.isPlaying)
        {
            // 如果目標顏色改變且當前被瞄準，立即更新
            if (isTargeted)
            {
                ApplyTargetedEffect();
            }
        }
    }
}