using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    [SerializeField] private WeaponCollider weaponCollider;

    [Header("References")]
    [SerializeField] private Transform player;

    private NavMeshAgent agent;
    private Animator animator;

    [Header("Detection")]
    [SerializeField] private float detectionRange = 10f;
    [SerializeField] private float attackRange = 2f;

    [Header("Combat")]
    [SerializeField] private float attackCooldown = 2f;

    private float nextAttackTime;

    private bool isDead = false;
    private bool isAttacking = false;

    //=================================================
    // PATROL
    //=================================================

    [Header("Patrol")]
    [SerializeField] private Transform[] patrolPoints = new Transform[2];

    [SerializeField] private float patrolWaitTime = 2f;

    [SerializeField] private float patrolRotationSpeed = 8f;

    private int currentPatrolPoint = 0;
    private float patrolWaitTimer = 0f;

    //=================================================
    // AWAKE
    //=================================================

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        if (player == null)
        {
            GameObject playerObject =
                GameObject.FindGameObjectWithTag("Player");

            if (playerObject != null)
                player = playerObject.transform;
        }
    }

    //=================================================
    // UPDATE
    //=================================================

    void Update()
    {
        if (isDead || player == null)
            return;

        float distance = Vector3.Distance(
            transform.position,
            player.position
        );

        //-------------------------------------------------
        // OUTSIDE DETECTION RANGE → PATROL
        //-------------------------------------------------

        if (distance > detectionRange)
        {
            Patrol();
            return;
        }

        //-------------------------------------------------
        // ATTACK
        //-------------------------------------------------

        if (distance <= attackRange)
        {
            agent.ResetPath();

            FacePlayer();

            animator.SetFloat("Speed", 0);

            if (!isAttacking && Time.time >= nextAttackTime)
            {
                Attack();
            }

            return;
        }

        //-------------------------------------------------
        // CHASE
        //-------------------------------------------------

        if (!isAttacking)
        {
            agent.isStopped = false;

            agent.SetDestination(player.position);

            animator.SetFloat(
                "Speed",
                agent.velocity.magnitude
            );
        }
    }

    //=================================================
    // PATROL
    //=================================================

    void Patrol()
    {
        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            agent.ResetPath();
            agent.isStopped = true;
            animator.SetFloat("Speed", 0);
            return;
        }

        if (currentPatrolPoint >= patrolPoints.Length)
            currentPatrolPoint = 0;

        Transform targetPoint = patrolPoints[currentPatrolPoint];

        if (targetPoint == null)
            return;

        // Direction toward patrol point
        Vector3 direction = targetPoint.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation =
                Quaternion.LookRotation(direction);

            // Rotate first
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                patrolRotationSpeed * Time.deltaTime
            );
        }

        // Check how closely we're facing the patrol point
        float angle = Vector3.Angle(
            transform.forward,
            direction.normalized
        );

        // Still turning
        if (angle > 5f)
        {
            agent.isStopped = true;

            animator.SetFloat("Speed", 0);

            return;
        }

        // Now walk toward patrol point
        agent.isStopped = false;

        agent.SetDestination(targetPoint.position);

        animator.SetFloat(
            "Speed",
            agent.velocity.magnitude
        );

        // Reached patrol point
        if (!agent.pathPending &&
            agent.remainingDistance <= agent.stoppingDistance)
        {
            agent.isStopped = true;

            animator.SetFloat("Speed", 0);

            patrolWaitTimer += Time.deltaTime;

            if (patrolWaitTimer >= patrolWaitTime)
            {
                currentPatrolPoint++;

                if (currentPatrolPoint >= patrolPoints.Length)
                    currentPatrolPoint = 0;

                patrolWaitTimer = 0f;
            }
        }
    }

    //=================================================
    // ATTACK
    //=================================================

    void Attack()
    {
        isAttacking = true;

        agent.isStopped = true;

        nextAttackTime = Time.time + attackCooldown;

        animator.ResetTrigger("Attack");
        animator.SetTrigger("Attack");
    }

    //=================================================
    // WEAPON
    //=================================================

    public void EnableWeapon()
    {
        weaponCollider.EnableWeapon();
    }

    public void DisableWeapon()
    {
        weaponCollider.DisableWeapon();
    }

    //=================================================
    // ATTACK FINISHED
    //=================================================

    public void AttackFinished()
    {
        isAttacking = false;

        agent.isStopped = false;
    }

    //=================================================
    // FACE PLAYER
    //=================================================

    void FacePlayer()
    {
        Vector3 direction =
            player.position - transform.position;

        direction.y = 0;

        if (direction.sqrMagnitude < 0.001f)
            return;

        Quaternion rotation =
            Quaternion.LookRotation(direction);

        transform.rotation =
            Quaternion.Slerp(
                transform.rotation,
                rotation,
                8f * Time.deltaTime
            );
    }

    //=================================================
    // DEATH
    //=================================================

    public void Die()
    {
        isDead = true;

        agent.isStopped = true;
        agent.enabled = false;
    }

    //=================================================
    // INTERRUPT ATTACK
    //=================================================

    public void InterruptAttack()
    {
        isAttacking = false;

        DisableWeapon();
    }
}