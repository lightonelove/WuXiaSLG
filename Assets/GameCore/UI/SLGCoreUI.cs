using UnityEngine;

public class SLGCoreUI : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    public APBar apBar;
    public TurnOrderUIController turnOrderUIController;
    public static SLGCoreUI Instance;
    void Start()
    {
        Instance = this;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
