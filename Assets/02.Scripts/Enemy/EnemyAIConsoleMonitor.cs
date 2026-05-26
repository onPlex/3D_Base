using System.Collections.Generic;
using Unity.Behavior;
using UnityEngine;
using UnityEngine.AI;

public class EnemyAIConsoleMonitor : MonoBehaviour
{
    [Header("Console Monitor")]
    [SerializeField] private bool enableLogs = true;
    [SerializeField] private bool verboseLogs = true;
    [SerializeField] private float throttleSeconds = 0.3f;
    [SerializeField] private float heartbeatInterval = 1.0f;

    private readonly Dictionary<string, float> lastLogTimeByKey = new Dictionary<string, float>();
    private float lastHeartbeatTime;
    private string lastBranch = "Unknown";
    private string lastReason = "None";

    private EnemyBehaviorBridge bridge;
    private NavMeshAgent navMeshAgent;
    private Animator animator;
    private BehaviorGraphAgent graphAgent;
    private EnemyAIDebugVisualizer visualizer;

    private void Awake()
    {
        bridge = GetComponent<EnemyBehaviorBridge>();
        navMeshAgent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        graphAgent = GetComponent<BehaviorGraphAgent>();
        visualizer = GetComponent<EnemyAIDebugVisualizer>();

        if (!enableLogs)
        {
            return;
        }

        // 초기 구성 누락을 바로 확인할 수 있도록 시작 시점 진단 로그를 출력한다.
        if (graphAgent == null)
            Debug.LogError($"[EnemyAI][{name}] BehaviorGraphAgent missing.");
        if (bridge == null)
            Debug.LogError($"[EnemyAI][{name}] EnemyBehaviorBridge missing.");
        if (navMeshAgent == null)
            Debug.LogError($"[EnemyAI][{name}] NavMeshAgent missing.");
        if (animator == null)
            Debug.LogError($"[EnemyAI][{name}] Animator missing.");
    }

    private void Start()
    {
        if (!enableLogs)
        {
            return;
        }

        int patrolCount = bridge != null && bridge.PatrolPoints != null ? bridge.PatrolPoints.Length : 0;
        bool hasConfig = bridge != null && bridge.HasConfig;
        Debug.Log($"[EnemyAI][{name}] Monitor start | graph={(graphAgent != null)} config={hasConfig} patrolCount={patrolCount}");
    }

    private void Update()
    {
        if (!enableLogs)
        {
            return;
        }

        if (Time.time - lastHeartbeatTime < heartbeatInterval)
        {
            return;
        }

        lastHeartbeatTime = Time.time;
        string branchText = lastBranch;
        string reasonText = lastReason;
        float speed = navMeshAgent != null ? navMeshAgent.speed : -1f;
        bool stopped = navMeshAgent != null && navMeshAgent.isStopped;
        bool hasPath = navMeshAgent != null && navMeshAgent.hasPath;
        Debug.Log($"[EnemyAI][{name}] Heartbeat | branch={branchText} reason={reasonText} speed={speed:F2} stopped={stopped} hasPath={hasPath}");
    }

    public void ReportAction(string actionName, string detail, bool isImportant = false)
    {
        if (!enableLogs)
        {
            return;
        }

        if (!isImportant && !verboseLogs)
        {
            return;
        }

        string key = $"{actionName}:{detail}";
        if (!ShouldLog(key, isImportant))
        {
            return;
        }

        Debug.Log($"[EnemyAI][{name}] {actionName} | {detail}");
    }

    public void ReportFailure(string actionName, string reason)
    {
        if (!enableLogs)
        {
            return;
        }

        string key = $"FAIL:{actionName}:{reason}";
        if (!ShouldLog(key, true))
        {
            return;
        }

        Debug.LogWarning($"[EnemyAI][{name}] {actionName} FAILED | {reason}");
    }

    public void ReportBranch(string branch, string reason)
    {
        if (!enableLogs)
        {
            return;
        }

        lastBranch = branch;
        lastReason = reason;
        if (visualizer != null)
        {
            visualizer.ReportBranch(branch, reason);
        }

        string key = $"BRANCH:{branch}:{reason}";
        if (!ShouldLog(key, true))
        {
            return;
        }

        Debug.Log($"[EnemyAI][{name}] Branch -> {branch} | {reason}");
    }

    // Sense 판정 결과와 실제 실행 branch 로그를 분리해 디버깅 혼선을 줄인다.
    public void ReportSenseEvaluation(bool visible, float distance, bool hasLastKnownPosition, string suggestedBranch)
    {
        if (!enableLogs)
        {
            return;
        }

        string detail = $"visible={visible} distance={distance:F2} hasLastKnown={hasLastKnownPosition} suggested={suggestedBranch}";
        string key = $"SENSE:{detail}";
        if (!ShouldLog(key, false))
        {
            return;
        }

        Debug.Log($"[EnemyAI][{name}] SenseEval | {detail}");
    }

    // Sense 실패 원인을 분해해 남겨, 왜 Attack/Chase 분기로 못 들어가는지 즉시 추적할 수 있게 한다.
    public void ReportSenseBreakdown(
        bool visible,
        float planarDistance,
        float detectRadius,
        float attackRange,
        bool inRadius,
        bool inFov,
        bool blocked,
        bool hasLastKnownPosition,
        string suggestedBranch)
    {
        if (!enableLogs || !verboseLogs)
        {
            return;
        }

        string reason;
        if (visible)
        {
            reason = planarDistance <= attackRange ? "attack-window" : "chase-window";
        }
        else if (!inRadius)
        {
            reason = "out-of-radius";
        }
        else if (!inFov)
        {
            reason = "out-of-fov";
        }
        else if (blocked)
        {
            reason = "los-blocked";
        }
        else
        {
            reason = "unknown";
        }

        string detail =
            $"visible={visible} planar={planarDistance:F2} detect={detectRadius:F2} attack={attackRange:F2} " +
            $"inRadius={inRadius} inFov={inFov} blocked={blocked} hasLastKnown={hasLastKnownPosition} " +
            $"suggested={suggestedBranch} reason={reason}";

        string key = $"SENSE_BREAKDOWN:{detail}";
        if (!ShouldLog(key, false))
        {
            return;
        }

        Debug.Log($"[EnemyAI][{name}] SenseBreakdown | {detail}");
    }

    private bool ShouldLog(string key, bool isImportant)
    {
        if (isImportant)
        {
            return true;
        }

        if (!lastLogTimeByKey.TryGetValue(key, out float lastTime))
        {
            lastLogTimeByKey[key] = Time.time;
            return true;
        }

        if (Time.time - lastTime < throttleSeconds)
        {
            return false;
        }

        lastLogTimeByKey[key] = Time.time;
        return true;
    }
}
