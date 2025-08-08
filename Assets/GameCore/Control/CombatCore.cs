using System.Collections.Generic;
using UnityEngine;
using System.Collections;

public enum CombatState
{
    Initializing,    // 戰鬥初始化中
    WaitingForTurn,  // 等待下一回合
    EntityTurn,      // 某個實體的回合
    ProcessingAction,// 處理行動中
    CombatEnd        // 戰鬥結束
}

public class CombatCore : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public static CombatCore Instance;
    
    public List<EnemyCore> AllEnemies;
    public List<CharacterCore> AllCharacters;

    public List<CombatEntity> AllCombatEntity;
    public List<CombatEntity> CurrentCombatEntityQueue;
    
    public const float ACTION_THRESHOLD = 500;
    public CombatEntity currentRoundEntity;
    
    [Header("Combat State Management")]
    public CombatState currentCombatState = CombatState.Initializing;
    public bool isCombatActive = false;
    public int currentTurnNumber = 0;
    
    [Header("Turn Management")]
    public float turnTransitionDelay = 0.5f; // 回合轉換間的延遲時間
    private bool isProcessingTurn = false;
    
    
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

        // 初始化戰鬥
        InitializeCombat();
    }
    
    void InitializeCombat()
    {
        currentCombatState = CombatState.Initializing;
        currentTurnNumber = 0;
        
        // 預測並顯示回合順序（初始時還沒有當前回合實體）
        List<ClonedCombatEntity> predictedTurnOrder = PredictTurnOrder(11);  // 多預測一個，因為會在戰鬥開始時使用第一個
        if (SLGCoreUI.Instance != null && SLGCoreUI.Instance.turnOrderUIController != null)
        {
            SLGCoreUI.Instance.turnOrderUIController.UpdateTurnOrderDisplay(predictedTurnOrder);
        }
        
        // 開始戰鬥
        StartCombat();
    }
    
    public void StartCombat()
    {
        if (isCombatActive) return;
        
        isCombatActive = true;
        currentCombatState = CombatState.WaitingForTurn;
        
        // 開始處理回合
        StartCoroutine(CombatLoop());
    }
    
    public void StopCombat()
    {
        isCombatActive = false;
        currentCombatState = CombatState.CombatEnd;
        StopAllCoroutines();
    }
    
    IEnumerator CombatLoop()
    {
        while (isCombatActive)
        {
            // 計算下一個行動者
            CombatEntity nextEntity = CalculateNextActorFromActual();
            
            if (nextEntity == null)
            {
                yield break;
            }
            
            // 設定當前回合實體
            currentRoundEntity = nextEntity;
            currentTurnNumber++;
            
            // 更新UI顯示
            UpdateTurnOrderUI();
            
            // 開始實體的回合
            yield return StartCoroutine(ProcessEntityTurn(nextEntity));
            
            // 回合間的短暫延遲
            yield return new WaitForSeconds(turnTransitionDelay);
            
            // 檢查戰鬥是否結束
            if (CheckCombatEnd())
            {
                StopCombat();
            }
        }
    }
    
    IEnumerator ProcessEntityTurn(CombatEntity entity)
    {
        currentCombatState = CombatState.EntityTurn;
        isProcessingTurn = true;
        
        
        // 判斷是玩家角色還是敵人
        CharacterCore character = entity.GetComponent<CharacterCore>();
        EnemyCore enemy = entity.GetComponent<EnemyCore>();
        
        if (character != null)
        {
            // 玩家回合 - 等待玩家輸入
            yield return StartCoroutine(ProcessPlayerTurn(character));
        }
        else if (enemy != null)
        {
            // 敵人回合 - AI行動
            yield return StartCoroutine(ProcessEnemyTurn(enemy));
        }
        
        isProcessingTurn = false;
        currentCombatState = CombatState.WaitingForTurn;
    }
    
    IEnumerator ProcessPlayerTurn(CharacterCore character)
    {
        
        // 設定角色為控制狀態
        character.nowState = CharacterCore.CharacterCoreState.ControlState;
        
        // 重置動作模式為 None（無動作狀態）
        character.currentActionMode = CharacterCore.PlayerActionMode.None;
        
        // 重置行動點
        character.AP = character.MaxAP;
        
        // 清空之前的行動記錄
        character.RecordedActions.Clear();
        character.points.Clear();
        character.AddPoint(new Vector3(character.transform.position.x, 0.1f, character.transform.position.z));
        
        // 等待玩家完成所有操作直到回合結束
        while (character.nowState != CharacterCore.CharacterCoreState.TurnComplete && isCombatActive)
        {
            // 可以在控制狀態、使用技能狀態、執行狀態、執行技能狀態
            // 只有到達 TurnComplete 才算回合結束
            
            if (character.nowState == CharacterCore.CharacterCoreState.ControlState)
            {
                // 玩家正在控制中
            }
            else if (character.nowState == CharacterCore.CharacterCoreState.UsingSkill)
            {
                // 玩家正在使用技能動畫
            }
            else if (character.nowState == CharacterCore.CharacterCoreState.ExcutionState)
            {
                // 正在執行記錄的動作
            }
            else if (character.nowState == CharacterCore.CharacterCoreState.ExecutingSkill)
            {
                // 正在執行技能動作
            }
            
            yield return null;
        }
        
        // 同步玩家位置（將 CharacterController 位置更新為 CharacterExecutor 的位置）
        
    }
    
    IEnumerator ProcessEnemyTurn(EnemyCore enemy)
    {
        
        currentCombatState = CombatState.ProcessingAction;
        
        // 開始敵人回合
        enemy.StartTurn();
        
        // 等待一小段時間讓玩家看到是誰的回合
        yield return new WaitForSeconds(0.5f);
        
        // 執行敵人的AI邏輯（追蹤玩家）
        yield return StartCoroutine(enemy.ExecuteTurn());
        
        // 等待敵人狀態變為 TurnComplete
        while (enemy.CurrentState != EnemyState.TurnComplete && isCombatActive)
        {
            yield return null;
        }
        
        // 重置敵人狀態為 Idle，準備下一回合
        enemy.SetState(EnemyState.Idle);
        
    }
    
    CombatEntity CalculateNextActorFromActual()
    {
        float minTimeToAct = float.MaxValue;
        CombatEntity nextActor = null;
        
        foreach (var entity in AllCombatEntity)
        {
            if (entity.Speed <= 0) continue;
            
            float remainingValue = ACTION_THRESHOLD - entity.ActionValue;
            float timeToAct = remainingValue / entity.Speed;
            
            if (timeToAct < minTimeToAct)
            {
                minTimeToAct = timeToAct;
                nextActor = entity;
            }
        }
        
        if (nextActor == null) return null;
        
        // 推進所有實體的行動值
        foreach (var entity in AllCombatEntity)
        {
            entity.AdvanceActionValue(minTimeToAct);
        }
        
        // 處理行動完的實體的行動值
        nextActor.ActionValue -= ACTION_THRESHOLD;
        
        return nextActor;
    }
    
    void UpdateTurnOrderUI()
    {
        // 重新預測回合順序
        List<ClonedCombatEntity> predictedOrder = PredictTurnOrder(10);
        
        // 如果有當前回合實體，將其加到列表開頭
        if (currentRoundEntity != null)
        {
            // 創建當前實體的克隆並插入到第一位
            ClonedCombatEntity currentEntityClone = currentRoundEntity.GetClone();
            predictedOrder.Insert(0, currentEntityClone);
        }
        
        if (SLGCoreUI.Instance != null && SLGCoreUI.Instance.turnOrderUIController != null)
        {
            SLGCoreUI.Instance.turnOrderUIController.UpdateTurnOrderDisplay(predictedOrder);
        }
    }
    
    bool CheckCombatEnd()
    {
        // 檢查是否所有敵人都被擊敗
        bool allEnemiesDefeated = true;
        foreach (var enemy in AllEnemies)
        {
            if (enemy.health != null && enemy.health.GetCurrentHealth() > 0)
            {
                allEnemiesDefeated = false;
                break;
            }
        }
        
        if (allEnemiesDefeated)
        {
            return true;
        }
        
        // 檢查是否所有玩家角色都被擊敗
        bool allPlayersDefeated = true;
        foreach (var character in AllCharacters)
        {
            // 假設角色也有Health組件
            Health characterHealth = character.GetComponent<Health>();
            if (characterHealth != null && characterHealth.GetCurrentHealth() > 0)
            {
                allPlayersDefeated = false;
                break;
            }
        }
        
        if (allPlayersDefeated)
        {
            return true;
        }
        
        return false;
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
        
        // 結束當前玩家的回合
        if (currentRoundEntity != null)
        {
            CharacterCore character = currentRoundEntity.GetComponent<CharacterCore>();
            if (character != null && character.nowState == CharacterCore.CharacterCoreState.ControlState)
            {
                // 改變狀態以結束玩家回合
                character.nowState = CharacterCore.CharacterCoreState.ExcutionState;
            }
        }
    }
    
    public void EndCurrentEntityTurn()
    {
        // 強制結束當前實體的回合
        if (currentRoundEntity != null)
        {
            
            CharacterCore character = currentRoundEntity.GetComponent<CharacterCore>();
            if (character != null)
            {
                // 如果角色正在控制狀態，直接跳過執行階段，結束回合
                if (character.nowState == CharacterCore.CharacterCoreState.ControlState)
                {
                    // 清除任何路徑預覽
                    character.ClearPathDisplay();
                    
                    // 如果沒有記錄任何動作，直接結束回合

                    character.nowState = CharacterCore.CharacterCoreState.TurnComplete;
                }
            }
            
            EnemyCore enemy = currentRoundEntity.GetComponent<EnemyCore>();
            if (enemy != null)
            {
                // 強制結束敵人回合
                enemy.SetState(EnemyState.TurnComplete);
            }
        }
    }
    
    public void FindNextRoundEntity()
    {
        // 這個方法現在主要用於手動觸發下一回合
        if (!isCombatActive)
        {
            StartCombat();
        }
    }
    
    public bool IsPlayerTurn()
    {
        if (currentRoundEntity == null) return false;
        
        CharacterCore character = currentRoundEntity.GetComponent<CharacterCore>();
        if (character == null) return false;
        
        // 如果角色已經完成回合，不允許操作
        if (character.nowState == CharacterCore.CharacterCoreState.TurnComplete)
        {
            return false;
        }
        
        return true;
    }
    
    public bool IsEnemyTurn()
    {
        if (currentRoundEntity == null) return false;
        return currentRoundEntity.GetComponent<EnemyCore>() != null;
    }
    
    public CombatEntity GetCurrentTurnEntity()
    {
        return currentRoundEntity;
    }
    
    public int GetCurrentTurnNumber()
    {
        return currentTurnNumber;
    }
    
    // Update is called once per frame
    void Update()
    {
        
    }
}
