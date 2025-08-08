using UnityEngine;
using System.Collections.Generic;

public class AnimationRelativePos : MonoBehaviour
{
    [Header("動畫根運動設定")]
    public Animator animator;
    
    void Start()
    {
    }

    void OnAnimatorMove()
    {
        if (animator == null) return;
            
        Vector3 deltaPosition = animator.deltaPosition;
        Quaternion deltaRotation = animator.deltaRotation;
        
        // 直接操作Transform應用根運動位移
        transform.position += deltaPosition;
        
        // 應用旋轉
        transform.rotation = deltaRotation * transform.rotation;
    }
    
    
    
    
    
    
    
    
    
    

    
}
