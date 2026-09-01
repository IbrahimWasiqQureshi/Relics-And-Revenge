using UnityEngine;

public class EnemyHealthKnight : MonoBehaviour, IDamageable
{
    [Header("Health")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private HealthBarUI healthBar;

    private int currentHealth;
    private KnightEnemyAI enemyAI;


    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        currentHealth = maxHealth;

        enemyAI = GetComponent<KnightEnemyAI>();

        if (healthBar != null)
        {
            healthBar.SetMaxHealth(maxHealth);
            healthBar.SetHealth(currentHealth);
        }
    }


    // =========================================================
    // TAKE DAMAGE
    // =========================================================

    public void TakeDamage(int damage)
    {
        if (currentHealth <= 0)
            return;


        currentHealth -= damage;

        currentHealth = Mathf.Max(
            currentHealth,
            0
        );


        Debug.Log(
            gameObject.name +
            " HP : " +
            currentHealth
        );


        // =====================================================
        // HEALTH BAR
        // =====================================================

        if (healthBar != null)
        {
            healthBar.ShowBar();

            healthBar.SetHealth(
                currentHealth
            );
        }


        // =====================================================
        // TELL AI ABOUT DAMAGE
        // =====================================================

        if (enemyAI != null)
        {
            if (currentHealth <= 0)
            {
                enemyAI.Die();
            }
            else
            {
                enemyAI.TakeHit();
            }
        }
    }
}