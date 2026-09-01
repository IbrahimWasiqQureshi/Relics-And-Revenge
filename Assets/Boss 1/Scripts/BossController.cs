using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public enum BossState
{
    Idle,
    Taunting,
    Chasing,
    RunAttacking,
    Attacking,
    Blocking,
    Recovering,
    Hit,
    Dead
}

public enum BossAttackType
{
    Attack1,
    Attack2,
    Attack3
}

[Serializable]
public class BossAnimatorParams
{
    public string speedParam = "Speed";

    public string attack1Trigger = "Attack1";
    public string attack2Trigger = "Attack2";
    public string attack3Trigger = "Attack3";
    public string runAttackTrigger = "RunAttack";

    public string hitTrigger = "Hit";
    public string blockTrigger = "Block";

    public string tauntTrigger = "Taunt";

    public string deadBool = "Dead";
}


[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(NavMeshAgent))]
public class BossController : MonoBehaviour
{
    // =========================================================
    // REFERENCES
    // =========================================================

    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private BossWeaponCollider weaponCollider;


    // =========================================================
    // DETECTION
    // =========================================================

    [Header("Detection")]
    [SerializeField] private float detectionRange = 15f;

    [SerializeField] private float attackRange = 2.2f;

    [SerializeField] private float runAttackRange = 6f;

    [SerializeField] private float returnDistance = 0.5f;


    // =========================================================
    // MOVEMENT
    // =========================================================

    [Header("Movement")]
    [SerializeField] private float runSpeed = 3.5f;

    [SerializeField] private float acceleration = 8f;

    [SerializeField] private float maxAcceleration = 10f;

    [SerializeField] private float turnSpeed = 8f;


    // =========================================================
    // TIMING
    // =========================================================

    [Header("Timing")]
    [SerializeField] private float tauntDuration = 2f;

    [SerializeField] private float minTimeBetweenAttacks = 0.5f;

    [SerializeField] private float attackCooldown = 2.2f;

    [SerializeField] private float recoveryTime = 1f;

    [SerializeField] private float hitReactionDuration = 0.6f;

    [SerializeField] private float runAttackDuration = 4f;

    [Tooltip("How long into the Run Attack the boss keeps physically closing distance, before planting for the strike/recovery. Should match when the sword actually hits the ground in the clip - shorter than Run Attack Duration.")]
    [SerializeField] private float runAttackMoveDuration = 1.2f;

    [SerializeField] private float maxAttackLockDuration = 5f;


    // =========================================================
    // BLOCK
    // =========================================================

    [Header("Block")]
    [Range(0f, 1f)]
    [SerializeField] private float blockChance = 0.35f;

    [SerializeField] private float blockCooldown = 1.5f;

    [SerializeField] private float blockAttackDelay = 0.1f;


    // =========================================================
    // ANIMATOR
    // =========================================================

    private readonly BossAnimatorParams paramNames =
        new BossAnimatorParams();


    // =========================================================
    // EVENTS
    // =========================================================

    public event Action BossFightStarted;
    public event Action BossFightEnded;

    public BossState State => state;


    // =========================================================
    // PRIVATE
    // =========================================================

    private NavMeshAgent agent;

    private Animator animator;

    private BossHealth bossHealth;

    private HashSet<int> validParamHashes;

    private BossState state;

    private bool hasEngaged;

    private bool returningToStart;

    private float nextAttackAllowedTime;

    private float nextBlockAllowedTime;

    private float lockStartTime;

    private float lockDuration;

    private Vector3 originalPosition;

    private bool recoveringFromBlock;


    // =========================================================
    // RUN ATTACK
    // =========================================================

    private Vector3 runAttackTargetPosition;

    private bool hasRunAttackTarget;


    // =========================================================
    // AWAKE
    // =========================================================

    private void Awake()
    {
        agent =
            GetComponent<NavMeshAgent>();

        animator =
            GetComponent<Animator>();

        bossHealth =
            GetComponent<BossHealth>();


        // NavMeshAgent controls position.
        // BossController controls rotation.

        agent.updatePosition = true;

        agent.updateRotation = false;

        agent.speed = runSpeed;

        agent.acceleration =
            Mathf.Clamp(
                acceleration,
                0.1f,
                maxAcceleration
            );

        agent.angularSpeed = 0f;

        agent.autoBraking = true;

        agent.stoppingDistance =
            Mathf.Max(
                0.1f,
                attackRange * 0.8f
            );


        originalPosition =
            transform.position;


        CacheValidAnimatorParams();

        FindPlayer();

        state =
            BossState.Idle;


        if (!agent.isOnNavMesh)
        {
            NavMeshHit hit;

            if (NavMesh.SamplePosition(
                transform.position,
                out hit,
                5f,
                NavMesh.AllAreas))
            {
                agent.Warp(
                    hit.position
                );
            }
            else
            {
                Debug.LogError(
                    "BossController: Boss is NOT on a NavMesh!"
                );
            }
        }
    }


    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        if (state == BossState.Dead)
            return;


