using UnityEngine;
using UnityEngine.AI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
#if UNITY_EDITOR
using UnityEditor;
#endif

public class EnemyAIDebugVisualizer : MonoBehaviour
{
    [Header("Debug Toggle")]
    [SerializeField] private KeyCode toggleKey = KeyCode.F2;
    [SerializeField] private bool drawGizmosWhenNotSelected = true;

    [Header("Sensor Gizmo")]
    [SerializeField] private bool drawDistanceLabels = true;
    [SerializeField] private bool drawNavigationGizmos = false;
    [SerializeField] [Range(12, 96)] private int ringSegments = 40;
    [SerializeField] [Range(6, 48)] private int sectorSegments = 18;
    [SerializeField] private float sensorEyeHeight = 1.5f;
    [SerializeField] private float targetAimHeight = 1.0f;

    private static bool s_IsDebugVisible = true;
    private EnemyBehaviorBridge bridge;
    private NavMeshAgent navMeshAgent;
    private GameObject targetObject;
    private Vector3 latestTargetPosition;
    private bool actionFeedCanSeeTarget;
    private float actionFeedDistanceToTarget;
    private bool actionFeedHasLastKnownPosition;
    private Vector3 actionFeedLastKnownPosition;
    private bool hasRecentActionSense;
    private bool actionFeedHasPath;
    private Vector3 actionFeedDestination;
    private float actionFeedSenseTimestamp;
    private int attackTriggerCount;
    private string monitoredBranch = "Unknown";
    private string sensedBranch = "Unknown";
    private string lastEventReason = "None";
    private string lastSenseReason = "None";

    private const float ActionFeedLifetime = 0.5f;

    private void Awake()
    {
        bridge = GetComponent<EnemyBehaviorBridge>();
        navMeshAgent = GetComponent<NavMeshAgent>();
    }

    private void Update()
    {
        if (IsTogglePressed())
        {
            s_IsDebugVisible = !s_IsDebugVisible;
        }
    }

    private bool IsTogglePressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current == null)
        {
            return false;
        }

        if (toggleKey == KeyCode.F2)
        {
            return Keyboard.current.f2Key.wasPressedThisFrame;
        }

        if (System.Enum.TryParse(toggleKey.ToString(), true, out Key parsedKey))
        {
            return Keyboard.current[parsedKey].wasPressedThisFrame;
        }

        return false;
#elif ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(toggleKey);
#else
        return false;
