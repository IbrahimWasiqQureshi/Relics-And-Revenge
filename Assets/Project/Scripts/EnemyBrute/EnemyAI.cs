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

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player").transform;
    }

    void Update()
    {
        if (isDead)
            return;

        float distance = Vector3.Distance(transform.position, player.position);

        //-------------------------------------------------
        // Outside Detection Range
        //-------------------------------------------------

        if (distance > detectionRange)
        {
            agent.ResetPath();
            animator.SetFloat("Speed", 0);
            return;
        }

        //-------------------------------------------------
        // Attack
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
        // Chase
        //-------------------------------------------------

        agent.SetDestination(player.position);

        animator.SetFloat("Speed", agent.velocity.magnitude);
    }

    void Attack()
    {
        isAttacking = true;

        agent.isStopped = true;

        nextAttackTime = Time.time + attackCooldown;

        animator.ResetTrigger("Attack");
        animator.SetTrigger("Attack");
    }

    public void EnableWeapon()
    {
        weaponCollider.EnableWeapon();
    }

    public void DisableWeapon()
    {
        weaponCollider.DisableWeapon();
    }

    public void AttackFinished()
    {
        isAttacking = false;

        agent.isStopped = false;
    }

    void FacePlayer()
    {
        Vector3 direction = player.position - transform.position;

        direction.y = 0;

        if (direction.sqrMagnitude < 0.001f)
            return;

        Quaternion rotation = Quaternion.LookRotation(direction);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            rotation,
            8f * Time.deltaTime);
    }

    public void Die()
    {
        isDead = true;

        agent.isStopped = true;
        agent.enabled = false;
    }
    public void InterruptAttack()
    {
        isAttacking = false;

        DisableWeapon();
    }
}