        if (player == null)
        {
            FindPlayer();

            return;
        }


        switch (state)
        {
            case BossState.Idle:

                TickIdle();

                break;


            case BossState.Taunting:

                TickTaunting();

                break;


            case BossState.Chasing:

                TickChasing();

                break;


            case BossState.RunAttacking:

                TickRunAttacking();

                break;


            case BossState.Attacking:

                TickAttacking();

                break;


            case BossState.Blocking:

                TickBlocking();

                break;


            case BossState.Recovering:

                TickRecovering();

                break;


            case BossState.Hit:

                TickHit();

                break;
        }


        UpdateAnimatorSpeed();
    }


    // =========================================================
    // IDLE
    // =========================================================

    private void TickIdle()
    {
        StopMovement();

        FacePlayer();


        float distance =
            HorizontalDistance(
                transform.position,
                player.position
            );


        if (distance <= detectionRange)
        {
            // This is the FIRST moment
            // the player enters detection range.

            RaiseFightStarted();

            StartTaunt();
        }
    }


    // =========================================================
    // START FIGHT
    // =========================================================

    private void RaiseFightStarted()
    {
        if (hasEngaged)
            return;


        hasEngaged = true;


        // =====================================================
        // SHOW BOSS HEALTH BAR
        // =====================================================

        if (bossHealth != null)
        {
            bossHealth.ShowHealthBar();
        }


        // =====================================================
        // EVENT
        // =====================================================

        BossFightStarted?.Invoke();
    }


    // =========================================================
    // TAUNT
    // =========================================================

    private void StartTaunt()
    {
        StopMovement();

        state =
            BossState.Taunting;

        BeginLock(
            tauntDuration
        );

        SafeSetTrigger(
            paramNames.tauntTrigger
        );
    }


    private void TickTaunting()
    {
        StopMovement();

        FacePlayer();


        if (Time.time - lockStartTime >= lockDuration)
        {
            state =
                BossState.Chasing;
        }
    }


    // =========================================================
    // CHASING
    // =========================================================

    private void TickChasing()
    {
        if (player == null)
            return;


        if (returningToStart)
        {
            TickReturning();

            return;
        }


        float distance =
            HorizontalDistance(
                transform.position,
                player.position
            );


        // PLAYER ESCAPED

        if (distance > detectionRange)
        {
            StartReturning();

            return;
        }


        // MELEE RANGE

        if (distance <= attackRange)
        {
            StopMovement();

            FacePlayer();

            TryStartMeleeAttack();

            return;
        }


        // RUN ATTACK RANGE

        if (distance <= runAttackRange &&
            Time.time >= nextAttackAllowedTime)
        {
            StartRunAttack();

            return;
        }


        // NORMAL CHASE

        MoveTowardsPlayer();
    }


    // =========================================================
    // MOVE TOWARDS PLAYER
    // =========================================================

    private void MoveTowardsPlayer()
    {
        if (agent == null ||
            !agent.enabled ||
            !agent.isOnNavMesh ||
            player == null)
        {
            return;
        }


        agent.isStopped = false;

        agent.speed = runSpeed;

        agent.acceleration =
            Mathf.Clamp(
                acceleration,
                0.1f,
                maxAcceleration
            );


        Vector3 targetPosition =
            player.position;


        NavMeshHit hit;


        if (NavMesh.SamplePosition(
            targetPosition,
            out hit,
            2f,
            NavMesh.AllAreas))
        {
            targetPosition =
                hit.position;
        }


        agent.SetDestination(
            targetPosition
        );


        ClampAgentVelocity();


        Vector3 movementDirection =
            agent.velocity;

        movementDirection.y = 0f;


        if (movementDirection.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation =
                Quaternion.LookRotation(
                    movementDirection.normalized
                );


            transform.rotation =
                Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    turnSpeed *
                    Time.deltaTime
                );
        }
    }


    // =========================================================
    // STOP
    // =========================================================

    private void StopMovement()
    {
        if (agent == null ||
            !agent.enabled ||
            !agent.isOnNavMesh)
        {
            return;
        }


        agent.isStopped = true;

        agent.ResetPath();

        agent.velocity =
            Vector3.zero;
    }


    // =========================================================
    // SPEED
    // =========================================================

    private void UpdateAnimatorSpeed()
    {
        float speed = 0f;


        if (state == BossState.Chasing ||
            state == BossState.RunAttacking)
        {
            if (agent != null &&
                agent.enabled &&
                agent.isOnNavMesh &&
                !agent.isStopped)
            {
                speed =
                    agent.velocity.magnitude;
            }
        }


        SafeSetFloat(
            paramNames.speedParam,
            speed
        );
    }


    // =========================================================
    // VELOCITY
    // =========================================================

    private void ClampAgentVelocity()
    {
        if (agent == null ||
            !agent.enabled ||
            !agent.isOnNavMesh)
        {
            return;
        }


        float maxSpeed =
            agent.speed;


        Vector3 velocity =
            agent.velocity;


        if (velocity.magnitude > maxSpeed)
        {
            agent.velocity =
                velocity.normalized *
                maxSpeed;
        }
    }


    // =========================================================
    // MELEE ATTACK
    // =========================================================

    private void TryStartMeleeAttack()
    {
        if (Time.time < nextAttackAllowedTime)
            return;


        BossAttackType attack =
            (BossAttackType)
            UnityEngine.Random.Range(
                0,
                3
            );


        StartMeleeAttack(
            attack
        );
    }


    private void StartMeleeAttack(
        BossAttackType type)
    {
        StopMovement();

        FacePlayer();


        state =
            BossState.Attacking;


        recoveringFromBlock = false;


        BeginLock(
            maxAttackLockDuration
        );


        nextAttackAllowedTime =
            Time.time +
            Mathf.Max(
                attackCooldown,
                minTimeBetweenAttacks
            );


        switch (type)
        {
            case BossAttackType.Attack1:

                SafeSetTrigger(
                    paramNames.attack1Trigger
                );

                break;


            case BossAttackType.Attack2:

                SafeSetTrigger(
                    paramNames.attack2Trigger
                );

                break;


            case BossAttackType.Attack3:

                SafeSetTrigger(
                    paramNames.attack3Trigger
                );

                break;
        }
    }


    // =========================================================
    // ATTACKING
    // =========================================================

    private void TickAttacking()
    {
        StopMovement();

        FacePlayer();


        if (Time.time - lockStartTime >= lockDuration)
        {
            AttackFinished();
        }
    }


    // =========================================================
    // ATTACK FINISHED
    // =========================================================

    public void AttackFinished()
    {
        if (state != BossState.Attacking)
            return;


        DisableWeapon();

        StopMovement();


        state =
            BossState.Recovering;


        recoveringFromBlock = false;


        BeginLock(
            recoveryTime
        );
    }


    // =========================================================
    // RUN ATTACK
    // =========================================================

    private void StartRunAttack()
    {
        if (player == null)
            return;


        runAttackTargetPosition =
            player.position;


        NavMeshHit hit;


        if (NavMesh.SamplePosition(
            runAttackTargetPosition,
            out hit,
            2f,
            NavMesh.AllAreas))
        {
            runAttackTargetPosition =
                hit.position;
        }


        hasRunAttackTarget = true;


        state =
            BossState.RunAttacking;


        recoveringFromBlock = false;


        BeginLock(
            runAttackDuration
        );


        nextAttackAllowedTime =
            Time.time +
            Mathf.Max(
                attackCooldown,
                minTimeBetweenAttacks
            );


        if (agent != null &&
            agent.enabled &&
            agent.isOnNavMesh)
        {
            agent.speed =
                runSpeed;


            agent.acceleration =
                Mathf.Clamp(
                    acceleration,
                    0.1f,
                    maxAcceleration
                );


            agent.isStopped = false;


            agent.SetDestination(
                runAttackTargetPosition
            );
        }


        SafeSetTrigger(
            paramNames.runAttackTrigger
        );
    }


    // =========================================================
    // RUN ATTACK UPDATE
    // =========================================================

    private void TickRunAttacking()
    {
        if (!hasRunAttackTarget)
        {
            RunAttackFinished();

            return;
        }


        if (agent == null ||
            !agent.enabled ||
            !agent.isOnNavMesh)
        {
            RunAttackFinished();

            return;
        }


        bool stillClosingDistance =
            (Time.time - lockStartTime) < runAttackMoveDuration;


        if (stillClosingDistance)
        {
            agent.isStopped = false;

            agent.speed =
                runSpeed;


            if (player != null)
            {
                runAttackTargetPosition = player.position;

                NavMeshHit trackHit;

                if (NavMesh.SamplePosition(
                    runAttackTargetPosition,
                    out trackHit,
                    2f,
                    NavMesh.AllAreas))
                {
                    runAttackTargetPosition = trackHit.position;
                }
            }


            agent.SetDestination(
                runAttackTargetPosition
            );


            ClampAgentVelocity();


            Vector3 movementDirection =
                agent.velocity;

            movementDirection.y = 0f;


            if (movementDirection.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation =
                    Quaternion.LookRotation(
                        movementDirection.normalized
                    );


                transform.rotation =
                    Quaternion.Slerp(
                        transform.rotation,
                        targetRotation,
                        turnSpeed *
                        Time.deltaTime
                    );
            }


            if (!agent.pathPending &&
                agent.remainingDistance <= attackRange)
            {
                RunAttackFinished();

                return;
            }
        }
        else
        {
            // Move window elapsed - the clip is planting the sword /
            // recovering now, so stop translating outright instead of
            // continuing to chase a distance target.
            StopMovement();
        }


        if (Time.time - lockStartTime >= lockDuration)
        {
            RunAttackFinished();
        }
    }


    // =========================================================
    // RUN ATTACK FINISHED
    // =========================================================

    public void RunAttackFinished()
    {
        if (state != BossState.RunAttacking)
            return;


        hasRunAttackTarget = false;


        DisableWeapon();

        StopMovement();


        state =
            BossState.Recovering;


        recoveringFromBlock = false;


        BeginLock(
            recoveryTime
        );
    }


    // =========================================================
    // BLOCK
    // =========================================================

    public bool TryBlock()
    {
        if (state == BossState.Dead)
            return false;


        if (state == BossState.Blocking)
            return true;


        if (Time.time < nextBlockAllowedTime)
            return false;


        if (UnityEngine.Random.value > blockChance)
            return false;


        DisableWeapon();

        StopMovement();

        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.updatePosition = false;
        }


        state =
            BossState.Blocking;


        recoveringFromBlock = false;


        nextBlockAllowedTime =
            Time.time +
            blockCooldown;


        BeginLock(2f);


        SafeSetTrigger(
            paramNames.blockTrigger
        );


        return true;
    }


    // =========================================================
    // BLOCKING
    // =========================================================

    private void TickBlocking()
    {
        StopMovement();

        FacePlayer();


        if (Time.time - lockStartTime >= lockDuration)
        {
            BlockFinished();
        }
    }


    // =========================================================
    // BLOCK FINISHED
    // =========================================================

    public void BlockFinished()
    {
        if (state != BossState.Blocking)
            return;


        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.updatePosition = true;
        }


        DisableWeapon();

        StopMovement();


        recoveringFromBlock = true;


        state =
            BossState.Recovering;


        BeginLock(
            blockAttackDelay
        );
    }


    // =========================================================
    // RECOVERY
    // =========================================================

    private void TickRecovering()
    {
        StopMovement();

        FacePlayer();


        if (Time.time - lockStartTime < lockDuration)
            return;


        if (recoveringFromBlock)
        {
            recoveringFromBlock = false;


            float distanceToPlayer =
                player != null
                    ? HorizontalDistance(
                        transform.position,
                        player.position
                    )
                    : Mathf.Infinity;


            if (distanceToPlayer <= attackRange)
            {
                BossAttackType attack =
                    (BossAttackType)
                    UnityEngine.Random.Range(
                        0,
                        3
                    );


                StartMeleeAttack(
                    attack
                );
            }
            else
            {
                state =
                    BossState.Chasing;
            }


            return;
        }


        state =
            BossState.Chasing;
    }


    // =========================================================
    // HIT
    // =========================================================

    public void NotifyHit()
    {
        if (state == BossState.Dead)
            return;


        if (state == BossState.Blocking)
            return;


        DisableWeapon();

        StopMovement();


        hasRunAttackTarget = false;


        state =
            BossState.Hit;


        BeginLock(
            hitReactionDuration
        );


        SafeSetTrigger(
            paramNames.hitTrigger
        );
    }


    // =========================================================
    // HIT UPDATE
    // =========================================================

    private void TickHit()
    {
        StopMovement();

        FacePlayer();


        if (Time.time - lockStartTime >= lockDuration)
        {
            state =
                BossState.Chasing;
        }
    }


    // =========================================================
    // DEATH
    // =========================================================

    public void Die()
    {
        if (state == BossState.Dead)
            return;


        state =
            BossState.Dead;


        hasRunAttackTarget = false;


        DisableWeapon();


        if (agent != null &&
            agent.enabled)
        {
            agent.isStopped = true;

            agent.velocity =
                Vector3.zero;

            agent.enabled = false;
        }


        Collider col =
            GetComponent<Collider>();


        if (col != null)
        {
            col.enabled = false;
        }


        SafeSetBool(
            paramNames.deadBool,
            true
        );


        BossFightEnded?.Invoke();
    }


    // =========================================================
    // RETURN TO START
    // =========================================================

    private void StartReturning()
    {
        returningToStart = true;


        StopMovement();


        if (agent == null ||
            !agent.enabled ||
            !agent.isOnNavMesh)
        {
            return;
        }


        agent.speed =
            runSpeed;


        agent.isStopped =
            false;


        agent.SetDestination(
            originalPosition
        );
    }


    private void TickReturning()
    {
        if (player != null)
        {
            float playerDistance =
                HorizontalDistance(
                    transform.position,
                    player.position
                );


            if (playerDistance <= detectionRange)
            {
                returningToStart = false;

                return;
            }
        }


        if (agent == null ||
            !agent.enabled ||
            !agent.isOnNavMesh)
        {
            returningToStart = false;

            state =
                BossState.Idle;

            return;
        }


        float distance =
            HorizontalDistance(
                transform.position,
                originalPosition
            );


        if (distance <= returnDistance)
        {
            StopMovement();

            returningToStart = false;

            state =
                BossState.Idle;

            return;
        }


        agent.speed =
            runSpeed;


        agent.isStopped =
            false;


        agent.SetDestination(
            originalPosition
        );


        ClampAgentVelocity();


        Vector3 direction =
            agent.velocity;


        direction.y = 0f;


        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion rotation =
                Quaternion.LookRotation(
                    direction.normalized
                );


            transform.rotation =
                Quaternion.Slerp(
                    transform.rotation,
                    rotation,
                    turnSpeed *
                    Time.deltaTime
                );
        }
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


        if (direction.sqrMagnitude < 0.0001f)
            return;


        Quaternion rotation =
            Quaternion.LookRotation(
                direction.normalized
            );


        transform.rotation =
            Quaternion.Slerp(
                transform.rotation,
                rotation,
                turnSpeed *
                Time.deltaTime
            );
    }


    // =========================================================
    // DISTANCE
    // =========================================================

    private float HorizontalDistance(
        Vector3 a,
        Vector3 b)
    {
        a.y = 0f;

        b.y = 0f;


        return Vector3.Distance(
            a,
            b
        );
    }


    // =========================================================
    // LOCK
    // =========================================================

    private void BeginLock(
        float duration)
    {
        lockStartTime =
            Time.time;


        lockDuration =
            Mathf.Max(
                0.01f,
                duration
            );
    }


    // =========================================================
    // FIND PLAYER
    // =========================================================

    private void FindPlayer()
    {
        if (player != null)
            return;


        GameObject playerObject =
            GameObject.FindGameObjectWithTag(
                "Player"
            );


        if (playerObject != null)
        {
            player =
                playerObject.transform;
        }
    }


    // =========================================================
    // WEAPON
    // =========================================================

    public void EnableWeapon()
    {
        if (weaponCollider != null)
        {
            weaponCollider.EnableWeapon();
        }
    }


    public void DisableWeapon()
    {
        if (weaponCollider != null)
        {
            weaponCollider.DisableWeapon();
        }
    }


    // =========================================================
    // ANIMATOR CACHE
    // =========================================================

    private void CacheValidAnimatorParams()
    {
        validParamHashes =
            new HashSet<int>();


        foreach (
            AnimatorControllerParameter p
            in animator.parameters)
        {
            validParamHashes.Add(
                p.nameHash
            );
        }
    }


    // =========================================================
    // SAFE TRIGGER
    // =========================================================

    private void SafeSetTrigger(
        string paramName)
    {
        if (string.IsNullOrEmpty(paramName))
            return;


        int hash =
            Animator.StringToHash(
                paramName
            );


        if (validParamHashes.Contains(hash))
        {
            animator.SetTrigger(
                hash
            );
        }
    }


    // =========================================================
    // SAFE FLOAT
    // =========================================================

    private void SafeSetFloat(
        string paramName,
        float value)
    {
        if (string.IsNullOrEmpty(paramName))
            return;


        int hash =
            Animator.StringToHash(
                paramName
            );


        if (validParamHashes.Contains(hash))
        {
            animator.SetFloat(
                hash,
                value
            );
        }
    }


    // =========================================================
    // SAFE BOOL
    // =========================================================

    private void SafeSetBool(
        string paramName,
        bool value)
    {
        if (string.IsNullOrEmpty(paramName))
            return;


        int hash =
            Animator.StringToHash(
                paramName
            );


        if (validParamHashes.Contains(hash))
        {
            animator.SetBool(
                hash,
                value
            );
        }
    }
}