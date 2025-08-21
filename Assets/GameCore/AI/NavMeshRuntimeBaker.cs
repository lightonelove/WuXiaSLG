using Sirenix.OdinInspector;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class NavMeshRuntimeBaker : MonoBehaviour
{
    [Header("NavMesh Surface 參考")]
    public NavMeshSurface surface;
    
    [Header("自動烘焙設定")]
    [Tooltip("是否在啟動時自動烘焙")]
    public bool bakeOnStart = true;
    
    [Tooltip("烘焙延遲時間（秒）")]
    public float bakeDelay = 0.5f;
    
    private void Start()
    {
        // 如果沒有指定 surface，嘗試從當前物件獲取
        if (surface == null)
        {
            surface = GetComponent<NavMeshSurface>();
        }
        
        // 如果還是沒有，嘗試從子物件獲取
        if (surface == null)
        {
            surface = GetComponentInChildren<NavMeshSurface>();
        }
        
        // 如果設定為啟動時烘焙
        if (bakeOnStart && surface != null)
        {
            if (bakeDelay > 0)
            {
                StartCoroutine(BakeWithDelay());
            }
            else
            {
                Bake();
            }
        }
    }
    
    /// <summary>
    /// 強制重新烘焙 NavMesh（清除舊資料後重新烘焙）
    /// </summary>
    [Button("強制重新烘焙")]
    public void ForceBake()
    {
        if (surface == null)
        {
            surface = GetComponent<NavMeshSurface>();
        }
        
        if (surface == null)
        {
            Debug.LogError("[NavMeshRuntimeBaker] 找不到 NavMeshSurface 組件！");
            return;
        }
        
        // 先清除現有的 NavMesh 資料
        surface.RemoveData();
        
        // 等待一幀後重新烘焙
        StartCoroutine(ForceBakeCoroutine());
    }
    
    private System.Collections.IEnumerator ForceBakeCoroutine()
    {
        yield return null; // 等待一幀
        
        if (surface != null)
        {
            surface.BuildNavMesh();
            Debug.Log($"[NavMeshRuntimeBaker] NavMesh 已強制重新烘焙完成 - {gameObject.name}");
        }
    }
    
    private IEnumerator BakeWithDelay()
    {
        yield return new WaitForSeconds(bakeDelay);
        Bake();
    }
    
    [Button("烘焙 NavMesh")]
    public void Bake()
    {
        if (surface == null)
        {
            Debug.LogError("[NavMeshRuntimeBaker] 找不到 NavMeshSurface 組件！");
            return;
        }
        
        surface.BuildNavMesh(); // 這會在 Runtime 烘焙 NavMesh
        Debug.Log($"[NavMeshRuntimeBaker] NavMesh 已烘焙完成 - {gameObject.name}");
    }
    
    [Button("清除 NavMesh")]
    public void ClearNavMesh()
    {
        if (surface == null)
        {
            Debug.LogError("[NavMeshRuntimeBaker] 找不到 NavMeshSurface 組件！");
            return;
        }
        
        surface.RemoveData();
        Debug.Log($"[NavMeshRuntimeBaker] NavMesh 已清除 - {gameObject.name}");
    }
}