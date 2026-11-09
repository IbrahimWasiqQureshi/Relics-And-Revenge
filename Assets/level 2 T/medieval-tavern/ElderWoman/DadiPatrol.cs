using UnityEngine;
using UnityEngine.AI;

public class DadiPatrol : MonoBehaviour
{
    public Transform patrolPointA;
    public Transform patrolPointB;
    public float waitTime = 3f;

    private NavMeshAgent agent;
    private Animator animator;

    private bool goingToB = true;
    private bool waiting;
    private float waitTimer;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();

        GoToNextPoint();
    }

    void Update()
    {
        if (waiting)
        {
            animator.SetBool("IsWalking", false);

            waitTimer -= Time.deltaTime;

            if (waitTimer <= 0)
            {
                waiting = false;
                goingToB = !goingToB;
                GoToNextPoint();
            }

            return;
        }

        animator.SetBool("IsWalking", true);

        if (!agent.pathPending &&
            agent.remainingDistance <= agent.stoppingDistance)
        {
            agent.isStopped = true;
            waiting = true;
            waitTimer = waitTime;
            animator.SetBool("IsWalking", false);
        }
    }

    void GoToNextPoint()
    {
        agent.isStopped = false;

        if (goingToB)
            agent.SetDestination(patrolPointB.position);
        else
            agent.SetDestination(patrolPointA.position);
    }
}