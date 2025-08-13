using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 戰鬥實體陣營類型
/// </summary>
namespace Wuxia.GameCore
{
    [System.Flags]
    public enum CombatEntityFaction
    {
        None = 0,
        Ally = 1 << 0, // 1 (二進制: 001) - 友軍
        Neutral = 1 << 1, // 2 (二進制: 010) - 中立
        Hostile = 1 << 2, // 4 (二進制: 100) - 敵對
        All = Ally | Neutral | Hostile // 7 (二進制: 111) - 所有陣營
    }

    public class CombatEntity : MonoBehaviour
    {
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        public string Name;
        public float Speed;
        public float ActionValue; // 當前的行動值
        public Sprite PortraitIcon;
        public CharacterCore CharacterCore;

        [Header("陣營設定")] public CombatEntityFaction Faction = CombatEntityFaction.Neutral;
        
        
        // 提供一個方法來推進此角色的行動值
        public void AdvanceActionValue(float time)
        {
            // 確保速度不是 0 或負數，避免出錯
            if (this.Speed <= 0) return;
            this.ActionValue += this.Speed * time;
        }

        public ClonedCombatEntity GetClone()
        {
            ClonedCombatEntity clone = new ClonedCombatEntity(Name, Speed, ActionValue, this);
            return clone;
        }
    }
}