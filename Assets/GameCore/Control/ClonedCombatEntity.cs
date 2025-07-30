using UnityEngine;

public class ClonedCombatEntity 
{

    public string Name;
    public float Speed;
    public float ActionValue; 
    public CombatEntity realEntity;

    public ClonedCombatEntity(string namne, float speed, float actionValue, CombatEntity entity)
    {
        Name = namne;
        Speed = speed;
        ActionValue = actionValue;
        realEntity = entity;
    }
    
    public void AdvanceActionValue(float time)
    {
        // 確保速度不是 0 或負數，避免出錯
        if (this.Speed <= 0) return;
        this.ActionValue += this.Speed * time;
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
}