#endif
    }

    // SenseTargetAction 결과를 저장해 Scene Gizmo를 실제 SensorLoop 판정과 동기화한다.
    public void ReportSense(bool canSee, float distanceToTarget, bool hasKnownPosition, Vector3 knownPosition, Vector3 targetPosition)
    {
        actionFeedCanSeeTarget = canSee;
        actionFeedDistanceToTarget = distanceToTarget;
        actionFeedHasLastKnownPosition = hasKnownPosition;
        actionFeedLastKnownPosition = knownPosition;
        latestTargetPosition = targetPosition;
        actionFeedSenseTimestamp = Time.time;
        hasRecentActionSense = true;
    }

    public void ReportBranch(string branchName, string reason)
    {
        monitoredBranch = string.IsNullOrWhiteSpace(branchName) ? "Unknown" : branchName;
        lastEventReason = string.IsNullOrWhiteSpace(reason) ? "None" : reason;
    }

    public void ReportSenseHint(string branchName, string reason)
    {
        sensedBranch = string.IsNullOrWhiteSpace(branchName) ? "Unknown" : branchName;
        lastSenseReason = string.IsNullOrWhiteSpace(reason) ? "None" : reason;
    }

    public void ReportAgentState(bool isStopped, float speed, bool hasPath, Vector3 destination)
    {
        actionFeedHasPath = hasPath;
        actionFeedDestination = destination;
    }

    public void ReportAttackTriggered()
    {
        attackTriggerCount++;
        ReportBranch("Attack", "Animator Trigger fired");
    }

    public void ReportPatrol(int nextPatrolIndex, Vector3 selectedPoint)
    {
        ReportBranch("Patrol", $"Select point #{nextPatrolIndex}");
    }

    private void OnDrawGizmos()
    {
        if (!drawGizmosWhenNotSelected)
        {
            return;
        }

        DrawDebugGizmos();
    }

    private void OnDrawGizmosSelected()
    {
        DrawDebugGizmos();
    }

    private void DrawDebugGizmos()
    {
        if (!s_IsDebugVisible)
        {
            return;
        }

        EnemyBehaviorBridge currentBridge = bridge != null ? bridge : GetComponent<EnemyBehaviorBridge>();
        if (currentBridge == null || currentBridge.Config == null)
        {
            return;
        }

        if (targetObject == null)
        {
            try
            {
                targetObject = GameObject.FindGameObjectWithTag("Player");
            }
            catch (UnityException)
            {
                targetObject = null;
            }
        }

        Vector3 selectedTargetPosition = latestTargetPosition;
        if (selectedTargetPosition == Vector3.zero && targetObject != null)
        {
            selectedTargetPosition = targetObject.transform.position;
        }

        SenseSnapshot snapshot = BuildSenseSnapshot(currentBridge.Config, selectedTargetPosition);
        DrawSensorGizmos(snapshot, currentBridge.Config);

        if (drawNavigationGizmos)
        {
            DrawNavigationDebug();
        }
    }

    // SenseTargetAction과 동일한 계산 순서(거리 -> 평면 FOV -> LOS)를 사용해 표시 오차를 줄인다.
    private SenseSnapshot BuildSenseSnapshot(EnemyConfigSO config, Vector3 targetPosition)
    {
        SenseSnapshot snapshot = new SenseSnapshot
        {
            hasTarget = targetPosition != Vector3.zero,
            targetPosition = targetPosition,
            detectRadius = config.detectRadius,
            attackRange = config.attackRange,
            viewAngle = config.viewAngle
        };

        if (!snapshot.hasTarget)
        {
            return snapshot;
        }

        Vector3 selfPosition = transform.position;
        Vector3 toTarget = snapshot.targetPosition - selfPosition;
        snapshot.distance = toTarget.magnitude;
        snapshot.inRadius = snapshot.distance <= config.detectRadius;

        if (toTarget.sqrMagnitude > 0.0001f)
        {
            Vector3 flatDirection = toTarget;
            flatDirection.y = 0f;
            if (flatDirection.sqrMagnitude > 0.0001f)
            {
                snapshot.viewAngleDelta = Vector3.Angle(transform.forward, flatDirection.normalized);
                snapshot.inFov = snapshot.viewAngleDelta <= config.viewAngle * 0.5f;
            }
        }

        snapshot.eyeOrigin = selfPosition + Vector3.up * sensorEyeHeight;
        snapshot.aimPoint = snapshot.targetPosition + Vector3.up * targetAimHeight;
        Vector3 rayDirection = (snapshot.aimPoint - snapshot.eyeOrigin).normalized;
        snapshot.hasLineDirection = rayDirection.sqrMagnitude > 0.0001f;

        if (snapshot.hasLineDirection)
        {
            snapshot.blocked = Physics.Raycast(
                snapshot.eyeOrigin,
                rayDirection,
                out snapshot.blockingHit,
                snapshot.distance,
                config.obstacleLayer);
        }

        bool feedIsFresh = hasRecentActionSense && (Time.time - actionFeedSenseTimestamp) <= ActionFeedLifetime;
        snapshot.visible = feedIsFresh ? actionFeedCanSeeTarget : snapshot.inRadius && snapshot.inFov && !snapshot.blocked;
        snapshot.distance = feedIsFresh ? actionFeedDistanceToTarget : snapshot.distance;

        return snapshot;
    }

    private void DrawSensorGizmos(SenseSnapshot snapshot, EnemyConfigSO config)
    {
        DrawGroundRing(transform.position, snapshot.detectRadius, new Color(1f, 0.92f, 0.16f, 0.9f));
        DrawGroundRing(transform.position, snapshot.attackRange, new Color(1f, 0.2f, 0.2f, 0.85f));

        Color sectorColor = snapshot.visible
            ? new Color(0.1f, 0.9f, 0.2f, 0.9f)
            : snapshot.inRadius ? new Color(1f, 0.2f, 0.2f, 0.9f) : new Color(0.5f, 0.5f, 0.5f, 0.8f);
        DrawFovSector(snapshot.eyeOrigin, snapshot.detectRadius, snapshot.viewAngle, sectorColor);

        if (!snapshot.hasTarget)
        {
            return;
        }

        Color lineColor = snapshot.visible ? Color.green : Color.red;
        Vector3 rayEnd = snapshot.blocked ? snapshot.blockingHit.point : snapshot.aimPoint;
        Gizmos.color = lineColor;
        Gizmos.DrawLine(snapshot.eyeOrigin, rayEnd);
        DrawCross(snapshot.aimPoint, 0.18f, lineColor);

        if (actionFeedHasLastKnownPosition)
        {
            Gizmos.color = new Color(1f, 0.55f, 0f, 1f);
            Gizmos.DrawSphere(actionFeedLastKnownPosition, 0.14f);
            DrawCross(actionFeedLastKnownPosition + Vector3.up * 0.04f, 0.16f, new Color(1f, 0.55f, 0f, 1f));
        }

#if UNITY_EDITOR
        if (drawDistanceLabels)
        {
            Vector3 labelPosition = (snapshot.eyeOrigin + snapshot.aimPoint) * 0.5f + Vector3.up * 0.2f;
            string reason = snapshot.visible
                ? "Visible"
                : snapshot.blocked ? "LOS Blocked" : snapshot.inRadius ? "Out Of FOV" : "Out Of Radius";
            string label = $"Sense {snapshot.distance:F2}m / {snapshot.detectRadius:F2}m | {reason}";
            Handles.Label(labelPosition, label);
            Handles.Label(snapshot.eyeOrigin + Vector3.up * 0.18f, $"AttackCount:{attackTriggerCount} Branch:{monitoredBranch}");
            Handles.Label(snapshot.eyeOrigin + Vector3.up * 0.36f, $"Hint:{sensedBranch} Event:{lastEventReason} Sense:{lastSenseReason}");
        }
#endif
    }

    private void DrawNavigationDebug()
    {
        if (actionFeedHasPath)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(actionFeedDestination, 0.1f);
            Gizmos.DrawLine(transform.position, actionFeedDestination);
        }

        NavMeshAgent currentAgent = navMeshAgent != null ? navMeshAgent : GetComponent<NavMeshAgent>();
        if (currentAgent == null || !currentAgent.hasPath)
        {
            return;
        }

        Vector3[] corners = currentAgent.path.corners;
        Gizmos.color = Color.magenta;
        for (int i = 0; i < corners.Length - 1; i++)
        {
            Gizmos.DrawLine(corners[i], corners[i + 1]);
        }
    }

    private void DrawGroundRing(Vector3 center, float radius, Color color)
    {
        if (radius <= 0f)
        {
            return;
        }

        Gizmos.color = color;
        int segments = Mathf.Max(12, ringSegments);
        Vector3 previousPoint = center + new Vector3(radius, 0f, 0f);
        for (int segmentIndex = 1; segmentIndex <= segments; segmentIndex++)
        {
            float angle = (segmentIndex / (float)segments) * Mathf.PI * 2f;
            Vector3 currentPoint = center + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
            Gizmos.DrawLine(previousPoint, currentPoint);
            previousPoint = currentPoint;
        }
    }

    private void DrawFovSector(Vector3 eyePosition, float radius, float viewAngle, Color color)
    {
        if (radius <= 0f)
        {
            return;
        }

        Gizmos.color = color;
        int segments = Mathf.Max(6, sectorSegments);
        float halfAngle = viewAngle * 0.5f;
        Vector3 previousDirection = Quaternion.Euler(0f, -halfAngle, 0f) * transform.forward;
        Vector3 previousPoint = eyePosition + previousDirection * radius;
        Gizmos.DrawLine(eyePosition, previousPoint);

        for (int segmentIndex = 1; segmentIndex <= segments; segmentIndex++)
        {
            float t = segmentIndex / (float)segments;
            float currentAngle = Mathf.Lerp(-halfAngle, halfAngle, t);
            Vector3 currentDirection = Quaternion.Euler(0f, currentAngle, 0f) * transform.forward;
            Vector3 currentPoint = eyePosition + currentDirection * radius;
            Gizmos.DrawLine(previousPoint, currentPoint);
            Gizmos.DrawLine(eyePosition, currentPoint);
            previousPoint = currentPoint;
        }
    }

    private void DrawCross(Vector3 point, float size, Color color)
    {
        Gizmos.color = color;
        Gizmos.DrawLine(point + Vector3.left * size, point + Vector3.right * size);
        Gizmos.DrawLine(point + Vector3.forward * size, point + Vector3.back * size);
        Gizmos.DrawLine(point + Vector3.up * size, point + Vector3.down * size);
    }

    private struct SenseSnapshot
    {
        public bool hasTarget;
        public bool visible;
        public bool inRadius;
        public bool inFov;
        public bool blocked;
        public bool hasLineDirection;
        public float distance;
        public float detectRadius;
        public float attackRange;
        public float viewAngle;
        public float viewAngleDelta;
        public Vector3 targetPosition;
        public Vector3 eyeOrigin;
        public Vector3 aimPoint;
        public RaycastHit blockingHit;
    }
}
