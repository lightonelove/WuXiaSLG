using UnityEngine;
using FIMSpace.Generating;
using Unity.AI.Navigation;
using UnityEngine.AI;
using Sirenix.OdinInspector;
using System.Collections;

namespace GameCore.Map
{
    /// <summary>
    /// Runtime 地圖生成器 - 可在運行時動態生成地圖和 NavMesh
    /// </summary>
    public class RuntimeMapGenerator : MonoBehaviour
    {
        [Title("地圖生成設定")]
        [Tooltip("要使用的 Field Setup 資產")]
        [Required("必須指定一個 Field Setup")]
        public FieldSetup fieldSetup;
        
        [Tooltip("地圖大小（格數）")]
        public Vector3Int mapSize = new Vector3Int(10, 0, 10);
        
        [Tooltip("是否將地圖中心點設在原點")]
        public bool centerOrigin = true;
        
        [Title("隨機設定")]
        [Tooltip("每次生成時使用隨機種子")]
        public bool useRandomSeed = true;
        
        [Tooltip("固定種子（當 useRandomSeed 為 false 時使用）")]
        [DisableIf("useRandomSeed")]
        public int fixedSeed = 12345;
        
        [Title("NavMesh 設定")]
        [Tooltip("生成地圖後自動烘焙 NavMesh")]
        public bool autoBakeNavMesh = true;
        
        [Tooltip("烘焙延遲時間（秒）")]
        [ShowIf("autoBakeNavMesh")]
        public float bakeDelay = 0.5f;

        [Title("Debug 資訊")]
        [ReadOnly]
        [ShowInInspector]
        public bool IsMapGenerated => generatedInfo != null && generatedInfo.Instantiated.Count > 0;
        
        [ReadOnly]
        [ShowInInspector]
        public int GeneratedObjectsCount => generatedInfo?.Instantiated?.Count ?? 0;
        
        [ReadOnly]
        [ShowInInspector]
        public string LastUsedSeed => lastUsedSeed.ToString();

        // 私有變數
        private InstantiatedFieldInfo generatedInfo;
        private int lastUsedSeed;
        private NavMeshSurface navMeshSurface;
        private NavMeshRuntimeBaker navMeshBaker;

        #region 公開方法

        /// <summary>
        /// 生成地圖
        /// </summary>
        [Button("生成地圖", ButtonSizes.Large)]
        [GUIColor(0.4f, 0.8f, 1f)]
        public void GenerateMap()
        {
            if (fieldSetup == null)
            {
                Debug.LogError("[RuntimeMapGenerator] Field Setup 未設定！");
                return;
            }

            // 清除現有地圖
            ClearMap();

            // 設定種子
            int seed = useRandomSeed ? FGenerators.GetRandom(-99999, 99999) : fixedSeed;
            lastUsedSeed = seed;

            Debug.Log($"[RuntimeMapGenerator] 開始生成地圖 - 種子: {seed}, 大小: {mapSize}");

            // 計算原點
            Vector3Int origin = Vector3Int.zero;
            if (centerOrigin)
            {
                origin = new Vector3Int(-mapSize.x / 2, 0, -mapSize.z / 2);
            }

            // 使用 PGG 生成地圖
            generatedInfo = IGeneration.GenerateFieldObjectsRectangleGrid(
                fieldSetup, 
                mapSize, 
                seed, 
                transform, 
                true, // useContainer
                null, // guides
                true, // generateObjects
                origin
            );

            Debug.Log($"[RuntimeMapGenerator] 地圖生成完成 - 生成了 {GeneratedObjectsCount} 個物件");

            // 如果設定為自動烘焙 NavMesh
            if (autoBakeNavMesh)
            {
                StartCoroutine(BakeNavMeshWithDelay());
            }
        }

        /// <summary>
        /// 清除地圖
        /// </summary>
        [Button("清除地圖", ButtonSizes.Large)]
        [GUIColor(1f, 0.6f, 0.4f)]
        public void ClearMap()
        {
            if (generatedInfo != null && generatedInfo.Instantiated != null)
            {
                Debug.Log($"[RuntimeMapGenerator] 清除 {generatedInfo.Instantiated.Count} 個生成的物件");
                
                // 銷毀所有生成的物件
                for (int i = 0; i < generatedInfo.Instantiated.Count; i++)
                {
                    if (generatedInfo.Instantiated[i] != null)
                    {
                        if (Application.isPlaying)
                        {
                            Destroy(generatedInfo.Instantiated[i]);
                        }
                        else
                        {
                            DestroyImmediate(generatedInfo.Instantiated[i]);
                        }
                    }
                }
                
                generatedInfo = null;
            }

            // 清除 NavMesh 組件
            ClearNavMeshComponents();
        }

