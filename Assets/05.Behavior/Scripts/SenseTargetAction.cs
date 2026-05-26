using System;
using Unity.Behavior;
using UnityEngine;
using UnityEngine.AI;
using Action = Unity.Behavior.Action;
using Unity.Properties;

// [Serializable, GeneratePropertyBag]
// 커스텀 노드가 Unity 에디터에서 직렬화(저장)되고, Inspector 및 Graph UI에 정상적으로 노출되도록 하는 필수 속성입니다.
[Serializable, GeneratePropertyBag]
// [NodeDescription]
// 이 노드가 Behavior Graph 에디터에서 어떻게 보일지 정의합니다.
// 스토리(story)에 여러 변수들을 나열하여, 노드 UI에서 각 변수들을 한눈에 보고 연결할 수 있게 만듭니다.
[NodeDescription(name: "Sense Target",
    story: "[Self] senses [Target] and updates",
    category: "Action/AI", id: "24cb70622f80a7812633f21cbf35c323")]
public partial class SenseTargetAction : Action
{
    // Player tag 자동 탐색에 사용할 상수입니다.
    private const string PlayerTag = "Player";
    private const string AttackTriggerParameter = "Trigger";
    private const float DefaultInvestigateArrivalTolerance = 1.2f;
    private const float DefaultInvestigateTimeoutSeconds = 8.0f;

    // 그래프 분기가 꼬일 때도 공격이 완전히 사라지지 않도록 최소 fallback 공격 쿨다운을 유지합니다.
    private float nextFallbackAttackTime;
    // 마지막 목격 이후 Investigate가 무한 지속되지 않도록 시간 기반 종료 조건을 관리합니다.
    private float investigateStartedTime = -1f;
    private bool wasInvestigatingWithLastKnown;
    // Graph 전이가 실패해도 Patrol 이동이 재개되도록 최소 fallback 순찰 상태를 보관합니다.
    private int fallbackPatrolIndex;
    private float nextFallbackPatrolDecisionTime = -1f;

    // [SerializeReference] 및 BlackboardVariable<T>
    // Behavior Tree의 Blackboard와 통신할 변수들입니다.
    // 이 노드는 'Self'와 'Target'이라는 입력(Input)을 받아서, 
    // 거리, 시야 확보 여부, 마지막 위치 등의 결과(Output)를 Blackboard에 기록하는 역할을 합니다.
    [SerializeReference] public BlackboardVariable<GameObject> Self;
    [SerializeReference] public BlackboardVariable<GameObject> Target;
    [SerializeReference] public BlackboardVariable<bool> CanSeeTarget;
    [SerializeReference] public BlackboardVariable<float> DistanceToTarget;
    [SerializeReference] public BlackboardVariable<Vector3> LastKnownPosition;
    [SerializeReference] public BlackboardVariable<bool> HasLastKnownPosition;
    [SerializeReference] public BlackboardVariable<Vector3> TargetPosition;

