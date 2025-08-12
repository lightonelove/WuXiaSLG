using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 戰鬥實體陣營類型
/// </summary>
public enum CombatEntityFaction
{
    Ally,       // 友軍
    Neutral,    // 中立
    Hostile     // 敵對
}

public class CombatEntity : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public string Name;
    public float Speed;
    public float ActionValue; // 當前的行動值
    public Sprite PortraitIcon;
    public CharacterCore CharacterCore;
    
    [Header("陣營設定")]
    public CombatEntityFaction Faction = CombatEntityFaction.Neutral;

    
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
