using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class CharacterControl : MonoBehaviour
{
    public float moveSpeed = 5f;
    public Transform cameraTransform;

    public InputActionAsset controls;
    public float turnRate = 720;
    
    private Vector2 moveAmount;

    public float ActionPoints = 100;
    public float MaxActionPoints = 100;
    public Vector2 lastPosition;
    
    public float pointSpacing = 0.2f; // 每隔幾公尺新增一點
    private LineRenderer line;
    private List<Vector3> points = new List<Vector3>();
    private Vector3 lastDrawPoint;

    public CharacterController characterController;

    public Queue<Vector3> RecordedPositions = new Queue<Vector3>();
    public Queue<Quaternion> RecordedRotaitons = new Queue<Quaternion>();
    
    void Start()
    {
        line = GetComponent<LineRenderer>();
        line.positionCount = 0;
        AddPoint();
    }

    // Update is called once per frame
    public void ControlUpdate()
    {
        Move();
        CheckConfirm();
        lastPosition = new Vector2(transform.position.x, transform.position.z);
        
        Vector3 flatCurrentPos = new Vector3(transform.position.x, 0.1f, transform.position.z);

        if (Vector3.Distance(flatCurrentPos, lastDrawPoint) >= pointSpacing)
        {
            AddPoint(flatCurrentPos);
        }
    }

    public void CheckConfirm()
    {
        
        bool confirmPressed = controls.FindActionMap("GeneralControl").FindAction("Confirm").WasPressedThisFrame();
        if (confirmPressed)
        {
            Debug.Log("ConfirmPressed");
        }
    }
    public InputAction FindAction(string id)
    {
       return controls.FindActionMap("CharacterControl").FindAction(id);
    }

    public void Move()
    {
        moveAmount = FindAction("MoveCharacter").ReadValue<Vector2>();

        if (moveAmount.magnitude < 0.5f)
            return;

        if (ActionPoints <= 0)
            return;

        Vector2 moveDir = moveAmount.normalized;
        Vector3 inputDirection = new Vector3(moveDir.x, 0, moveDir.y);

        // 角色轉向（保留你原本的 turnRate 邏輯）
        if (inputDirection.sqrMagnitude > 0f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(inputDirection, Vector3.up);
            float angleDiff = Quaternion.Angle(transform.rotation, targetRotation);

            float rotationStep = turnRate * Time.deltaTime;
            float t = Mathf.Min(1f, rotationStep / angleDiff);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, t);
        }

        // 用 CharacterController 移動
        Vector3 moveVelocity = inputDirection * moveSpeed;
        characterController.Move(moveVelocity * Time.deltaTime); //  有考慮碰撞！
        
        // 計算移動距離（Z 軸）
        Vector2 nowPosition = new Vector2(transform.position.x, transform.position.z);
        RecordedPositions.Enqueue(transform.position);
        RecordedRotaitons.Enqueue(transform.rotation);
        float distance = (lastPosition - nowPosition).magnitude;
        
        lastPosition = nowPosition;
        ActionPoints -= distance * 5.0f;

        // 更新 UI
        SLGCoreUI.Instance.apBar.slider.maxValue = MaxActionPoints;
        SLGCoreUI.Instance.apBar.slider.value = ActionPoints;
    }

    void AddPoint(Vector3 flatPos)
    {
        points.Add(flatPos);
        line.positionCount = points.Count;
        line.SetPositions(points.ToArray());
        lastDrawPoint = flatPos;
    }

    public void ReFillAP()
    {
        ActionPoints = MaxActionPoints;
    }

    void AddPoint() => AddPoint(new Vector3(transform.position.x, 0.1f, transform.position.z));
}