    // OnUpdate()
    // Behavior Tree가 이 노드를 실행할 때마다 호출되는 함수입니다.
    protected override Status OnUpdate()
    {
        EnemyAIDebugVisualizer debugVisualizer = Self?.Value != null
            ? Self.Value.GetComponent<EnemyAIDebugVisualizer>()
            : null;
        EnemyAIConsoleMonitor consoleMonitor = Self?.Value != null
            ? Self.Value.GetComponent<EnemyAIConsoleMonitor>()
            : null;

        // Target이 비어 있으면 Player tag 오브젝트를 자동 탐색해 바인딩합니다.
        // Prefab에서 Target을 고정 참조하지 않아도 기본 동작이 가능하도록 합니다.
        if (Target != null && Target.Value == null)
        {
            // Tag가 아직 설정되지 않은 프로젝트에서도 예외로 AI 전체가 멈추지 않도록 보호합니다.
            try
            {
                GameObject playerObject = GameObject.FindGameObjectWithTag(PlayerTag);
                if (playerObject != null)
                    Target.Value = playerObject;
            }
            catch (UnityException)
            {
                // Player tag 미설정 시 다음 틱에서 다시 시도합니다.
            }
        }

        // 1. 유효성 검사 (안전 장치)
        // 나와 타겟 중 하나라도 존재하지 않는다면 감지 로직을 수행할 수 없습니다.
        if (Self?.Value == null || Target?.Value == null)
        {
            // 타겟이 없으므로 안 보인다고 처리하고 거리를 임의의 큰 값(9999f)으로 설정합니다.
            CanSeeTarget.Value = false;
            DistanceToTarget.Value = 9999f;
            if (debugVisualizer != null)
            {
                debugVisualizer.ReportSense(false, 9999f, HasLastKnownPosition.Value, LastKnownPosition.Value, Vector3.zero);
                debugVisualizer.ReportSenseHint(HasLastKnownPosition.Value ? "Investigate" : "Patrol", "Sense failed: target missing");
            }
            if (consoleMonitor != null)
            {
                consoleMonitor.ReportFailure("SenseTargetAction", "Self or Target is null");
                consoleMonitor.ReportSenseEvaluation(false, 9999f, HasLastKnownPosition.Value, HasLastKnownPosition.Value ? "Investigate" : "Patrol");
            }
            // Status.Failure를 반환하면 트리의 흐름이 끊길 수 있으므로, 
            // "감지 시도 자체는 정상적으로 완료되었으나 안 보인다"는 의미로 Success를 반환합니다.
            return Status.Success;
        }

        Transform self = Self.Value.transform;
        Transform target = Target.Value.transform;

        // 2. 적 설정 데이터 가져오기
        // EnemyBehaviorBridge를 통해 기획 데이터(Config)를 가져옵니다.
        EnemyBehaviorBridge bridge = Self.Value.GetComponent<EnemyBehaviorBridge>();
        EnemyConfigSO config = bridge != null ? bridge.Config : null;

        // 삼항 연산자(?:)를 사용하여 Config가 없을 경우 기본값을 할당합니다. (에러 방지)
        float detectRadius = config != null ? config.detectRadius : 10f; // 기본 감지 반경 10
        float viewAngle = config != null ? config.viewAngle : 120f;      // 기본 시야각 120도
        LayerMask obstacleLayer = config != null ? config.obstacleLayer : ~0; // 기본적으로 모든 레이어를 장애물로 취급(~0)

        // 3. 타겟과의 거리 및 방향 계산
        Vector3 toTarget = target.position - self.position;
        Vector3 flatDirection = toTarget;
        flatDirection.y = 0f;
        float planarDistance = flatDirection.magnitude;
        
        // 계산된 거리와 타겟의 현재 위치를 Blackboard에 즉시 업데이트합니다.
        DistanceToTarget.Value = planarDistance;
        TargetPosition.Value = target.position;

        bool visible = false; // 최종적으로 타겟이 보이는지 여부를 저장할 변수
        bool inRadius = false;
        bool inFov = false;
        bool blocked = false;

        // 4. 시야 감지 로직 (거리 -> 각도 -> 장애물 순서로 검사)
        // 4-1. 거리 검사: 감지 반경(detectRadius) 안에 들어왔는가?
        if (planarDistance <= detectRadius)
        {
            inRadius = true;
            // 4-2. 시야각 검사: 타겟이 내 앞을 기준으로 시야각(viewAngle) 안에 있는가?
            // 높낮이 차이 때문에 각도 계산이 왜곡되는 것을 막기 위해 Y축을 0으로 평탄화한 방향을 사용한다.
            
            // 내 정면(self.forward)과 타겟을 향하는 방향 사이의 각도를 구합니다.
            float angle = Vector3.Angle(self.forward, flatDirection.normalized);
            
            // 시야각(viewAngle)의 절반(0.5f)보다 작아야 내 시야 영역 안에 있는 것입니다.
            // (예: 시야각이 120도라면 좌측 60도, 우측 60도 이내여야 함)
            if (angle <= viewAngle * 0.5f)
            {
                inFov = true;
                // 4-3. 장애물 검사 (Line of Sight - 시야 확보): 사이에 벽이 없는가?
                // 발끝(바닥)에서 쏘면 바닥에 걸릴 수 있으므로, 눈높이(위로 1.5f)에서 타겟의 가슴 높이(위로 1.0f)로 레이(광선)를 쏩니다.
                Vector3 origin = self.position + Vector3.up * 1.5f;
                Vector3 dir = (target.position + Vector3.up * 1.0f - origin).normalized;
                float rayDistance = Vector3.Distance(origin, target.position + Vector3.up * 1.0f);
                
                // Physics.Raycast로 광선을 쏴서 장애물(obstacleLayer)에 부딪혔는지 확인합니다.
                blocked = Physics.Raycast(origin, dir, rayDistance, obstacleLayer);
                
                // 막히지 않았다면(!blocked) 최종적으로 눈에 보인다는 의미입니다.
                visible = !blocked;
            }
        }

        // 5. 시야 검사 결과를 Blackboard에 동기화
        CanSeeTarget.Value = visible;
        
        // 만약 타겟이 보인다면, '마지막으로 목격한 위치'를 현재 위치로 갱신해줍니다.
        // 타겟이 시야에서 사라지더라도 AI가 이 위치로 수색하러 갈 수 있도록 하기 위함입니다.
        if (visible)
        {
            LastKnownPosition.Value = target.position;
            HasLastKnownPosition.Value = true;
            investigateStartedTime = -1f;
            wasInvestigatingWithLastKnown = false;
        }

        // 그래프 분기 상태가 일시적으로 꼬여도 추적 동작이 끊기지 않도록 최소한의 추적 보정 로직을 적용합니다.
        if (visible)
        {
            NavMeshAgent agent = Self.Value.GetComponent<NavMeshAgent>();
            if (agent != null && agent.isOnNavMesh)
            {
                float attackRange = config != null ? config.attackRange : 2f;
                float chaseSpeed = config != null ? config.chaseSpeed : agent.speed;

                if (planarDistance > attackRange)
                {
                    agent.isStopped = false;
                    agent.speed = chaseSpeed;
                    agent.SetDestination(target.position);

                    if (consoleMonitor != null)
                    {
                        consoleMonitor.ReportAction(
                            "SenseTargetAction",
                            $"fallback chase | speed={agent.speed:F2} dest={target.position}",
                            false);
                    }
                }
                else
                {
                    // Attack 분기가 실패해도 완전 무반응이 되지 않도록 최소 fallback 공격을 트리거한다.
                    if (Time.time >= nextFallbackAttackTime)
                    {
                        EnemyRpgAnimatorDriver animatorDriver = Self.Value.GetComponent<EnemyRpgAnimatorDriver>();
                        if (animatorDriver != null)
                        {
                            animatorDriver.PrepareAttackParameters();
                        }

                        Animator selfAnimator = Self.Value.GetComponent<Animator>();
                        if (selfAnimator != null)
                        {
                            selfAnimator.SetTrigger(AttackTriggerParameter);
                            float fallbackCooldown = config != null ? Mathf.Max(0.2f, config.attackCooldown) : 0.8f;
                            nextFallbackAttackTime = Time.time + fallbackCooldown;

                            if (debugVisualizer != null)
                            {
                                debugVisualizer.ReportBranch("Attack", "Sense fallback attack trigger");
                            }
                            if (consoleMonitor != null)
                            {
                                consoleMonitor.ReportAction(
                                    "SenseTargetAction",
                                    $"fallback attack trigger | planar={planarDistance:F2} cooldown={fallbackCooldown:F2}",
                                    true);
                                consoleMonitor.ReportBranch("Attack", "Sense fallback attack trigger");
                            }
                        }
                    }
                }
            }
        }
        else if (HasLastKnownPosition.Value)
        {
            if (!wasInvestigatingWithLastKnown)
            {
                investigateStartedTime = Time.time;
                wasInvestigatingWithLastKnown = true;
                if (consoleMonitor != null)
                {
                    consoleMonitor.ReportAction(
                        "SenseTargetAction",
                        $"investigate started | lastKnown={LastKnownPosition.Value}",
                        true);
                }
            }

            // 근거리 교전 직후 시야에서 벗어난 경우에도 마지막 위치로 재추적을 유지한다.
            NavMeshAgent agent = Self.Value.GetComponent<NavMeshAgent>();
            float investigateArrivalTolerance = Mathf.Max(
                0.3f,
                config != null ? config.attackRange * 0.5f : DefaultInvestigateArrivalTolerance);
            float investigateTimeoutSeconds = DefaultInvestigateTimeoutSeconds;
            if (config != null)
            {
                float chaseSpeed = Mathf.Max(0.1f, config.chaseSpeed);
                // 감지 반경 기준으로 "해당 지점을 수색할 합리적 시간"을 산정해 무한 Investigate를 차단한다.
                investigateTimeoutSeconds = Mathf.Max(
                    DefaultInvestigateTimeoutSeconds,
                    (config.detectRadius / chaseSpeed) + 2.0f);
            }

            float planarToLastKnown = Vector3.Distance(
                new Vector3(self.position.x, 0f, self.position.z),
                new Vector3(LastKnownPosition.Value.x, 0f, LastKnownPosition.Value.z));
            bool reachedByDistance = planarToLastKnown <= investigateArrivalTolerance;
            bool reachedByAgent = agent != null &&
                agent.isOnNavMesh &&
                !agent.pathPending &&
                agent.remainingDistance <= investigateArrivalTolerance;
            bool investigateTimedOut = investigateStartedTime > 0f &&
                Time.time - investigateStartedTime >= investigateTimeoutSeconds;

            if (reachedByDistance || reachedByAgent || investigateTimedOut)
            {
                HasLastKnownPosition.Value = false;
                investigateStartedTime = -1f;
                wasInvestigatingWithLastKnown = false;

                if (agent != null && agent.isOnNavMesh)
                {
                    agent.ResetPath();
                }

                if (debugVisualizer != null)
                {
                    debugVisualizer.ReportSenseHint(
                        "Patrol",
                        investigateTimedOut ? "Investigate timeout -> Patrol" : "Investigate reached -> Patrol");
                }
                if (consoleMonitor != null)
                {
                    string clearReason = investigateTimedOut ? "timeout" : "reached-last-known";
                    consoleMonitor.ReportAction(
                        "SenseTargetAction",
                        $"clear last known | reason={clearReason} planarToLastKnown={planarToLastKnown:F2}",
                        true);
                    consoleMonitor.ReportBranch("Patrol", $"Investigate complete ({clearReason})");
                }
            }

            if (HasLastKnownPosition.Value && agent != null && agent.isOnNavMesh)
            {
                float chaseSpeed = config != null ? config.chaseSpeed : agent.speed;
                agent.isStopped = false;
                agent.speed = chaseSpeed;
                agent.SetDestination(LastKnownPosition.Value);

                if (consoleMonitor != null)
                {
                    consoleMonitor.ReportAction(
                        "SenseTargetAction",
                        $"fallback investigate chase | speed={agent.speed:F2} dest={LastKnownPosition.Value}",
                        false);
                }
            }
        }
        else
        {
            investigateStartedTime = -1f;
            wasInvestigatingWithLastKnown = false;
            DriveFallbackPatrol(self, bridge, config, consoleMonitor, debugVisualizer);
        }

        if (debugVisualizer != null)
        {
            debugVisualizer.ReportSense(visible, planarDistance, HasLastKnownPosition.Value, LastKnownPosition.Value, target.position);

            string suggestedBranch;
            if (visible && planarDistance <= (config != null ? config.attackRange : 2f))
            {
                suggestedBranch = "Attack";
            }
            else if (visible)
            {
                suggestedBranch = "Chase";
            }
            else if (HasLastKnownPosition.Value)
            {
                suggestedBranch = "Investigate";
            }
            else
            {
                suggestedBranch = "Patrol";
            }
            debugVisualizer.ReportSenseHint(suggestedBranch, "Sense evaluation");
        }
        if (consoleMonitor != null)
        {
            string suggestedBranch;
            if (visible && planarDistance <= (config != null ? config.attackRange : 2f))
            {
                suggestedBranch = "Attack";
            }
            else if (visible)
            {
                suggestedBranch = "Chase";
            }
            else if (HasLastKnownPosition.Value)
            {
                suggestedBranch = "Investigate";
            }
            else
            {
                suggestedBranch = "Patrol";
            }

            float attackRange = config != null ? config.attackRange : 2f;
            consoleMonitor.ReportSenseEvaluation(visible, planarDistance, HasLastKnownPosition.Value, suggestedBranch);
            consoleMonitor.ReportSenseBreakdown(
                visible,
                planarDistance,
                detectRadius,
                attackRange,
                inRadius,
                inFov,
                blocked,
                HasLastKnownPosition.Value,
                suggestedBranch);
        }

        // 감지 및 정보 업데이트가 완료되었으므로 성공(Success)을 반환합니다.
        return Status.Success;
    }

