using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Events;

/// <summary>
/// CharacterSkills 自動設定 Editor 腳本
/// 在 Prefab 原型存檔時自動設定 ColliderEventReceiver 事件連接
/// </summary>
[InitializeOnLoad]
public class CharacterSkillsAutoSetup
{
    static CharacterSkillsAutoSetup()
    {
        // 訂閱 Prefab 編輯模式的事件
        UnityEditor.SceneManagement.PrefabStage.prefabSaving += OnPrefabSaving;
        UnityEditor.SceneManagement.PrefabStage.prefabSaved += OnPrefabSaved;
        
        // 保留場景存檔事件以支援場景中的 CharacterSkills
        EditorSceneManager.sceneSaving += OnSceneSaving;
        EditorSceneManager.sceneSaved += OnSceneSaved;
    }
    
    /// <summary>
    /// 當 Prefab 即將儲存時執行
    /// </summary>
    /// <param name="obj">正在儲存的 Prefab 根物件</param>
    private static void OnPrefabSaving(GameObject obj)
    {
        if (Application.isPlaying || obj == null)
            return;
            
        ProcessPrefabForCharacterSkills(obj, "Prefab Saving");
    }
    
    /// <summary>
    /// 當 Prefab 已經儲存後執行
    /// </summary>
    /// <param name="obj">已儲存的 Prefab 根物件</param>
    private static void OnPrefabSaved(GameObject obj)
    {
        if (Application.isPlaying || obj == null)
            return;
            
        Debug.Log($"[CharacterSkillsAutoSetup] Prefab saved: {obj.name}");
    }
    
    /// <summary>
    /// 處理 Prefab 中的 CharacterSkills 組件
    /// </summary>
    /// <param name="prefabRoot">Prefab 根物件</param>
    /// <param name="context">執行上下文</param>
    private static void ProcessPrefabForCharacterSkills(GameObject prefabRoot, string context)
    {
        // 檢查這個 Prefab 及其子物件是否有 CharacterSkills
        CharacterSkills[] characterSkills = prefabRoot.GetComponentsInChildren<CharacterSkills>(true);
        
        if (characterSkills.Length > 0)
        {
            Debug.Log($"[CharacterSkillsAutoSetup] {context}: Processing {characterSkills.Length} CharacterSkills component(s) in prefab: {prefabRoot.name}");
            
            foreach (CharacterSkills skills in characterSkills)
            {
                if (skills != null)
                {
                    // 自動設定每個 CharacterSkills 的 ColliderEventReceiver
                    skills.AutoSetupColliderEventReceivers();
                    EditorUtility.SetDirty(skills);
                }
            }
            
            EditorUtility.SetDirty(prefabRoot);
            Debug.Log($"[CharacterSkillsAutoSetup] Auto setup completed for CharacterSkills in prefab: {prefabRoot.name}");
        }
    }
    
    /// <summary>
    /// 場景存檔前執行
    /// </summary>
    /// <param name="scene">正在存檔的場景</param>
    /// <param name="path">場景路徑</param>
    private static void OnSceneSaving(UnityEngine.SceneManagement.Scene scene, string path)
    {
        // 只在非播放模式下執行
        if (Application.isPlaying)
            return;
            
        Debug.Log($"[CharacterSkillsAutoSetup] Scene saving: {scene.name}, auto-setting up ColliderEventReceivers...");
        
        // 找出所有場景中的 CharacterSkills 組件
        CharacterSkills[] characterSkills = Object.FindObjectsOfType<CharacterSkills>();
        
        int totalSetupCount = 0;
        
        foreach (CharacterSkills skills in characterSkills)
        {
            if (skills != null)
            {
                // 自動設定每個 CharacterSkills 的 ColliderEventReceiver
                skills.AutoSetupColliderEventReceivers();
                totalSetupCount++;
            }
        }
        
        if (totalSetupCount > 0)
        {
            Debug.Log($"[CharacterSkillsAutoSetup] Auto setup completed for {totalSetupCount} CharacterSkills component(s) before scene save.");
        }
    }
    
    /// <summary>
    /// 場景存檔後執行
    /// </summary>
    /// <param name="scene">已存檔的場景</param>
    private static void OnSceneSaved(UnityEngine.SceneManagement.Scene scene)
    {
        Debug.Log($"[CharacterSkillsAutoSetup] Scene saved: {scene.name}");
    }
}

/// <summary>
/// CharacterSkills 組件的自訂 Inspector
/// 提供手動設定按鈕和自動設定狀態顯示
/// </summary>
[CustomEditor(typeof(CharacterSkills))]
public class CharacterSkillsEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // 顯示預設的 Inspector
        DrawDefaultInspector();
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("ColliderEventReceiver Auto Setup", EditorStyles.boldLabel);
        
        CharacterSkills characterSkills = (CharacterSkills)target;
        
        // 顯示子物件中 ColliderEventReceiver 的數量
        ColliderEventReceiver[] receivers = characterSkills.GetComponentsInChildren<ColliderEventReceiver>(true);
        EditorGUILayout.LabelField($"Found ColliderEventReceivers: {receivers.Length}");
        
        // 手動設定按鈕
        if (GUILayout.Button("Manual Setup ColliderEventReceivers"))
        {
            characterSkills.AutoSetupColliderEventReceivers();
        }
        
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("ColliderEventReceivers will be automatically configured when prefab is saved or scene is saved.", MessageType.Info);
        
        // 顯示每個 receiver 的狀態
        if (receivers.Length > 0)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("ColliderEventReceiver Status:", EditorStyles.boldLabel);
            
            foreach (ColliderEventReceiver receiver in receivers)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"• {receiver.gameObject.name}", GUILayout.Width(200));
                
                // 檢查 UnityEvent 是否已連接（檢查持久監聽者數量）
                bool hasEvents = receiver.OnTriggerEnterEvent.GetPersistentEventCount() > 0 ||
                                receiver.OnTriggerStayEvent.GetPersistentEventCount() > 0 ||
                                receiver.OnTriggerExitEvent.GetPersistentEventCount() > 0;
                string statusText = hasEvents ? "Connected" : "Not Connected";
                Color statusColor = hasEvents ? Color.green : Color.red;
                
                var oldColor = GUI.color;
                GUI.color = statusColor;
                EditorGUILayout.LabelField(statusText, GUILayout.Width(80));
                GUI.color = oldColor;
                
                EditorGUILayout.EndHorizontal();
            }
        }
        
        // 如果有任何修改，標記為 dirty
        if (GUI.changed)
        {
            EditorUtility.SetDirty(characterSkills);
        }
    }
}