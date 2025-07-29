using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;


public class CharacterCore : MonoBehaviour
{
    // For Control //
    public float moveSpeed = 20f;
    public Transform cameraTransform;
    public float turnRate = 720;
    private Vector2 moveAmount;

    public float ActionPoints = 100;
    public float MaxActionPoints = 100;
    public Vector2 lastPosition;
    
    public float pointSpacing = 0.2f; // 每隔幾公尺新增一點
    public LineRenderer line;
    private List<Vector3> points = new List<Vector3>();
    private Vector3 lastDrawPoint;
    
    public Queue<CombatAction> RecordedActions = new Queue<CombatAction>();
    
    public CharacterController characterController;
    public CharacterController characterControllerForExecutor;
    
    public Animator CharacterControlAnimator;
    public Animator CharacterExecuteAnimator;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public InputActionAsset controls;
    public GameObject characterExecutor;

    public enum CharacterCoreState{ ControlState, ExcutionState, UsingSkill, ExecutingSkill }

    public CharacterCoreState nowState = CharacterCoreState.ControlState;
    
    void Start()
    {
        line.positionCount = 0;
        AddPoint(new Vector3(characterController.transform.position.x, 0.1f, characterController.transform.position.z));
      
    }
    void AddPoint(Vector3 flatPos)
    {
        points.Add(flatPos);
        line.positionCount = points.Count;
        line.SetPositions(points.ToArray());
        lastDrawPoint = flatPos;
    }
    
    public void ControlUpdate()
    {

        Move();
        CheckConfirm();
        lastPosition = new Vector2(characterController.transform.position.x, characterController.transform.position.z);
        
        Vector3 flatCurrentPos = new Vector3(characterController.transform.position.x, 0.1f, characterController.transform.position.z);

        if (Vector3.Distance(flatCurrentPos, lastDrawPoint) >= pointSpacing)
        {
            AddPoint(flatCurrentPos);
        }
    }
    public void Move()
    {
        moveAmount = FindAction("MoveCharacter").ReadValue<Vector2>();

        if (moveAmount.magnitude < 0.5f)
        {
            CharacterControlAnimator.SetBool("isMoving", false);
            return;
        }

        if (ActionPoints <= 0)
        {
            CharacterControlAnimator.SetBool("isMoving", false);
            return;
        }

        Vector2 moveDir = moveAmount.normalized;
        Vector3 inputDirection = new Vector3(moveDir.x, 0, moveDir.y);

        // 角色轉向（保留你原本的 turnRate 邏輯）
        if (inputDirection.sqrMagnitude > 0f)
        {
            CharacterControlAnimator.SetBool("isMoving", true);
            Quaternion targetRotation = Quaternion.LookRotation(inputDirection, Vector3.up);
            float angleDiff = Quaternion.Angle(characterController.transform.rotation, targetRotation);

            float rotationStep = turnRate * Time.deltaTime;
            float t = Mathf.Min(1f, rotationStep / angleDiff);
            characterController.transform.rotation = Quaternion.Slerp(characterController.transform.rotation, targetRotation, t);
        }


        // 用 CharacterController 移動
        Vector3 moveVelocity = inputDirection * moveSpeed;
        characterController.Move(moveVelocity * Time.deltaTime); //  有考慮碰撞！
        
        // 計算移動距離（Z 軸）
        Vector2 nowPosition = new Vector2(characterController.transform.position.x, characterController.transform.position.z);
        CombatAction tempAction = new CombatAction();
        tempAction.Position = characterController.transform.position;
        tempAction.rotation = characterController.transform.rotation;
        tempAction.type = CombatAction.ActionType.Move;
        RecordedActions.Enqueue(tempAction);
        float distance = (lastPosition - nowPosition).magnitude;
        lastPosition = nowPosition;
        ActionPoints -= distance * 5.0f;

        // 更新 UI
        SLGCoreUI.Instance.apBar.slider.maxValue = MaxActionPoints;
        SLGCoreUI.Instance.apBar.slider.value = ActionPoints;
    }
    
    public InputAction FindAction(string id)
    {
        return controls.FindActionMap("CharacterControl").FindAction(id);
    }

    public bool CheckConfirm()
    {
        bool confirmPressed = FindAction("Comfirm").WasPressedThisFrame();
        if (confirmPressed)
        {
            Debug.Log("ConfirmPressed");
            return true;
        }

        return false;
    }
    
    public void ReFillAP()
    {
        ActionPoints = MaxActionPoints;
    }
    
