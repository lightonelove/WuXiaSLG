#if UNITY_EDITOR
using FIMSpace.FEditor;
#endif
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace FIMSpace.Generating
{
    [AddComponentMenu("FImpossible Creations/PGG/Simple Field Generator", 101)]
    public class SimpleFieldGenerator : MonoBehaviour
    {
        public bool GenrateOnGameStart = false;
        public bool RandomSeed = true;
        public int Seed = 0;

        [Space(3)]
        public FieldSetup FieldPreset;
        [Space(2)]
        [Tooltip("Enable random field size generation")]
        public bool UseRandomSize = false;
        [Tooltip("Fixed field size (when UseRandomSize is disabled)")]
        public Vector3Int FieldSizeInCells = new Vector3Int(5, 0, 4);
        [Space(2)]
        [Header("Random Size Settings (when UseRandomSize is enabled)")]
        [Tooltip("Random range for field width (X-axis)")]
        public Vector2Int RandomWidthRange = new Vector2Int(3, 8);
        [Tooltip("Random range for field height (Y-axis)")]  
        public Vector2Int RandomHeightRange = new Vector2Int(0, 0);
        [Tooltip("Random range for field depth (Z-axis)")]
        public Vector2Int RandomDepthRange = new Vector2Int(3, 6);
        [Space(2)]
        public bool CenterOrigin = false;

        [SerializeField] [HideInInspector] public InstantiatedFieldInfo Generated;
        [HideInInspector] public UnityEvent RunAfterGenerating;

        private void Start()
        {
            if (GenrateOnGameStart)
            {
                Generate();
            }
        }

        public void Generate()
        {
            Generate(null);
        }

        public void Generate(List<SpawnInstruction> guides)
        {
            if (RandomSeed) Seed = FGenerators.GetRandom(-99999, 99999);
            ClearGenerated();

            if (FieldPreset == null) return;

            // Determine field size based on settings
            Vector3Int currentFieldSize = GetCurrentFieldSize();
            
            Vector3Int origin = Vector3Int.zero;
            if (CenterOrigin) origin = new Vector3Int(-currentFieldSize.x / 2, 0, -currentFieldSize.z / 2);
            Generated = IGeneration.GenerateFieldObjectsRectangleGrid(FieldPreset, currentFieldSize, Seed, transform, true, guides, true, origin);
            if (RunAfterGenerating != null) RunAfterGenerating.Invoke();
        }

        /// <summary>
        /// Gets the current field size based on whether random size is enabled
        /// </summary>
        private Vector3Int GetCurrentFieldSize()
        {
            if (UseRandomSize)
            {
                // Generate random size within specified ranges
                int randomWidth = Random.Range(RandomWidthRange.x, RandomWidthRange.y + 1);
                int randomHeight = Random.Range(RandomHeightRange.x, RandomHeightRange.y + 1);
                int randomDepth = Random.Range(RandomDepthRange.x, RandomDepthRange.y + 1);
                
                return new Vector3Int(randomWidth, randomHeight, randomDepth);
            }
            else
            {
                return FieldSizeInCells;
            }
        }

        public void ClearGenerated()
        {
            if (Generated != null)
                if (Generated.Instantiated != null)
                {
                    for (int i = 0; i < Generated.Instantiated.Count; i++)
                        if (Generated.Instantiated[i] != null)
                            FGenerators.DestroyObject(Generated.Instantiated[i]);
                }

        }


        #region Gizmos

        private void OnDrawGizmosSelected()
        {
            if (FieldPreset == null) return;

            Color preColor = GUI.color;
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
            
            Vector3 presetSize = FieldPreset.GetCellUnitSize();

            if (UseRandomSize)
            {
                // Draw range preview for random size
                Gizmos.color = new Color(1f, 1f, 0f, 0.3f); // Yellow for random ranges
                
                // Draw min size
                Vector3Int minSize = new Vector3Int(RandomWidthRange.x, RandomHeightRange.x, RandomDepthRange.x);
                Vector3 minOrigin = Vector3.zero;
                if (CenterOrigin) minOrigin = new Vector3(-minSize.x / 2f, 0, -minSize.z / 2f);
                DrawFieldSizeGizmo(minSize, minOrigin, presetSize);
                
                // Draw max size  
                Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f); // Orange for max range
                Vector3Int maxSize = new Vector3Int(RandomWidthRange.y, RandomHeightRange.y, RandomDepthRange.y);
                Vector3 maxOrigin = Vector3.zero;
                if (CenterOrigin) maxOrigin = new Vector3(-maxSize.x / 2f, 0, -maxSize.z / 2f);
                DrawFieldSizeGizmo(maxSize, maxOrigin, presetSize);
            }
            else
            {
                // Draw fixed size
                Gizmos.color = new Color(1f, 1f, 1f, 0.5f);
                Vector3 origin = Vector3.zero;
                if (CenterOrigin) origin = new Vector3(-FieldSizeInCells.x / 2f, 0, -FieldSizeInCells.z / 2f);
                DrawFieldSizeGizmo(FieldSizeInCells, origin, presetSize);
            }

            Gizmos.color = preColor;
            Gizmos.matrix = Matrix4x4.identity;
        }

        private void DrawFieldSizeGizmo(Vector3Int size, Vector3 origin, Vector3 presetSize)
        {
            for (int x = 0; x < size.x; x++)
            {
                for (int y = 0; y <= size.y; y++)
                    for (int z = 0; z < size.z; z++)
                    {
                        Vector3 genPosition = Vector3.Scale(presetSize, new Vector3(x, y, z)) + origin * presetSize.x;
                        Gizmos.DrawWireCube(genPosition, new Vector3(presetSize.x, presetSize.x * 0.2f, presetSize.x));
                    }
            }
        }

        #endregion

    }

    #region Drawing 'Generate' and 'Clear' buttons inside inspector window


