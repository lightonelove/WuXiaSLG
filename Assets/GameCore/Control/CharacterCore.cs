using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class CharacterCore : MonoBehaviour
{
    // For Control //
    public float moveSpeed = 5f;
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
    
    public Queue<Vector3> RecordedPositions = new Queue<Vector3>();
    public Queue<Quaternion> RecordedRotaitons = new Queue<Quaternion>();
    
    public CharacterController characterController;
    
    public Animator CharacterControlAnimator;
    public Animator CharacterExecuteAnimator;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public InputActionAsset controls;
    public GameObject characterExecutor;

    public enum CharacterCoreState{ ControlState, ExcutionState, UsingSkill }

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
        RecordedPositions.Enqueue(characterController.transform.position);
        RecordedRotaitons.Enqueue(characterController.transform.rotation);
        float distance = (lastPosition - nowPosition).magnitude;
        Debug.Log("??????" + distance);
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

            if (CheckSkillA())
            {
                nowState = CharacterCoreState.UsingSkill;
                CharacterControlAnimator.Play("SkillA");
            }
            else if (CheckSkillB())
            {
                nowState = CharacterCoreState.UsingSkill;
                CharacterControlAnimator.Play("SkillB");
            }
            else if (CheckSkillC())
            {
                nowState = CharacterCoreState.UsingSkill;
                CharacterControlAnimator.Play("SkillC");
            }
            else if (CheckSkillD())
            {
                nowState = CharacterCoreState.UsingSkill;
                CharacterControlAnimator.Play("SkillD");
            }
            else if (CheckConfirm())
            {
                nowState = CharacterCoreState.ExcutionState;
                return;
            }
        }
        else if (nowState == CharacterCoreState.ExcutionState)
        {
            ExecutorUpdate();
            ReFillAP();
        }
        else if (nowState == CharacterCoreState.UsingSkill)
        {
            AnimatorStateInfo stateInfo = CharacterControlAnimator.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.normalizedTime >= 1.0f)
            {
                Debug.Log("動畫播放完畢，執行事件");
                nowState = CharacterCoreState.ControlState;
            }

        }
    }
    
    public void ExecutorUpdate()
    {
        if (RecordedPositions.Count != 0)
        {
            Vector3 pos = RecordedPositions.Dequeue();
            Quaternion rot = RecordedRotaitons.Dequeue();
            characterExecutor.transform.position = pos;
            characterExecutor.transform.rotation = rot;
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