        /// <summary>
        /// 手動烘焙 NavMesh
        /// </summary>
        [Button("烘焙 NavMesh")]
        [EnableIf("IsMapGenerated")]
        public void BakeNavMesh()
        {
            if (!IsMapGenerated)
            {
                Debug.LogWarning("[RuntimeMapGenerator] 尚未生成地圖，無法烘焙 NavMesh");
                return;
            }

            // 尋找或創建 NavMesh 組件
            SetupNavMeshComponents();

            if (navMeshSurface != null)
            {
                navMeshSurface.BuildNavMesh();
                Debug.Log("[RuntimeMapGenerator] NavMesh 烘焙完成");
            }
        }

        /// <summary>
        /// 清除 NavMesh
        /// </summary>
        [Button("清除 NavMesh")]
        public void ClearNavMesh()
        {
            if (navMeshSurface != null)
            {
                navMeshSurface.RemoveData();
                Debug.Log("[RuntimeMapGenerator] NavMesh 已清除");
            }
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 延遲烘焙 NavMesh
        /// </summary>
        private IEnumerator BakeNavMeshWithDelay()
        {
            yield return new WaitForSeconds(bakeDelay);
            BakeNavMesh();
        }

        /// <summary>
        /// 設定 NavMesh 組件
        /// </summary>
        private void SetupNavMeshComponents()
        {
            // 尋找或創建 NavMeshSurface
            if (navMeshSurface == null)
            {
                navMeshSurface = GetComponent<NavMeshSurface>();
                if (navMeshSurface == null)
                {
                    navMeshSurface = gameObject.AddComponent<NavMeshSurface>();
                    
                    // 設定基本參數
                    navMeshSurface.collectObjects = CollectObjects.All;
                    navMeshSurface.useGeometry = NavMeshCollectGeometry.RenderMeshes;
                    
                    Debug.Log("[RuntimeMapGenerator] 已添加 NavMeshSurface 組件");
                }
            }

            // 尋找或創建 NavMeshRuntimeBaker
            if (navMeshBaker == null)
            {
                navMeshBaker = GetComponent<NavMeshRuntimeBaker>();
                if (navMeshBaker == null)
                {
                    navMeshBaker = gameObject.AddComponent<NavMeshRuntimeBaker>();
                    navMeshBaker.surface = navMeshSurface;
                    navMeshBaker.bakeOnStart = false; // Runtime 控制烘焙時機
                    
                    Debug.Log("[RuntimeMapGenerator] 已添加 NavMeshRuntimeBaker 組件");
                }
            }
        }

        /// <summary>
        /// 清除 NavMesh 組件
        /// </summary>
        private void ClearNavMeshComponents()
        {
            if (navMeshSurface != null)
            {
                navMeshSurface.RemoveData();
            }
        }

        #endregion

        #region Unity 事件

        private void OnDestroy()
        {
            // 確保在物件銷毀時清理地圖
            ClearMap();
        }

        private void OnDrawGizmosSelected()
        {
            if (fieldSetup == null) return;

            // 繪製地圖預覽
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.3f);
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);

            Vector3 origin = Vector3.zero;
            if (centerOrigin)
            {
                origin = new Vector3(-mapSize.x / 2f, 0, -mapSize.z / 2f);
            }

            Vector3 cellSize = fieldSetup.GetCellUnitSize();
            
            for (int x = 0; x < mapSize.x; x++)
            {
                for (int z = 0; z < mapSize.z; z++)
                {
                    Vector3 cellPosition = Vector3.Scale(cellSize, new Vector3(x, 0, z)) + Vector3.Scale(origin, cellSize);
                    Gizmos.DrawWireCube(cellPosition, new Vector3(cellSize.x, cellSize.x * 0.1f, cellSize.z));
                }
            }

            Gizmos.matrix = Matrix4x4.identity;
        }

        #endregion
    }
}