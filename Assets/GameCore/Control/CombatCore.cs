using System.Collections.Generic;
using UnityEngine;

public class CombatCore : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public static CombatCore Instance;
    
    public List<EnemyCore> AllEnemies;
    public List<CharacterCore> AllCharacters;

    public List<CombatEntity> AllCombatEntity;
    
    public const float ACTION_THRESHOLD = 500;
    
    
    void Start()
    {
        Instance = this;
        
        AllEnemies = new List<EnemyCore>();
        EnemyCore[] enemiesInScene = FindObjectsOfType<EnemyCore>();
        AllEnemies.AddRange(enemiesInScene);
        
        
        AllCharacters = new List<CharacterCore>();
        CharacterCore[] characterInScene = FindObjectsOfType<CharacterCore>();
        AllCharacters.AddRange(characterInScene);

        AllCombatEntity = new List<CombatEntity>();
        CombatEntity[] combatEntityInScene = FindObjectsOfType<CombatEntity>();
        AllCombatEntity.AddRange(combatEntityInScene);

        List<ClonedCombatEntity> ClonedCombatEntity = PredictTurnOrder(10);
        SLGCoreUI.Instance.turnOrderUIController.UpdateTurnOrderDisplay(ClonedCombatEntity);

        for (int i = 0; i < ClonedCombatEntity.Count; ++i)
        {
            Debug.Log(ClonedCombatEntity[i].Name);
        }
    }

    public List<ClonedCombatEntity> PredictTurnOrder(int numberOfTurns)
    {
        var predictedOrder = new List<ClonedCombatEntity>();
        List<ClonedCombatEntity> simulatedCombatants = new List<ClonedCombatEntity>();

        // 1. 建立當前戰鬥狀態的深度複製副本
        for (int i = 0; i < AllCombatEntity.Count; ++i)
        {
            simulatedCombatants.Add(AllCombatEntity[i].GetClone());
        }
        
        // 2. 在副本上進行 N 次模擬
        for (int i = 0; i < numberOfTurns; i++)
        {
            // 呼叫核心邏輯，但傳入的是模擬用的列表
            ClonedCombatEntity nextActorInSim = CalculateNextActor(simulatedCombatants);
            if (nextActorInSim != null)
            {
                predictedOrder.Add(nextActorInSim);
            }
            else
            {
                break; // 如果沒有下一個行動者，提前結束模擬
            }
        }

        return predictedOrder;
    }
    
    private ClonedCombatEntity CalculateNextActor(List<ClonedCombatEntity> characterList)
    {
        float minTimeToAct = float.MaxValue;
        ClonedCombatEntity nextActor = null;

        foreach (var character in characterList)
        {
            if (character.Speed <= 0) continue;
            float remainingValue = ACTION_THRESHOLD - character.ActionValue;
            float timeToAct = remainingValue / character.Speed;
            
            if (timeToAct < minTimeToAct)
            {
                minTimeToAct = timeToAct;
                nextActor = character;
            }
        }
        
        if(nextActor == null) return null;

        // 推進傳入列表中的所有角色時間
        foreach (var character in characterList)
        {
            character.AdvanceActionValue(minTimeToAct);
        }

        // 處理行動完的角色的行動值
        nextActor.ActionValue -= ACTION_THRESHOLD;

        return nextActor;
    }
    
    public void ConfirmAction()
    {
        for (int i = 0; i < AllEnemies.Count; i++)
        {
            AllEnemies[i].ReturnFromPreview();
        }
        
    }
    
    // Update is called once per frame
    void Update()
    {
        
    }
}
