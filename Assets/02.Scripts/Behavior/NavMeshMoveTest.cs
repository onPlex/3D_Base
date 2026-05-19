using UnityEngine;
using UnityEngine.AI;

public class NavMeshMoveTest : MonoBehaviour
{
    [SerializeField] private Transform target;
    private NavMeshAgent agent;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    private void Update()
    {
        if (target != null)
            agent.SetDestination(target.position);
    }
}

