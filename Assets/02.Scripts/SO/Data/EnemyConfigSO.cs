using UnityEngine;

[CreateAssetMenu(menuName = "AI/Enemy Config")]
public class EnemyConfigSO : ScriptableObject
{
    [Header("Movement")]
    public float patrolSpeed = 1.8f;
    public float chaseSpeed = 3.8f;
    public float rotationSpeed = 720f;

    [Header("Detection")]
    public float detectRadius = 10f;
    public float viewAngle = 120f;
    public LayerMask targetLayer;
    public LayerMask obstacleLayer;

    [Header("Combat")]
    public float attackRange = 2.1f;
    public float attackCooldown = 1.2f;
    public float attackStopDistanceRate = 0.85f;

    [Header("Patrol")]
    public float waitAtPatrolPoint = 1.0f;
}