#if UNITY_EDITOR
    [UnityEditor.CanEditMultipleObjects]
    [UnityEditor.CustomEditor(typeof(SimpleFieldGenerator))]
    public class ExampleSimpleFieldGeneratorEditor : UnityEditor.Editor
    {
        public SimpleFieldGenerator Get { get { if (_get == null) _get = (SimpleFieldGenerator)target; return _get; } }
        private SimpleFieldGenerator _get;
        bool displayEvent = false;

        public override void OnInspectorGUI()
        {
            UnityEditor.EditorGUILayout.HelpBox("This component is just simple generator for choosed 'Field Setup', you should use 'GRID PAINTER' for more customized generation!", UnityEditor.MessageType.Info);

            FGUI_Inspector.LastGameObjectSelected = Get.gameObject;

            // Show different UI based on UseRandomSize setting
            serializedObject.Update();
            
            UnityEditor.EditorGUILayout.PropertyField(serializedObject.FindProperty("GenrateOnGameStart"));
            UnityEditor.EditorGUILayout.PropertyField(serializedObject.FindProperty("RandomSeed"));
            if (!Get.RandomSeed)
                UnityEditor.EditorGUILayout.PropertyField(serializedObject.FindProperty("Seed"));
            
            UnityEditor.EditorGUILayout.Space(3);
            UnityEditor.EditorGUILayout.PropertyField(serializedObject.FindProperty("FieldPreset"));
            
            UnityEditor.EditorGUILayout.Space(2);
            UnityEditor.EditorGUILayout.PropertyField(serializedObject.FindProperty("UseRandomSize"));
            
            if (Get.UseRandomSize)
            {
                UnityEditor.EditorGUILayout.HelpBox("隨機大小模式：每次生成時會在指定範圍內隨機選擇場地大小\n黃色線框 = 最小尺寸，橘色線框 = 最大尺寸", UnityEditor.MessageType.Info);
                
                UnityEditor.EditorGUILayout.PropertyField(serializedObject.FindProperty("RandomWidthRange"));
                UnityEditor.EditorGUILayout.PropertyField(serializedObject.FindProperty("RandomHeightRange"));
                UnityEditor.EditorGUILayout.PropertyField(serializedObject.FindProperty("RandomDepthRange"));
                
                // Validate ranges
                if (Get.RandomWidthRange.x > Get.RandomWidthRange.y)
                    UnityEditor.EditorGUILayout.HelpBox("寬度範圍：最小值不能大於最大值", UnityEditor.MessageType.Warning);
                if (Get.RandomDepthRange.x > Get.RandomDepthRange.y)
                    UnityEditor.EditorGUILayout.HelpBox("深度範圍：最小值不能大於最大值", UnityEditor.MessageType.Warning);
                if (Get.RandomHeightRange.x > Get.RandomHeightRange.y)
                    UnityEditor.EditorGUILayout.HelpBox("高度範圍：最小值不能大於最大值", UnityEditor.MessageType.Warning);
            }
            else
            {
                UnityEditor.EditorGUILayout.HelpBox("固定大小模式：每次生成都使用相同的場地大小", UnityEditor.MessageType.Info);
                UnityEditor.EditorGUILayout.PropertyField(serializedObject.FindProperty("FieldSizeInCells"));
            }
            
            UnityEditor.EditorGUILayout.Space(2);
            UnityEditor.EditorGUILayout.PropertyField(serializedObject.FindProperty("CenterOrigin"));

            serializedObject.ApplyModifiedProperties();

            GUILayout.Space(4);
            if (GUILayout.Button("Generate")) Get.Generate();
            if (Get.Generated.Instantiated != null) if (Get.Generated.Instantiated.Count > 0) if (GUILayout.Button("Clear Generated")) Get.ClearGenerated();

            displayEvent = UnityEditor.EditorGUILayout.Foldout(displayEvent, "Event After Generating", true);
            if (displayEvent) UnityEditor.EditorGUILayout.PropertyField(serializedObject.FindProperty("RunAfterGenerating"));
        }
    }
#endif


    #endregion

}