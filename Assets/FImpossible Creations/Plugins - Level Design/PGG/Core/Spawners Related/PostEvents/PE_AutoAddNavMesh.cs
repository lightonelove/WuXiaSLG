using UnityEngine;
using Unity.AI.Navigation;
using UnityEngine.AI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FIMSpace.Generating
{
    /// <summary>
    /// PGG 後處理事件：自動在容器物件上添加 NavMeshSurface 和 NavMeshRuntimeBaker 組件
    /// </summary>
    [CreateAssetMenu(fileName = "PE Auto Add NavMesh", menuName = "FImpossible Creations/Generators/Post Events/PE Auto Add NavMesh", order = 1)]
    public class PE_AutoAddNavMesh : FieldSpawnerPostEvent_Base
    {
        [Header("NavMesh 設定")]
        [Tooltip("是否自動添加 NavMeshSurface 組件")]
        public bool AddNavMeshSurface = true;
        
        [Tooltip("是否自動添加 NavMeshRuntimeBaker 組件")]
        public bool AddNavMeshRuntimeBaker = true;
        
        [Header("NavMeshSurface 參數")]
        [Tooltip("NavMesh Agent 類型 ID")]
        public int AgentTypeID = 0;
        
        [Tooltip("收集物件的類型")]
        public CollectObjects CollectObjects = CollectObjects.All;
        
        [Tooltip("使用的圖層遮罩")]
        public LayerMask LayerMask = -1;
        
        [Tooltip("使用幾何體的類型")]
        public NavMeshCollectGeometry UseGeometry = NavMeshCollectGeometry.RenderMeshes;

        [Header("NavMeshRuntimeBaker 參數")]
        [Tooltip("是否在啟動時自動烘焙")]
        public bool BakeOnStart = true;
        
        [Tooltip("烘焙延遲時間（秒）")]
        public float BakeDelay = 0.5f;

        /// <summary>
        /// 在所有生成完成後調用
        /// </summary>
        public override void OnAfterAllGeneratingCall(FieldSetup.CustomPostEventHelper helper, InstantiatedFieldInfo generatedRef)
        {
            base.OnAfterAllGeneratingCall(helper, generatedRef);
            
            // 嘗試獲取生成的容器物件 - 有多個可能的來源
            GameObject container = null;
            
            // 先嘗試 FieldTransform
            if (generatedRef.FieldTransform != null)
            {
                container = generatedRef.FieldTransform.gameObject;
                Debug.Log($"[PE_AutoAddNavMesh] 使用 FieldTransform: {container.name}");
            }
            // 再嘗試 MainContainer
            else if (generatedRef.MainContainer != null)
            {
                container = generatedRef.MainContainer;
                Debug.Log($"[PE_AutoAddNavMesh] 使用 MainContainer: {container.name}");
            }
            // 最後嘗試從實例化清單中找第一個父物件
            else if (generatedRef.Instantiated != null && generatedRef.Instantiated.Count > 0)
            {
                // 找到最頂層的父物件
                Transform topParent = generatedRef.Instantiated[0].transform;
                while (topParent.parent != null && generatedRef.Instantiated.Contains(topParent.parent.gameObject))
                {
                    topParent = topParent.parent;
                }
                container = topParent.gameObject;
                Debug.Log($"[PE_AutoAddNavMesh] 使用頂層實例物件: {container.name}");
            }
            
            if (container == null)
            {
                Debug.LogError("[PE_AutoAddNavMesh] 找不到任何容器物件！generatedRef 資訊：" +
                    $"\n- FieldTransform: {generatedRef.FieldTransform}" +
                    $"\n- MainContainer: {generatedRef.MainContainer}" +
                    $"\n- Instantiated Count: {(generatedRef.Instantiated != null ? generatedRef.Instantiated.Count : 0)}");
                return;
            }
            
            // 添加 NavMeshSurface 組件
            if (AddNavMeshSurface)
            {
                NavMeshSurface navMeshSurface = container.GetComponent<NavMeshSurface>();
                
                if (navMeshSurface == null)
                {
                    navMeshSurface = container.AddComponent<NavMeshSurface>();
                    Debug.Log($"[PE_AutoAddNavMesh] 已添加 NavMeshSurface 到 {container.name}");
                    
                    // 設定 NavMeshSurface 參數
                    navMeshSurface.agentTypeID = AgentTypeID;
                    navMeshSurface.collectObjects = CollectObjects;
                    navMeshSurface.layerMask = LayerMask;
                    navMeshSurface.useGeometry = UseGeometry;
                }
                else
                {
                    Debug.Log($"[PE_AutoAddNavMesh] {container.name} 已有 NavMeshSurface 組件，重新設定參數");
                    
                    // 即使組件存在，也要更新設定參數（以防使用者改變了設定）
                    navMeshSurface.agentTypeID = AgentTypeID;
                    navMeshSurface.collectObjects = CollectObjects;
                    navMeshSurface.layerMask = LayerMask;
                    navMeshSurface.useGeometry = UseGeometry;
                }
                
                // 無論組件是新增還是已存在，都要重新烘焙 NavMesh
                navMeshSurface.BuildNavMesh();
                Debug.Log($"[PE_AutoAddNavMesh] NavMesh 已重新烘焙完成");
            }
            
            // 添加 NavMeshRuntimeBaker 組件
            if (AddNavMeshRuntimeBaker)
            {
                NavMeshRuntimeBaker runtimeBaker = container.GetComponent<NavMeshRuntimeBaker>();
                
                if (runtimeBaker == null)
                {
                    runtimeBaker = container.AddComponent<NavMeshRuntimeBaker>();
                    Debug.Log($"[PE_AutoAddNavMesh] 已添加 NavMeshRuntimeBaker 到 {container.name}");
                    
                    // 設定 NavMeshRuntimeBaker 參數
                    runtimeBaker.bakeOnStart = BakeOnStart;
                    runtimeBaker.bakeDelay = BakeDelay;
                    
                    // 如果有 NavMeshSurface，將其連接到 RuntimeBaker
                    NavMeshSurface surface = container.GetComponent<NavMeshSurface>();
                    if (surface != null)
                    {
                        runtimeBaker.surface = surface;
                    }
                }
                else
                {
                    Debug.Log($"[PE_AutoAddNavMesh] {container.name} 已有 NavMeshRuntimeBaker 組件");
                }
            }
        }

#if UNITY_EDITOR
        /// <summary>
        /// 在編輯器中顯示自定義 GUI
        /// </summary>
        public override void Editor_DisplayGUI(FieldSetup.CustomPostEventHelper helper)
        {
            base.Editor_DisplayGUI(helper);
            
            EditorGUILayout.HelpBox(
                "此後處理事件會在 PGG 生成完成後，自動在容器物件上添加：\n" +
                "• NavMeshSurface - 用於生成 NavMesh\n" +
                "• NavMeshRuntimeBaker - 用於運行時動態烘焙",
                MessageType.Info
            );
            
            if (GUILayout.Button("測試尋找 NavMesh 組件"))
            {
                // 測試是否能找到 NavMesh 相關類型
                System.Type navMeshSurfaceType = typeof(NavMeshSurface);
                System.Type runtimeBakerType = typeof(NavMeshRuntimeBaker);
                
                EditorUtility.DisplayDialog(
                    "組件檢測結果",
                    $"NavMeshSurface: {(navMeshSurfaceType != null ? "找到" : "未找到")}\n" +
                    $"NavMeshRuntimeBaker: {(runtimeBakerType != null ? "找到" : "未找到")}",
                    "確定"
                );
            }
        }
#endif
    }
}