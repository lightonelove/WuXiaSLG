using UnityEngine;

public enum SkillTargetingMode
{
    FrontDash,    // 前方衝刺瞄準模式（目前實作的模式）
    StandStill,   // 原地施放瞄準模式（待實作）
}

[CreateAssetMenu(fileName = "NewCombatSkill", menuName = "WuXiaSLG/CombatSkill", order = 1)]
public class CombatSkill : ScriptableObject
{
    [Header("技能基本資訊")]
    [SerializeField] private string skillName;
    [SerializeField] private string animationName;
    
    [Header("瞄準模式")]
    [SerializeField] private SkillTargetingMode targetingMode = SkillTargetingMode.FrontDash;
    
    [Header("技能範圍參數")]
    [SerializeField] [Range(1f, 360f)] private float skillAngle = 60f; // 技能角度（扇形範圍）
    [SerializeField] [Range(0.5f, 20f)] private float skillRange = 5f; // 技能距離（半徑）
    
    [Header("戰鬥屬性")]
    [SerializeField] private float attackMultiplier = 1.0f;
    [SerializeField] [Range(0f, 1f)] private float dodgeChance = 0f;
    [SerializeField] private float defense = 0f;
    
    [Header("消耗")]
    [SerializeField] private int spCost = 20;
    
    public string SkillName => skillName;
    public string AnimationName => animationName;
    public SkillTargetingMode TargetingMode => targetingMode;
    public float SkillAngle => skillAngle;
    public float SkillRange => skillRange;
    public float AttackMultiplier => attackMultiplier;
    public float DodgeChance => dodgeChance;
    public float Defense => defense;
    public int SPCost => spCost;
}