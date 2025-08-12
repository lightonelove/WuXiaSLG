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
    [SerializeField] private bool isFixedRange = false; // 是否固定距離（不跟隨滑鼠）
    
    [Header("目標設定")]
    [SerializeField] private CombatEntityFaction targetableFactions = CombatEntityFaction.Hostile; // 可瞄準的陣營
    
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
    public bool IsFixedRange => isFixedRange;
    public CombatEntityFaction TargetableFactions => targetableFactions;
    public float AttackMultiplier => attackMultiplier;
    public float DodgeChance => dodgeChance;
    public float Defense => defense;
    public int SPCost => spCost;
    
    /// <summary>
    /// 檢查指定陣營是否可以被此技能瞄準
    /// </summary>
    /// <param name="faction">要檢查的陣營</param>
    /// <returns>是否可以瞄準</returns>
    public bool CanTargetFaction(CombatEntityFaction faction)
    {
        return (targetableFactions & faction) != 0;
    }
}