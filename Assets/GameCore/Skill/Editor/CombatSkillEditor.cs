using UnityEngine;
using UnityEditor;

namespace Wuxia.GameCore
{
    [CustomEditor(typeof(CombatSkill))]
    public class CombatSkillEditor : Editor
    {
        private SerializedProperty skillName;
        private SerializedProperty animationName;
        private SerializedProperty targetingMode;
        private SerializedProperty skillRange;
        private SerializedProperty isFixedRange;
        private SerializedProperty skillAngle;
        private SerializedProperty targetableFactions;
        private SerializedProperty attackMultiplier;
        private SerializedProperty dodgeChance;
        private SerializedProperty defense;
        private SerializedProperty spCost;

        void OnEnable()
        {
            skillName = serializedObject.FindProperty("skillName");
            animationName = serializedObject.FindProperty("animationName");
            targetingMode = serializedObject.FindProperty("targetingMode");
            skillRange = serializedObject.FindProperty("skillRange");
            isFixedRange = serializedObject.FindProperty("isFixedRange");
            skillAngle = serializedObject.FindProperty("skillAngle");
            targetableFactions = serializedObject.FindProperty("targetableFactions");
            attackMultiplier = serializedObject.FindProperty("attackMultiplier");
            dodgeChance = serializedObject.FindProperty("dodgeChance");
            defense = serializedObject.FindProperty("defense");
            spCost = serializedObject.FindProperty("spCost");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // 技能基本資訊
            EditorGUILayout.LabelField("技能基本資訊", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(skillName);
            EditorGUILayout.PropertyField(animationName);

            EditorGUILayout.Space();

            // 瞄準模式
            EditorGUILayout.LabelField("瞄準模式", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(targetingMode);

            EditorGUILayout.Space();

            // 技能範圍參數
            EditorGUILayout.LabelField("技能範圍參數", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(skillRange);

            // 根據瞄準模式顯示對應的選項
            SkillTargetingMode currentMode = (SkillTargetingMode)targetingMode.enumValueIndex;

            if (currentMode == SkillTargetingMode.FrontDash)
            {
                // FrontDash 模式顯示固定距離選項
                EditorGUILayout.PropertyField(isFixedRange);
            }
            else if (currentMode == SkillTargetingMode.StandStill)
            {
                // StandStill 模式顯示角度選項
                EditorGUILayout.PropertyField(skillAngle);
            }

            EditorGUILayout.Space();

            // 目標設定
            EditorGUILayout.LabelField("目標設定", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(targetableFactions);

            EditorGUILayout.Space();

            // 戰鬥屬性
            EditorGUILayout.LabelField("戰鬥屬性", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(attackMultiplier);
            EditorGUILayout.PropertyField(dodgeChance);
            EditorGUILayout.PropertyField(defense);

            EditorGUILayout.Space();

            // 消耗
            EditorGUILayout.LabelField("消耗", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(spCost);

            serializedObject.ApplyModifiedProperties();
        }
    }
}