using UnityEngine;
using UnityEngine.AI;

public class NPCController : MonoBehaviour
{
    [Header("Patrol Points")]
    [SerializeField] private Transform[] patrolPoints;

    [Header("Player Weapon Colliders")]
    [SerializeField] private WeaponCollider[] playerWeaponColliders;

    [Header("Enemy Weapon Colliders")]
    [SerializeField] private WeaponCollider[] enemyWeaponColliders;

    [Header("Movement")]
    [SerializeField] private float walkSpeed = 1.5f;
    [SerializeField] private float runSpeed = 4f;
    [SerializeField] private float waitTime = 1f;

    [Header("Combat")]
    [SerializeField] private int hitsToDie = 1;
    [SerializeField] private float destroyDelay = 5f;

    private NavMeshAgent agent;
    private Animator animator;

    private int currentPatrolPoint;
    private int hitCount;

    private float waitTimer;

    private bool waiting;
    private bool running;
    private bool fleeing;
    private bool dead;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        agent.updatePosition = true;
        agent.updateRotation = true;
        agent.stoppingDistance = 0.1f;

        animator.applyRootMotion = false;
    }

    private void Start()
    {
        if (patrolPoints == null || patrolPoints.Length == 0)
            return;

        if (!agent.isOnNavMesh)
            return;

        GoToPoint();
    }

    private void Update()
    {
        if (dead || !agent.isOnNavMesh)
            return;

        if (waiting)
        {
            animator.SetFloat("Speed", 0f);

            waitTimer += Time.deltaTime;

            if (waitTimer >= waitTime)
            {
                waiting = false;
                waitTimer = 0f;
                MoveToNextPoint();
            }

            return;
        }

        if (!agent.pathPending &&
            agent.hasPath &&
            agent.remainingDistance <= agent.stoppingDistance + 0.15f)
        {
            ReachedPoint();
            return;
        }

        animator.SetFloat("Speed", running ? 2f : 1f);
    }

    private void GoToPoint()
    {
        if (patrolPoints == null || patrolPoints.Length == 0)
            return;

        if (currentPatrolPoint >= patrolPoints.Length)
            currentPatrolPoint = 0;

        Transform target = patrolPoints[currentPatrolPoint];

        if (target == null)
            return;

        agent.speed = running ? runSpeed : walkSpeed;
        agent.isStopped = false;
        agent.SetDestination(target.position);
    }

    private void ReachedPoint()
    {
        agent.isStopped = true;
        animator.SetFloat("Speed", 0f);

        if (fleeing)
        {
            fleeing = false;
            running = false;

            agent.speed = walkSpeed;

            MoveToNextPoint();

            return;
        }

        waiting = true;
        waitTimer = 0f;
    }

    private void MoveToNextPoint()
    {
        currentPatrolPoint++;

        if (currentPatrolPoint >= patrolPoints.Length)
            currentPatrolPoint = 0;

        GoToPoint();
    }

    public void TakeHit(WeaponCollider weapon)
    {
        if (dead || !IsValidWeapon(weapon))
            return;

        hitCount++;

        if (hitCount >= hitsToDie)
        {
            Die();
            return;
        }

        if (hitCount == 1)
        {
            RunToFarthestPoint();
        }
    }

    private bool IsValidWeapon(WeaponCollider weapon)
    {
        if (weapon == null)
            return false;

        if (playerWeaponColliders != null)
        {
            foreach (WeaponCollider playerWeapon in playerWeaponColliders)
            {
                if (playerWeapon == weapon)
                    return true;
            }
        }

        if (enemyWeaponColliders != null)
        {
            foreach (WeaponCollider enemyWeapon in enemyWeaponColliders)
            {
                if (enemyWeapon == weapon)
                    return true;
            }
        }

        return false;
    }

    private void RunToFarthestPoint()
    {
        if (patrolPoints == null || patrolPoints.Length == 0)
            return;

        int farthestPoint = GetFarthestPoint();

        if (farthestPoint == -1)
            return;

        waiting = false;
        waitTimer = 0f;

        fleeing = true;
        running = true;

        currentPatrolPoint = farthestPoint;

        agent.speed = runSpeed;
        agent.isStopped = false;
        agent.SetDestination(
            patrolPoints[currentPatrolPoint].position
        );

        animator.SetFloat("Speed", 2f);
    }

    private int GetFarthestPoint()
    {
        int farthestIndex = -1;
        float farthestDistance = -1f;

        for (int i = 0; i < patrolPoints.Length; i++)
        {
            if (patrolPoints[i] == null)
                continue;

            float distance = Vector3.Distance(
                transform.position,
                patrolPoints[i].position
            );

            if (distance > farthestDistance)
            {
                farthestDistance = distance;
                farthestIndex = i;
            }
        }

        return farthestIndex;
    }

    private void Die()
    {
        dead = true;

        agent.isStopped = true;
        agent.ResetPath();

        animator.SetFloat("Speed", 0f);
        animator.ResetTrigger("Death");
        animator.SetTrigger("Death");

        Destroy(gameObject, destroyDelay);

    }

    public bool IsDead()
    {
        return dead;
    }

    public int GetHitCount()
    {
        return hitCount;
    }
}