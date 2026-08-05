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
            healthBar.ShowBar();
            healthBar.SetHealth(currentHealth);

        EnemyAI ai = GetComponent<EnemyAI>();

        if (ai != null)
        {
            ai.InterruptAttack();
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

        animator.ResetTrigger("Death");
        animator.SetTrigger("Death");

        Collider col = GetComponent<Collider>();
        if (col != null)
            col.enabled = false;

        EnemyAI ai = GetComponent<EnemyAI>();
        if (ai != null)
            ai.Die();

        if (healthBar != null)
            healthBar.HideBarDelayed(2f);

        Destroy(gameObject, destroyAfter);
    }
}