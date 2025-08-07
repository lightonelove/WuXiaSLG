using UnityEngine;

public class CombatAction
{
    public enum ActionType {SkillA, SkillB, SkillC, SkillD, Move}

    public ActionType type;
    public Vector3 Position;
    public Quaternion rotation;
    public Vector3 targetPosition; // 技能目標位置
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