    // Branch 전이가 실패해 Patrol SubGraph가 실행되지 않을 때를 대비한 최소 순찰 복구 로직입니다.
    private void DriveFallbackPatrol(
        Transform self,
        EnemyBehaviorBridge bridge,
        EnemyConfigSO config,
        EnemyAIConsoleMonitor consoleMonitor,
        EnemyAIDebugVisualizer debugVisualizer)
    {
        if (Self?.Value == null || bridge == null || !bridge.HasPatrolPoints)
            return;

        NavMeshAgent agent = Self.Value.GetComponent<NavMeshAgent>();
        if (agent == null || !agent.isOnNavMesh)
            return;

        if (Time.time < nextFallbackPatrolDecisionTime)
            return;

        float patrolSpeed = config != null ? config.patrolSpeed : agent.speed;
        float arrivalThreshold = Mathf.Max(0.25f, agent.stoppingDistance + 0.15f);
        bool reachedCurrent = agent.hasPath &&
                              !agent.pathPending &&
                              agent.remainingDistance <= arrivalThreshold;
        bool hasBrokenPath = agent.hasPath && agent.pathStatus != NavMeshPathStatus.PathComplete;
        bool shouldSelectNewPoint = !agent.hasPath || reachedCurrent || hasBrokenPath || agent.isStopped;

        if (!shouldSelectNewPoint)
            return;

        int selectedPatrolIndex = fallbackPatrolIndex;
        Vector3 nextPatrolPosition = bridge.GetPatrolPosition(selectedPatrolIndex);
        fallbackPatrolIndex++;

        agent.isStopped = false;
        agent.speed = patrolSpeed;
        agent.SetDestination(nextPatrolPosition);
        nextFallbackPatrolDecisionTime = Time.time + 0.35f;

        if (debugVisualizer != null)
        {
            debugVisualizer.ReportPatrol(selectedPatrolIndex, nextPatrolPosition);
            debugVisualizer.ReportSenseHint("Patrol", "Sense fallback patrol steering");
        }
        if (consoleMonitor != null)
        {
            consoleMonitor.ReportAction(
                "SenseTargetAction",
                $"fallback patrol steer | index={selectedPatrolIndex} speed={patrolSpeed:F2} dest={nextPatrolPosition}",
                false);
            consoleMonitor.ReportBranch("Patrol", "Sense fallback patrol steering");
        }
    }
}