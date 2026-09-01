using UnityEngine;
using UnityEngine.AI;

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


    // =========================================================
    // AWAKE
    // =========================================================

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        animator = GetComponent<Animator>();

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
            StopMovement();

            animator.SetFloat(
                "Speed",
                0f
            );

            FacePlayer();


            if (Time.time >= nextAttackTime)
            {
                Attack();
            }

            return;
        }


        // =====================================================
        // CHASE
        // =====================================================

        ChasePlayer();
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

        agent.SetDestination(
            player.position
        );


        animator.SetFloat(
            "Speed",
            agent.desiredVelocity.magnitude
        );
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


        animator.SetFloat(
            "Speed",
            agent.desiredVelocity.magnitude
        );
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


        animator.SetFloat(
            "Speed",
            agent.desiredVelocity.magnitude
        );
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