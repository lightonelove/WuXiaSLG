using Sirenix.OdinInspector;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

public class NavMeshRuntimeBaker : MonoBehaviour
{
    public NavMeshSurface surface;
    
    [Button]
    public void Bake()
    {
        surface.BuildNavMesh(); // 這會在 Runtime 烘焙 NavMesh
    }
}