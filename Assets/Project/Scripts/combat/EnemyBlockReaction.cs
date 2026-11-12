using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Sword blowback: when the player's shield blocks an enemy sword, the enemy plays the
/// "Block reaction" animation (played in reverse), its attack is cancelled and it is
/// dazed for a moment.
/// You do not need to add this by hand - ShieldBlock adds it to the enemy the first time it is blocked.
/// </summary>
public class EnemyBlockReaction : MonoBehaviour
{
    [SerializeField] private string triggerName = "BlockReaction";
    [Tooltip("How long the enemy stays dazed after the blowback, in seconds.")]
    [SerializeField, Min(0.1f)] private float stunTime = 0.6f;
    [Tooltip("Minimum time between two block reactions.")]
    [SerializeField, Min(0f)] private float cooldown = 0.5f;

    private Animator animator;
    private NavMeshAgent agent;
    private MonoBehaviour ai;
    private float lastTime = -10f;
    private bool stunned;
    private float stunEndTime;

    public static void Play(Collider enemyWeapon)
    {
        if (enemyWeapon == null) return;

        // Some enemies have a second Animator on their mesh with no controller,
        // so take the nearest Animator that really has one.
        Animator anim = null;

        foreach (Animator candidate in enemyWeapon.GetComponentsInParent<Animator>())
        {
            if (candidate.runtimeAnimatorController != null)
            {
                anim = candidate;
                break;
            }
        }

        if (anim == null) return;

        // The boss runs its own state machine
        BossController boss = anim.GetComponent<BossController>();

        if (boss != null)
        {
            boss.NotifyBlocked();
            return;
        }

        EnemyBlockReaction reaction = anim.GetComponent<EnemyBlockReaction>();
        if (reaction == null)
            reaction = anim.gameObject.AddComponent<EnemyBlockReaction>();

        reaction.Trigger();
    }

    private void Awake()
    {
        animator = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();

        ai = GetComponent<KnightEnemyAI>();
        if (ai == null) ai = GetComponent<EnemyAI>();
    }

    private void Trigger()
    {
        if (animator == null) return;
        if (Time.time - lastTime < cooldown) return;

        // A dead enemy has its NavMeshAgent switched off
        if (agent != null && !agent.enabled) return;

        lastTime = Time.time;

        // End the current attack and switch the weapon off
        SendMessage("InterruptAttack", SendMessageOptions.DontRequireReceiver);

        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
            agent.velocity = Vector3.zero;
        }

        animator.ResetTrigger(triggerName);
        animator.SetTrigger(triggerName);

        // Daze: the AI stays off until the timer ends (see Update)
        if (ai != null)
            ai.enabled = false;

        stunned = true;
        stunEndTime = Time.time + stunTime;
    }

    private void Update()
    {
        if (stunned && Time.time >= stunEndTime)
            EndStun();
    }

    private void EndStun()
    {
        stunned = false;

        bool alive = agent == null || agent.enabled;

        if (ai != null && alive)
            ai.enabled = true;

        if (agent != null && agent.enabled && agent.isOnNavMesh)
            agent.isStopped = false;
    }

    private void OnDisable()
    {
        // Never leave the enemy AI switched off if this component is turned off mid-stun
        if (ai != null && (agent == null || agent.enabled))
            ai.enabled = true;
    }
}
