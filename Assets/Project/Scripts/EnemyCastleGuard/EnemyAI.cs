
using UnityEngine;
using UnityEngine.AI;
using System.Collections;

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
    private bool isBeingKnockedBack = false;

    //=================================================
    // KNOCKBACK SETTINGS
    //=================================================

    [Header("Hit Knockback")]
    [SerializeField] private float knockbackDistance = 1.2f;
    [SerializeField] private float knockbackDuration = 0.2f;
    [SerializeField] private float hitRotationSpeed = 8f;

    private Coroutine knockbackCoroutine;

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

        // Do not let normal AI movement override knockback.
        if (isBeingKnockedBack)
            return;

        float distance = Vector3.Distance(
            transform.position,
            player.position
        );

        if (distance > detectionRange)
        {
            Patrol();
            return;
        }

        if (distance <= attackRange)
        {
            agent.ResetPath();
            agent.isStopped = true;

            FacePlayer();

            animator.SetFloat("Speed", 0);

            if (!isAttacking && Time.time >= nextAttackTime)
            {
                Attack();
            }

            return;
        }

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

        Vector3 direction =
            targetPoint.position - transform.position;

        direction.y = 0f;

        if (direction.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation =
                Quaternion.LookRotation(direction);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                patrolRotationSpeed * Time.deltaTime
            );
        }

        float angle = Vector3.Angle(
            transform.forward,
            direction.normalized
        );

        if (angle > 5f)
        {
            agent.isStopped = true;
            animator.SetFloat("Speed", 0);
            return;
        }

        agent.isStopped = false;
        agent.SetDestination(targetPoint.position);

        animator.SetFloat(
            "Speed",
            agent.velocity.magnitude
        );

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
        else
        {
            patrolWaitTimer = 0f;
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
        if (weaponCollider != null)
            weaponCollider.EnableWeapon();
    }

    public void DisableWeapon()
    {
        if (weaponCollider != null)
            weaponCollider.DisableWeapon();
    }

    //=================================================
    // ATTACK FINISHED
    //=================================================

    public void AttackFinished()
    {
        isAttacking = false;

        if (!isDead && !isBeingKnockedBack && agent.enabled)
            agent.isStopped = false;
    }

    //=================================================
    // FACE PLAYER
    //=================================================

    void FacePlayer()
    {
        if (player == null)
            return;

        Vector3 direction =
            player.position - transform.position;

        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
            return;

        Quaternion rotation =
            Quaternion.LookRotation(direction);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            rotation,
            8f * Time.deltaTime
        );
    }

    //=================================================
    // HIT / KNOCKBACK
    //=================================================

    public void TakeHit()
    {
        if (isDead || isBeingKnockedBack)
            return;

        if (knockbackCoroutine != null)
            StopCoroutine(knockbackCoroutine);

        knockbackCoroutine = StartCoroutine(KnockbackRoutine());
    }

    IEnumerator KnockbackRoutine()
    {
        isBeingKnockedBack = true;
        isAttacking = false;

        DisableWeapon();

        // Stop NavMesh movement so it does not fight knockback.
        if (agent != null && agent.enabled)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        // Face the player before being pushed away from them.
        if (player != null)
        {
            Vector3 toPlayer = player.position - transform.position;
            toPlayer.y = 0f;

            if (toPlayer.sqrMagnitude > 0.001f)
            {
                transform.rotation =
                    Quaternion.LookRotation(toPlayer);
            }
        }

        // Play the hit animation.
        animator.ResetTrigger("Attack");
        animator.ResetTrigger("Hit");
        animator.SetTrigger("Hit");

        // Move backward relative to the enemy's facing direction.
        Vector3 startPosition = transform.position;
        Vector3 backwardDirection = -transform.forward;

        Vector3 targetPosition =
            startPosition + backwardDirection * knockbackDistance;

        float elapsed = 0f;

        while (elapsed < knockbackDuration && !isDead)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / knockbackDuration);

            Vector3 nextPosition = Vector3.Lerp(
                startPosition,
                targetPosition,
                t
            );

            // Keep movement on the NavMesh when possible.
            if (agent != null && agent.enabled &&
                agent.isOnNavMesh)
            {
                agent.Move(nextPosition - transform.position);
            }
            else
            {
                transform.position = nextPosition;
            }

            yield return null;
        }

        if (!isDead)
        {
            isBeingKnockedBack = false;

            if (agent != null && agent.enabled)
                agent.isStopped = false;
        }

        knockbackCoroutine = null;
    }

    //=================================================
    // DEATH
    //=================================================

    public void Die()
    {
        isDead = true;
        isBeingKnockedBack = false;

        if (knockbackCoroutine != null)
        {
            StopCoroutine(knockbackCoroutine);
            knockbackCoroutine = null;
        }

        DisableWeapon();

        if (agent != null && agent.enabled)
        {
            agent.isStopped = true;
            agent.ResetPath();
            agent.enabled = false;
        }
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
