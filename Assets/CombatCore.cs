using System.Collections.Generic;
using UnityEngine;

public class CombatCore : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public static CombatCore Instance;
    
    public List<EnemyCore> AllEnemies;
    public List<CharacterCore> AllCharacters;
    
    
    void Start()
    {
        Instance = this;
        
        AllEnemies = new List<EnemyCore>();
        EnemyCore[] enemiesInScene = FindObjectsOfType<EnemyCore>();
        AllEnemies.AddRange(enemiesInScene);
        
        
        AllCharacters = new List<CharacterCore>();
        CharacterCore[] characterInScene = FindObjectsOfType<CharacterCore>();
        AllCharacters.AddRange(characterInScene);
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
