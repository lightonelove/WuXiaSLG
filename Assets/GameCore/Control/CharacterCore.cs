using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class CharacterCore : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public CharacterControl characterControl;
    public InputActionAsset controls;
    public CharacterExcutor characterExecutor;

    public enum CharacterCoreState{ ControlState, ExcutionState }

    public CharacterCoreState nowState = CharacterCoreState.ControlState;
    
    void Start()
    {
        
    }

    public bool CheckConfirm()
    {
        
        bool confirmPressed = controls.FindActionMap("GeneralControl").FindAction("Confirm").WasPressedThisFrame();
        if (confirmPressed)
        {
            Debug.Log("ConfirmPressed");
            return true;
        }

        return false;
    }
    
    // Update is called once per frame
    void Update()
    {
        if (nowState == CharacterCoreState.ControlState)
        {
            characterControl.ControlUpdate();
            if (CheckConfirm())
            {
                characterExecutor.RecordedPosition = characterControl.RecordedPositions;
                characterExecutor.RecordedRotation = characterControl.RecordedRotaitons;
                nowState = CharacterCoreState.ExcutionState;
            }
        }
        else if (nowState == CharacterCoreState.ExcutionState)
        {
            characterExecutor.ExecutorUpdate();
            characterControl.ReFillAP();
        }
    }

}