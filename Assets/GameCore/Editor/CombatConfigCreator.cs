using UnityEngine;
using UnityEditor;

namespace Wuxia.GameCore
{
    public class CombatConfigCreator
    {
        [MenuItem("Wuxia/Create Combat Config")]
        public static void CreateCombatConfig()
        {
            // Check if config already exists
            string resourcePath = "Assets/GameCore/Resources/CombatConfig.asset";
            CombatConfig existingConfig = AssetDatabase.LoadAssetAtPath<CombatConfig>(resourcePath);
            
            if (existingConfig != null)
            {
                bool overwrite = EditorUtility.DisplayDialog(
                    "Combat Config Exists", 
                    "A CombatConfig already exists. Do you want to select it instead of creating a new one?", 
                    "Select Existing", 
                    "Create New"
                );
                
                if (overwrite)
                {
                    Selection.activeObject = existingConfig;
                    EditorGUIUtility.PingObject(existingConfig);
                    return;
                }
            }
            
            // Create new config
            CombatConfig config = ScriptableObject.CreateInstance<CombatConfig>();
            
            // Ensure Resources folder exists
            string resourcesFolder = "Assets/GameCore/Resources";
            if (!AssetDatabase.IsValidFolder(resourcesFolder))
            {
                AssetDatabase.CreateFolder("Assets/GameCore", "Resources");
            }
            
            // Create asset
            string assetPath = resourcePath;
            if (existingConfig != null)
            {
                assetPath = AssetDatabase.GenerateUniqueAssetPath(resourcePath);
            }
            
            AssetDatabase.CreateAsset(config, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            // Select the created asset
            Selection.activeObject = config;
            EditorGUIUtility.PingObject(config);
            
            Debug.Log($"Created CombatConfig at: {assetPath}");
        }
        
        [MenuItem("Wuxia/Select Combat Config")]
        public static void SelectCombatConfig()
        {
            string resourcePath = "Assets/GameCore/Resources/CombatConfig.asset";
            CombatConfig config = AssetDatabase.LoadAssetAtPath<CombatConfig>(resourcePath);
            
            if (config != null)
            {
                Selection.activeObject = config;
                EditorGUIUtility.PingObject(config);
            }
            else
            {
                bool create = EditorUtility.DisplayDialog(
                    "Combat Config Not Found", 
                    "CombatConfig not found. Would you like to create one?", 
                    "Create", 
                    "Cancel"
                );
                
                if (create)
                {
                    CreateCombatConfig();
                }
            }
        }
    }
}