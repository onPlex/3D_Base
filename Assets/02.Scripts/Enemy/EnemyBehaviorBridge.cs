using UnityEngine;
public class EnemyBehaviorBridge : MonoBehaviour
{
    [SerializeField] private EnemyConfigSO config;
    [SerializeField] private Transform[] patrolPoints;

    public EnemyConfigSO Config => config;
    public Transform[] PatrolPoints => patrolPoints;
    public bool HasConfig => config != null;
    public bool HasPatrolPoints => patrolPoints != null && patrolPoints.Length > 0;

    private void Awake()
    {
        TryPopulatePatrolPointsFromScene();
    }

    private void TryPopulatePatrolPointsFromScene()
    {
        if (HasPatrolPoints)
        {
            return;
        }

        // 수동 할당이 비어 있으면 scene의 PatrolPoint 루트를 기준으로 자동 수집한다.
        GameObject patrolRootObject = GameObject.Find("PatrolPoint");
        if (patrolRootObject == null)
        {
            return;
        }

        Transform patrolRoot = patrolRootObject.transform;
        if (patrolRoot.childCount <= 0)
        {
            return;
        }

        Transform[] discoveredPoints = new Transform[patrolRoot.childCount];
        for (int childIndex = 0; childIndex < patrolRoot.childCount; childIndex++)
        {
            discoveredPoints[childIndex] = patrolRoot.GetChild(childIndex);
        }

        patrolPoints = discoveredPoints;
    }

    public Vector3 GetPatrolPosition(int index)
    {
        if (!HasPatrolPoints)
            return transform.position;

        int safeIndex = Mathf.Abs(index) % patrolPoints.Length;
        Transform point = patrolPoints[safeIndex];
        return point != null ? point.position : transform.position;
    }
}