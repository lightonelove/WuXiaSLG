using UnityEngine;
using UnityEditor;


/// <summary>
/// TargetedIndicator 自動設定 Editor 腳本
/// 在 Prefab 原型存檔時自動設定 CombatEntity 引用
/// </summary>
namespace Wuxia.GameCore
{
    [InitializeOnLoad]
    public class TargetedIndicatorAutoSetup
    {
        static TargetedIndicatorAutoSetup()
        {
            // 訂閱 Prefab 編輯模式的事件
            UnityEditor.SceneManagement.PrefabStage.prefabSaving += OnPrefabSaving;
            UnityEditor.SceneManagement.PrefabStage.prefabSaved += OnPrefabSaved;

        }

        /// <summary>
        /// 當 Prefab 即將儲存時執行
        /// </summary>
        /// <param name="obj">正在儲存的 Prefab 根物件</param>
        private static void OnPrefabSaving(GameObject obj)
        {
            if (Application.isPlaying || obj == null)
                return;

            ProcessPrefabForTargetedIndicators(obj, "Prefab Saving");
        }

        /// <summary>
        /// 當 Prefab 已經儲存後執行
        /// </summary>
        /// <param name="obj">已儲存的 Prefab 根物件</param>
        private static void OnPrefabSaved(GameObject obj)
        {
            if (Application.isPlaying || obj == null)
                return;

            Debug.Log($"[TargetedIndicatorAutoSetup] Prefab saved: {obj.name}");
        }

        /// <summary>
        /// 處理 Prefab 中的 TargetedIndicator 組件
        /// </summary>
        /// <param name="prefabRoot">Prefab 根物件</param>
        /// <param name="context">執行上下文</param>
        private static void ProcessPrefabForTargetedIndicators(GameObject prefabRoot, string context)
        {
            // 檢查這個 Prefab 及其子物件是否有 TargetedIndicator
            TargetedIndicator[] indicators = prefabRoot.GetComponentsInChildren<TargetedIndicator>(true);

            if (indicators.Length > 0)
            {
                Debug.Log(
                    $"[TargetedIndicatorAutoSetup] {context}: Processing {indicators.Length} TargetedIndicator(s) in prefab: {prefabRoot.name}");

                bool anyChanges = false;

                foreach (TargetedIndicator indicator in indicators)
                {
                    if (indicator != null)
                    {
                        // 檢查是否需要更新 CombatEntity 引用
                        CombatEntity currentEntity = indicator.GetCombatEntity();
                        if (currentEntity == null)
                        {
                            indicator.AutoFindCombatEntity();
                            anyChanges = true;
                            Debug.Log(
                                $"[TargetedIndicatorAutoSetup] Auto-found CombatEntity for {indicator.gameObject.name} in prefab {prefabRoot.name}");
                        }
                        else
                        {
                            Debug.Log(
                                $"[TargetedIndicatorAutoSetup] CombatEntity already assigned for {indicator.gameObject.name} in prefab {prefabRoot.name}");
                        }
                    }
                }

                // 如果有變更，標記相關物件為已修改
                if (anyChanges)
                {
                    foreach (TargetedIndicator indicator in indicators)
                    {
                        if (indicator != null)
                        {
                            EditorUtility.SetDirty(indicator);
                        }
                    }

                    EditorUtility.SetDirty(prefabRoot);
                    Debug.Log(
                        $"[TargetedIndicatorAutoSetup] Updated CombatEntity references in prefab: {prefabRoot.name}");
                }
            }
        }
    }

    /// <summary>
    /// TargetedIndicator 組件的自訂 Inspector
    /// 提供手動設定按鈕和 CombatEntity 狀態顯示
    /// </summary>
    [CustomEditor(typeof(TargetedIndicator))]
    public class TargetedIndicatorEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            // 顯示預設的 Inspector
            DrawDefaultInspector();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("CombatEntity Auto Setup", EditorStyles.boldLabel);

            TargetedIndicator indicator = (TargetedIndicator)target;

            // 顯示 CombatEntity 狀態
            CombatEntity combatEntity = indicator.GetCombatEntity();
            if (combatEntity != null)
            {
                EditorGUILayout.LabelField($"CombatEntity: {combatEntity.gameObject.name}", EditorStyles.miniLabel);

                var oldColor = GUI.color;
                GUI.color = Color.green;
                EditorGUILayout.LabelField("✓ CombatEntity Reference Found", EditorStyles.boldLabel);
                GUI.color = oldColor;
            }
            else
            {
                var oldColor = GUI.color;
                GUI.color = Color.red;
                EditorGUILayout.LabelField("✗ No CombatEntity Reference", EditorStyles.boldLabel);
                GUI.color = oldColor;
            }

            EditorGUILayout.Space();

            // 手動設定按鈕
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Auto Find CombatEntity"))
            {
                indicator.AutoFindCombatEntity();
            }

            if (GUILayout.Button("Clear Reference"))
            {
                indicator.ClearCombatEntity();
            }

            if (GUILayout.Button("Validate Reference"))
            {
                indicator.ValidateCombatEntity();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "CombatEntity reference will be automatically found when prefab is saved or scene is saved.",
                MessageType.Info);

            // 搜尋範圍說明
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Search Order:", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("1. Same GameObject", EditorStyles.miniLabel);
            EditorGUILayout.LabelField("2. Parent GameObjects", EditorStyles.miniLabel);
            EditorGUILayout.LabelField("3. Child GameObjects", EditorStyles.miniLabel);

            // 如果有任何修改，標記為 dirty
            if (GUI.changed)
            {
                EditorUtility.SetDirty(indicator);
            }
        }
    }
}