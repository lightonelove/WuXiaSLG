using UnityEngine;
using System.Collections.Generic;


public class CharacterExcutor : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public Queue<Vector3> RecordedPosition = new Queue<Vector3>();
    public Queue<Quaternion> RecordedRotation = new Queue<Quaternion>();
    public CharacterCore characterCore;
    public CharacterControl characterControl;
    void Start()
    {
        
    }

    // Update is called once per frame
    public void ExecutorUpdate()
    {
        if (RecordedPosition.Count != 0)
        {
            Vector3 pos = RecordedPosition.Dequeue();
            Quaternion rot = RecordedRotation.Dequeue();
            transform.position = pos;
            transform.rotation = rot;
        }
        else
        {
            characterCore.nowState = CharacterCore.CharacterCoreState.ControlState;
            
        }
    }
}