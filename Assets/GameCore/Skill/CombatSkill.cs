using UnityEngine;

[CreateAssetMenu(fileName = "NewCombatSkill", menuName = "WuXiaSLG/CombatSkill", order = 1)]
public class CombatSkill : ScriptableObject
{
    [Header("技能基本資訊")]
    [SerializeField] private string skillName;
    [SerializeField] private string animationName;
    
    [Header("戰鬥屬性")]
    [SerializeField] private float attackMultiplier = 1.0f;
    [SerializeField] [Range(0f, 1f)] private float dodgeChance = 0f;
    [SerializeField] private float defense = 0f;
    
    [Header("消耗")]
    [SerializeField] private int spCost = 20;
    
    public string SkillName => skillName;
    public string AnimationName => animationName;
    public float AttackMultiplier => attackMultiplier;
    public float DodgeChance => dodgeChance;
    public float Defense => defense;
    public int SPCost => spCost;
}