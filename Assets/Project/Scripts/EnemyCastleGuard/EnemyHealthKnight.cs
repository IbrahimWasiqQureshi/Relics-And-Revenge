using UnityEngine;

public class EnemyHealth : MonoBehaviour, IDamageable
{
    [Header("Health")]
    [SerializeField] private int maxHealth = 100;

    [SerializeField] private HealthBarUI healthBar;

    private int currentHealth;

    [Header("Animator")]
    [SerializeField] private Animator animator;

    [Header("Death")]
    [SerializeField] private float destroyAfter = 3f;

    private bool isDead = false;

    private void Start()
    {
        currentHealth = maxHealth;

        if (animator == null)
            animator = GetComponent<Animator>();

        if (healthBar != null)
        {
            healthBar.SetMaxHealth(maxHealth);
            healthBar.SetHealth(currentHealth);
        }
    }

    public void TakeDamage(int damage)
    {
        if (isDead)
            return;

        currentHealth -= damage;
        currentHealth = Mathf.Max(currentHealth, 0);

        Debug.Log(gameObject.name + " HP : " + currentHealth);

        if (healthBar != null)
            if (healthBar != null)
        {
            healthBar.ShowBar();
            healthBar.SetHealth(currentHealth);
        }

        EnemyAI ai = GetComponent<EnemyAI>();

        if (ai != null)
        {
            ai.InterruptAttack();

            // Start the knockback + hit reaction. This call was missing, so knockback never ran.
            if (currentHealth > 0)
                ai.TakeHit();
        }

        if (currentHealth > 0)
        {
            animator.ResetTrigger("Hit");
            animator.SetTrigger("Hit");
        }
        else
        {
            Die();
        }
    }

    private void Die()
    {
        isDead = true;

        // Let go of the lock-on right away
        LockOnTarget lockOnTarget = GetComponent<LockOnTarget>();

        if (lockOnTarget != null)
            lockOnTarget.OnTargetDied();

        animator.ResetTrigger("Death");
        animator.SetTrigger("Death");

        Collider col = GetComponent<Collider>();
        if (col != null)
            col.enabled = false;

        EnemyAI ai = GetComponent<EnemyAI>();
        if (ai != null)
            ai.Die();

        // Health bar disappears right away
        if (healthBar != null)
            healthBar.HideBar();

        Destroy(gameObject, destroyAfter);
    }
}