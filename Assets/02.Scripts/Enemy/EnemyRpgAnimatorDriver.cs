using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// RPG 계열 Animator Controller가 요구하는 파라미터를 Enemy AI 상태와 동기화한다.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyRpgAnimatorDriver : MonoBehaviour
{
    [Header("RPG Animator Parameters")]
    [SerializeField] private string movingParam = "Moving";
    [SerializeField] private string velocityXParam = "Velocity X";
    [SerializeField] private string velocityZParam = "Velocity Z";
    [SerializeField] private string weaponParam = "Weapon";
    [SerializeField] private string actionParam = "Action";
    [SerializeField] private string triggerNumberParam = "TriggerNumber";

    [Header("RPG Animator Values")]
    [SerializeField] private int meleeWeaponValue = 1;
    [SerializeField] private int meleeAttackActionValue = 1;
    [SerializeField] private int meleeAttackTriggerNumberValue = 4;
    [SerializeField] private float movingThreshold = 0.05f;

    private Animator cachedAnimator;
    private NavMeshAgent cachedAgent;

    private void Awake()
    {
        cachedAnimator = GetComponent<Animator>();
        cachedAgent = GetComponent<NavMeshAgent>();

        ApplyWeaponStyle();
    }

    private void Update()
    {
        if (cachedAnimator == null || cachedAgent == null)
        {
            return;
        }

        // NavMeshAgent 월드 속도를 로컬 기준으로 변환해 2D BlendTree 입력값으로 사용한다.
        Vector3 planarVelocity = cachedAgent.velocity;
        planarVelocity.y = 0f;
        Vector3 localVelocity = transform.InverseTransformDirection(planarVelocity);

        float normalizedX = Mathf.Clamp(localVelocity.x / Mathf.Max(0.01f, cachedAgent.speed), -1f, 1f);
        float normalizedZ = Mathf.Clamp(localVelocity.z / Mathf.Max(0.01f, cachedAgent.speed), -1f, 1f);
        bool isMoving = !cachedAgent.isStopped && planarVelocity.sqrMagnitude >= movingThreshold * movingThreshold;

        cachedAnimator.SetBool(movingParam, isMoving);
        cachedAnimator.SetFloat(velocityXParam, normalizedX);
        cachedAnimator.SetFloat(velocityZParam, normalizedZ);
    }

    /// <summary>
    /// Enemy가 사용하는 무기 스타일을 고정해 올바른 상태머신 분기로 진입시킨다.
    /// </summary>
    public void ApplyWeaponStyle()
    {
        if (cachedAnimator == null)
        {
            return;
        }

        cachedAnimator.SetInteger(weaponParam, meleeWeaponValue);
    }

    /// <summary>
    /// Trigger를 발행하기 전에 공격 분기 파라미터를 준비한다.
    /// </summary>
    public void PrepareAttackParameters()
    {
        if (cachedAnimator == null)
        {
            return;
        }

        cachedAnimator.SetInteger(weaponParam, meleeWeaponValue);
        cachedAnimator.SetInteger(actionParam, meleeAttackActionValue);
        cachedAnimator.SetInteger(triggerNumberParam, meleeAttackTriggerNumberValue);
        cachedAnimator.SetBool(movingParam, false);
    }
}
