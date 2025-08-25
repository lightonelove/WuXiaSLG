using UnityEngine;

namespace GameCore.Stats
{
    [CreateAssetMenu(fileName = "CombatEntityStats", menuName = "WuXiaSLG/Entity Stats", order = 1)]
    public class CombatEntityStats : ScriptableObject
    {
        [Header("基礎屬性")]
        [Tooltip("敏捷 (Dexterity) - 影響命中率和閃避")]
        [Range(1, 100)]
        public int DEX = 10;

        [Tooltip("速度 (Agility) - 影響行動順序和移動速度")]
        [Range(1, 100)]
        public int AGI = 10;

        [Tooltip("體質 (Constitution) - 影響生命值和防禦力")]
        [Range(1, 100)]
        public int CON = 10;

        [Tooltip("力量 (Strength) - 影響物理攻擊傷害")]
        [Range(1, 100)]
        public int STR = 10;

        [Header("特殊屬性")]
        [Tooltip("內力/氣 (Chi) - 影響內功技能和真氣值")]
        [Range(1, 100)]
        public int CHI = 10;

        [Tooltip("魅力 (Charisma) - 影響社交互動和團隊增益")]
        [Range(1, 100)]
        public int CHA = 10;

        [Tooltip("幸運 (Luck) - 影響爆擊率和掉寶率")]
        [Range(1, 100)]
        public int LUCK = 10;

        [Tooltip("感知 (Perception) - 影響偵測範圍和先制攻擊")]
        [Range(1, 100)]
        public int PERCEPTION = 10;

        [Header("衍生屬性")]
        [Tooltip("最大生命值")]
        public int MaxHealth => CON * 10 + STR * 5;

        [Tooltip("最大內力值")]
        public int MaxChi => CHI * 20;

        [Tooltip("基礎速度值")]
        public float Speed => AGI * 1.5f;

        [Tooltip("物理攻擊力")]
        public int PhysicalAttack => STR * 2 + DEX;

        [Tooltip("內功攻擊力")]
        public int ChiAttack => CHI * 3;

        [Tooltip("防禦力")]
        public int Defense => CON * 2 + STR / 2;

        [Tooltip("命中率")]
        public float HitRate => 70f + DEX * 0.5f + PERCEPTION * 0.3f;

        [Tooltip("閃避率")]
        public float DodgeRate => 10f + AGI * 0.4f + LUCK * 0.2f;

        [Tooltip("爆擊率")]
        public float CriticalRate => 5f + LUCK * 0.3f + DEX * 0.2f;

        [Tooltip("爆擊傷害倍率")]
        public float CriticalDamage => 1.5f + LUCK * 0.01f;

        [Tooltip("格擋率 (僅敵人使用)")]
        public float BlockRate => PERCEPTION * 1.5f + DEX * 2.5f;

        public void CopyFrom(CombatEntityStats other)
        {
            if (other == null) return;

            DEX = other.DEX;
            AGI = other.AGI;
            CON = other.CON;
            STR = other.STR;
            CHI = other.CHI;
            CHA = other.CHA;
            LUCK = other.LUCK;
            PERCEPTION = other.PERCEPTION;
        }

        public void Reset()
        {
            DEX = 10;
            AGI = 10;
            CON = 10;
            STR = 10;
            CHI = 10;
            CHA = 10;
            LUCK = 10;
            PERCEPTION = 10;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            DEX = Mathf.Clamp(DEX, 1, 100);
            AGI = Mathf.Clamp(AGI, 1, 100);
            CON = Mathf.Clamp(CON, 1, 100);
            STR = Mathf.Clamp(STR, 1, 100);
            CHI = Mathf.Clamp(CHI, 1, 100);
            CHA = Mathf.Clamp(CHA, 1, 100);
            LUCK = Mathf.Clamp(LUCK, 1, 100);
            PERCEPTION = Mathf.Clamp(PERCEPTION, 1, 100);
        }
#endif
    }
}