    // Update is called once per frame
    void Update()
    {
        if (nowState == CharacterCoreState.ControlState)
        {
            ControlUpdate();

            if (CheckSkillA() || CheckSkillB() || CheckSkillC() || CheckSkillC())
            {
                CombatAction tempActionMove = new CombatAction();
                nowState = CharacterCoreState.UsingSkill;
                tempActionMove.Position = characterController.transform.position;
                tempActionMove.rotation = characterController.transform.rotation;
                tempActionMove.type = CombatAction.ActionType.Move;
                RecordedActions.Enqueue(tempActionMove);
                CombatAction tempActionSkill = new CombatAction();
                if (CheckSkillA())
                {
                    CharacterControlAnimator.Play("SkillA");
                    tempActionSkill.type = CombatAction.ActionType.SkillA;
                }
                else if (CheckSkillB())
                {
                    CharacterControlAnimator.Play("SkillB");
                    tempActionSkill.type = CombatAction.ActionType.SkillB;
                }
                else if (CheckSkillC())
                {
                    CharacterControlAnimator.Play("SkillC");
                    tempActionSkill.type = CombatAction.ActionType.SkillC;
                }
                else if (CheckSkillD())
                {
                    CharacterControlAnimator.Play("SkillD");
                    tempActionSkill.type = CombatAction.ActionType.SkillD;
                }
                RecordedActions.Enqueue(tempActionSkill);
            }

            else if (CheckConfirm())
            {
                CombatCore.Instance.ConfirmAction();
                nowState = CharacterCoreState.ExcutionState;
                return;
            }
        }
        else if (nowState == CharacterCoreState.ExcutionState)
        {
            ExecutorUpdate();
            ReFillAP();
        }
        else if (nowState == CharacterCoreState.ExecutingSkill)
        {
            AnimatorStateInfo stateInfo = CharacterExecuteAnimator.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.normalizedTime >= 1.0f)
            {
                characterControllerForExecutor.enabled = false;
                nowState = CharacterCoreState.ControlState;
            }
        }
        else if (nowState == CharacterCoreState.UsingSkill)
        {
            AnimatorStateInfo stateInfo = CharacterControlAnimator.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.normalizedTime >= 1.0f)
            {
                nowState = CharacterCoreState.ControlState;
            }
        }
    }
    
    public void ExecutorUpdate()
    {
        if (RecordedActions.Count != 0)
        {
            
            CombatAction tempAction = RecordedActions.Dequeue();
            if (tempAction.type == CombatAction.ActionType.Move)
            {
                Vector3 pos = tempAction.Position;
                Quaternion rot = tempAction.rotation;
                characterExecutor.transform.position = pos;
                characterExecutor.transform.rotation = rot;
            }
            else if (tempAction.type == CombatAction.ActionType.SkillA)
            {
                Debug.Log("SkillA");
                characterControllerForExecutor.enabled = true;
                CharacterExecuteAnimator.Play("SkillA");
                nowState = CharacterCoreState.ExecutingSkill;
            }
            else if (tempAction.type == CombatAction.ActionType.SkillB)
            {
                Debug.Log("SkillB");
                characterControllerForExecutor.enabled = true;
                CharacterExecuteAnimator.Play("SkillB");
                nowState = CharacterCoreState.ExecutingSkill;
            }
            else if (tempAction.type == CombatAction.ActionType.SkillC)
            {
                Debug.Log("SkillC");
                characterControllerForExecutor.enabled = true;
                CharacterExecuteAnimator.Play("SkillC");
                nowState = CharacterCoreState.ExecutingSkill;
            }
            else if (tempAction.type == CombatAction.ActionType.SkillD)
            {
                Debug.Log("SkillD");
                characterControllerForExecutor.enabled = true;
                CharacterExecuteAnimator.Play("SkillD");
                nowState = CharacterCoreState.ExecutingSkill;
            }
        }
        else
        {
            nowState = CharacterCore.CharacterCoreState.ControlState;
        }
    }

    public bool CheckSkillA()
    {
        bool confirmPressed = FindAction("SkillA").WasPressedThisFrame();
        return confirmPressed;
    }
    public bool CheckSkillB()
    {
        bool confirmPressed = FindAction("SkillB").WasPressedThisFrame();
        return confirmPressed;
    }
    public bool CheckSkillC()
    {
        bool confirmPressed = FindAction("SkillC").WasPressedThisFrame();
        return confirmPressed;
    }
    public bool CheckSkillD()
    {
        bool confirmPressed = FindAction("SkillD").WasPressedThisFrame();
        return confirmPressed;
    }



}