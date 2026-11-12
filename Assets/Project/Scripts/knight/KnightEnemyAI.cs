using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class KnightEnemyAI : MonoBehaviour
{
    [SerializeField] private WeaponCollider weaponCollider;

    [Header("References")]
    [SerializeField] private Transform player;

    [Header("Detection")]
    [SerializeField] private float detectionRange = 10f;
    [SerializeField] private float attackRange = 2f;

    [Header("Combat")]
    [SerializeField] private float attackCooldown = 2f;

    [Header("Return")]
    [SerializeField] private float returnDistance = 0.5f;

    [Header("Rotation")]
    [SerializeField] private float facePlayerSpeed = 8f;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 0.9f;
    [Tooltip("Ground speed (m/s) the walk animation covers at normal playback. The walk animation is sped up or slowed down to match the real speed, so his feet do not slide.")]
    [SerializeField] private float walkAnimNaturalSpeed = 0.39f;
    [Tooltip("The knight stops this close to the player (a little inside attack range) so he does not jitter at the edge of the range.")]
    [SerializeField] private float stopDistance = 1.5f;
    [Tooltip("How often the chase path is refreshed, in seconds.")]
    [SerializeField] private float repathInterval = 0.2f;

    private float nextRepathTime;
    private bool holdingPosition;
    private float attackStartTime;

    [Header("Death")]
    [SerializeField] private float destroyAfterDeath = 3f;

    private NavMeshAgent agent;
    private Animator animator;

    private Vector3 originalPosition;

    private float nextAttackTime;

    private bool isDead;
    private bool isAttacking;
    private bool isBeingHit;
    private bool returningToStart;

    [Header("Hit Knockback")]
    [SerializeField] private float knockbackDistance = 1.2f;
    [SerializeField] private float knockbackDuration = 0.2f;
    [SerializeField] private float hitStunTime = 0.8f;

    private Coroutine knockbackCoroutine;


    // =========================================================
    // AWAKE
    // =========================================================

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        animator = GetComponent<Animator>();

        if (agent != null)
        {
            agent.speed = moveSpeed;
            agent.stoppingDistance = Mathf.Min(stopDistance, attackRange);
        }

        originalPosition = transform.position;


        if (player == null)
        {
            GameObject playerObject =
                GameObject.FindGameObjectWithTag("Player");

            if (playerObject != null)
            {
                player = playerObject.transform;
            }
        }
    }


    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        if (isDead)
            return;

        if (player == null)
            return;

        if (agent == null || !agent.enabled)
            return;


        // =====================================================
        // HIT
        // =====================================================

        if (isBeingHit)
        {
            StopMovement();

            animator.SetFloat("Speed", 0f);

            return;
        }


        // =====================================================
        // ATTACK
        // =====================================================

        if (isAttacking)
        {
            // Safety net: if the AttackFinished animation event never fires, end the attack
            // once the animator is back in idle (or after a maximum time).
            if (Time.time > attackStartTime + 0.3f)
            {
                AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);

                if ((!animator.IsInTransition(0) && state.IsName("idle")) ||
                    Time.time > attackStartTime + 8f)
                {
                    AttackFinished();
                    return;
                }
            }

            StopMovement();

            animator.SetFloat("Speed", 0f);

            return;
        }


        // =====================================================
        // PLAYER DISTANCE
        // =====================================================

        float playerDistance =
            Vector3.Distance(
                transform.position,
                player.position
            );


        // =====================================================
        // RETURNING
        // =====================================================

        if (returningToStart)
        {
            // Player came back into detection range.
            if (playerDistance <= detectionRange)
            {
                returningToStart = false;

                StopMovement();

                DecideNextAction();

                return;
            }

            ReturnToStart();

            return;
        }


        // =====================================================
        // PLAYER TOO FAR
        // =====================================================

        if (playerDistance > detectionRange)
        {
            StartReturning();

            return;
        }


        // =====================================================
        // PLAYER DETECTED
        // =====================================================

        DecideNextAction();
    }


    // =========================================================
    // DECIDE NEXT ACTION
    // =========================================================

    private void DecideNextAction()
    {
        if (isDead ||
            isBeingHit ||
            isAttacking)
            return;


        float distance =
            Vector3.Distance(
                transform.position,
                player.position
            );


        // =====================================================
        // ATTACK RANGE
        // =====================================================

        if (distance <= attackRange)
        {
            bool canAttack = Time.time >= nextAttackTime;

            // Waiting for the attack cooldown: keep closing in to the stop distance
            // instead of stopping at the very edge of attack range (that made him
            // stop and start every frame while the player moved).
            if (!canAttack)
            {
                float resumeDistance = holdingPosition
                    ? stopDistance + 0.3f
                    : stopDistance;

                if (distance > resumeDistance)
                {
                    holdingPosition = false;
                    ChasePlayer();
                    return;
                }

                holdingPosition = true;
            }

            StopMovement();

            animator.SetFloat("Speed", 0f);

            FacePlayer();

            if (canAttack)
            {
                Attack();
            }

            return;
        }

        holdingPosition = false;


        // =====================================================
        // CHASE
        // =====================================================

        ChasePlayer();
    }


    // =========================================================
    // MOVE ANIMATION
    // =========================================================

    // Keeps the walk animation in sync with how fast the agent really moves
    private void UpdateMoveAnimation()
    {
        float velocity = agent.velocity.magnitude;

        animator.SetFloat("Speed", velocity, 0.1f, Time.deltaTime);

        animator.SetFloat(
            "WalkAnimSpeed",
            Mathf.Clamp(velocity / Mathf.Max(0.05f, walkAnimNaturalSpeed), 0.6f, 2.4f)
        );
    }


    // =========================================================
    // CHASE PLAYER
    // =========================================================

    private void ChasePlayer()
    {
        if (!agent.enabled)
            return;


        returningToStart = false;

        agent.isStopped = false;

        if (Time.time >= nextRepathTime || !agent.hasPath)
        {
            agent.SetDestination(player.position);
            nextRepathTime = Time.time + repathInterval;
        }

        UpdateMoveAnimation();
    }


    // =========================================================
    // START RETURN
    // =========================================================

    private void StartReturning()
    {
        if (!agent.enabled)
            return;


        isAttacking = false;

        returningToStart = true;

        DisableWeapon();

        agent.isStopped = false;

        agent.SetDestination(
            originalPosition
        );


        UpdateMoveAnimation();
    }


    // =========================================================
    // RETURN TO START
    // =========================================================

    private void ReturnToStart()
    {
        if (!agent.enabled)
            return;


        float distance =
            Vector3.Distance(
                transform.position,
                originalPosition
            );


        if (distance <= returnDistance)
        {
            StopMovement();

            returningToStart = false;

            animator.SetFloat(
                "Speed",
                0f
            );

            return;
        }


        agent.isStopped = false;

        agent.SetDestination(
            originalPosition
        );


        UpdateMoveAnimation();
    }


    // =========================================================
    // STOP MOVEMENT
    // =========================================================

    private void StopMovement()
    {
        if (!agent.enabled)
            return;


        agent.isStopped = true;

        agent.ResetPath();

        agent.velocity = Vector3.zero;
    }


    // =========================================================
    // TAKE HIT
    // =========================================================

    public void TakeHit()
    {
        if (isDead)
            return;


        // Cancel current attack.
        isAttacking = false;


        // Cancel returning.
        returningToStart = false;


        // Enter hit state.
        isBeingHit = true;


        // Stop movement.
        StopMovement();


        // Disable weapon.
        DisableWeapon();


        // Stop movement animation.
        animator.SetFloat(
            "Speed",
            0f
        );


        // Play hit animation.
        animator.ResetTrigger("Hit");

        animator.SetTrigger("Hit");


        // Move slightly backward.
        if (knockbackCoroutine != null)
            StopCoroutine(knockbackCoroutine);

        knockbackCoroutine = StartCoroutine(KnockbackRoutine());
    }


    // =========================================================
    // KNOCKBACK
    // =========================================================

    private IEnumerator KnockbackRoutine()
    {
        FacePlayerInstant();

        Vector3 startPosition = transform.position;
        Vector3 targetPosition =
            startPosition - transform.forward * knockbackDistance;

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

            if (agent != null && agent.enabled && agent.isOnNavMesh)
                agent.Move(nextPosition - transform.position);
            else
                transform.position = nextPosition;

            yield return null;
        }

        float wait = Mathf.Max(0f, hitStunTime - knockbackDuration);

        yield return new WaitForSeconds(wait);

        if (!isDead && isBeingHit)
            HitFinished();

        knockbackCoroutine = null;
    }


    // =========================================================
    // HIT FINISHED
    // =========================================================
    //
    // Animation Event at the END of Hit animation.
    //

    public void HitFinished()
    {
        if (isDead)
            return;


        isBeingHit = false;


        StopMovement();


        animator.SetFloat(
            "Speed",
            0f
        );


        // Update() will now decide:
        // Chase / Attack / Return
    }


    // =========================================================
    // ATTACK
    // =========================================================

    private void Attack()
    {
        if (isDead ||
            isAttacking ||
            isBeingHit)
            return;


        isAttacking = true;
        attackStartTime = Time.time;


        StopMovement();

        FacePlayerInstant();


        nextAttackTime =
            Time.time +
            attackCooldown;


        int attack =
            Random.Range(
                1,
                4
            );


        if (attack == 1)
        {
            animator.ResetTrigger("Attack1");

            animator.SetTrigger("Attack1");
        }
        else if (attack == 2)
        {
            animator.ResetTrigger("Attack2");

            animator.SetTrigger("Attack2");
        }
        else
        {
            animator.ResetTrigger("Attack3");

            animator.SetTrigger("Attack3");
        }
    }


    // =========================================================
    // ATTACK FINISHED
    // =========================================================
    //
    // Animation Event at the END of attack animation.
    //

    public void AttackFinished()
    {
        if (isDead)
            return;


        isAttacking = false;

        DisableWeapon();

        StopMovement();

        animator.SetFloat(
            "Speed",
            0f
        );
    }


    // =========================================================
    // INTERRUPT ATTACK
    // =========================================================

    public void InterruptAttack()
    {
        if (isDead)
            return;


        isAttacking = false;

        DisableWeapon();

        StopMovement();

        animator.SetFloat(
            "Speed",
            0f
        );
    }


    // =========================================================
    // FACE PLAYER
    // =========================================================

    private void FacePlayer()
    {
        if (player == null)
            return;


        Vector3 direction =
            player.position -
            transform.position;

        direction.y = 0f;


        if (direction.sqrMagnitude < 0.001f)
            return;


        Quaternion targetRotation =
            Quaternion.LookRotation(
                direction
            );


        transform.rotation =
            Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                facePlayerSpeed *
                Time.deltaTime
            );
    }


    // =========================================================
    // FACE PLAYER INSTANT
    // =========================================================

    private void FacePlayerInstant()
    {
        if (player == null)
            return;


        Vector3 direction =
            player.position -
            transform.position;

        direction.y = 0f;


        if (direction.sqrMagnitude < 0.001f)
            return;


        transform.rotation =
            Quaternion.LookRotation(
                direction
            );
    }


    // =========================================================
    // ENABLE WEAPON
    // =========================================================

    public void EnableWeapon()
    {
        if (weaponCollider != null)
        {
            weaponCollider.EnableWeapon();
        }
    }


    // =========================================================
    // DISABLE WEAPON
    // =========================================================

    public void DisableWeapon()
    {
        if (weaponCollider != null)
        {
            weaponCollider.DisableWeapon();
        }
    }


    // =========================================================
    // DIE
    // =========================================================
    //
    // ALL DEATH LOGIC IS HERE.
    //

    public void Die()
    {
        if (isDead)
            return;

        isDead = true;

        isAttacking = false;
        isBeingHit = false;
        returningToStart = false;

        DisableWeapon();

        if (knockbackCoroutine != null)
        {
            StopCoroutine(knockbackCoroutine);
            knockbackCoroutine = null;
        }

        // Stop NavMesh
        if (agent != null && agent.enabled)
        {
            agent.isStopped = true;
            agent.ResetPath();
            agent.velocity = Vector3.zero;
            agent.enabled = false;
        }

        // Disable collider
        Collider col = GetComponent<Collider>();

        if (col != null)
        {
            col.enabled = false;
        }

        // Play death animation
        animator.ResetTrigger("Hit");
        animator.ResetTrigger("Death");
        animator.SetTrigger("Death");

        // Destroy Knight after delay
        Destroy(gameObject, destroyAfterDeath);
    }


    // =========================================================
    // DEAD CHECK
    // =========================================================

    public bool IsDead
    {
        get
        {
            return isDead;
        }
    }
}