using UnityEngine;

namespace Wuxia.GameCore
{
    [CreateAssetMenu(fileName = "CombatConfig", menuName = "Wuxia/Combat Config")]
    public class CombatConfig : ScriptableObject
    {
        [Header("Turn System")]
        
        [Tooltip("Default turn transition delay in seconds")]
        public float turnTransitionDelay = 0.5f;
        
        [Tooltip("AP cost per meter of movement")]
        public float APCostPerMeter = 5f;
        
        [Tooltip("角色順序計算跑道長度")]
        public float ACTION_THRESHOLD = 500f;
        
        [Tooltip("Default movement speed")]
        public float moveSpeed = 1f;
        
        [Header("Effects")]
        [Tooltip("Damage number display duration")]
        public float damageNumberDuration = 2f;
        
        [Tooltip("Number of predicted turns to display")]
        public int predictedTurnsToShow = 10;
        
        // Singleton instance for easy access
        private static CombatConfig _instance;
        public static CombatConfig Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<CombatConfig>("CombatConfig");
                    if (_instance == null)
                    {
                        Debug.LogError("CombatConfig not found in Resources folder! Please create one.");
                    }
                }
                return _instance;
            }
        }
    }
}