using UnityEngine;

public class AnimationRelativePos : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public Animator animator;
    public CharacterController characterController;
    void Start()
    {
        
    }

    void OnAnimatorMove()
    {
        Vector3 deltaPosition = animator.deltaPosition;
        
        if (characterController != null)
        {
            characterController.Move(deltaPosition);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
