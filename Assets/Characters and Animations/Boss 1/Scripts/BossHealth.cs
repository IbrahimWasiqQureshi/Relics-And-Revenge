using UnityEngine;

[RequireComponent(typeof(BossController))]
public class BossHealth : MonoBehaviour, IDamageable
{
    [Header("Health")]
    [SerializeField] private int maxHealth = 1000;

    [SerializeField] private BossHealthBarUI healthBar;


    [Header("Death")]
    [SerializeField] private float destroyAfter = 3f;


    private int currentHealth;

    private bool isDead;

    private BossController controller;

    private Animator animator;


    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        currentHealth = maxHealth;

        controller =
            GetComponent<BossController>();

        animator =
            GetComponent<Animator>();


        // =====================================================
        // SETUP HEALTH BAR
        // =====================================================

        if (healthBar != null)
        {
            healthBar.SetMaxHealth(
                maxHealth
            );

            healthBar.SetHealth(
                currentHealth
            );

            // IMPORTANT:
            // We DO NOT show the health bar here.
            //
            // BossController will show it when
            // the player enters detection range.
        }
    }


    // =========================================================
    // TAKE DAMAGE
    // =========================================================

    public void TakeDamage(int damage)
    {
        if (isDead)
            return;

        if (damage <= 0)
            return;


        // =====================================================
        // BLOCK
        // =====================================================

        if (controller != null &&
            controller.TryBlock())
        {
            Debug.Log(
                gameObject.name +
                " BLOCKED the attack!"
            );

            return;
        }


        // =====================================================
        // DAMAGE
        // =====================================================

        currentHealth -= damage;

        currentHealth =
            Mathf.Max(
                currentHealth,
                0
            );


        Debug.Log(
            gameObject.name +
            " HP : " +
            currentHealth +
            "/" +
            maxHealth
        );


        // =====================================================
        // UPDATE HEALTH BAR
        // =====================================================

        if (healthBar != null)
        {
            healthBar.SetHealth(
                currentHealth
            );
        }


        // =====================================================
        // DEATH
        // =====================================================

        if (currentHealth <= 0)
        {
            Die();

            return;
        }


        // =====================================================
        // HIT REACTION
        // =====================================================

        if (controller != null)
        {
            controller.NotifyHit();
        }


        if (animator != null)
        {
            animator.ResetTrigger("Hit");

            animator.SetTrigger("Hit");
        }
    }


    // =========================================================
    // SHOW HEALTH BAR
    // =========================================================

    public void ShowHealthBar()
    {
        if (isDead)
            return;

        if (healthBar != null)
        {
            healthBar.ShowBar();
        }
    }


    // =========================================================
    // DEATH
    // =========================================================

    private void Die()
    {
        if (isDead)
            return;

        isDead = true;

        currentHealth = 0;


        Debug.Log(
            gameObject.name +
            " DIED."
        );


        // =====================================================
        // HEALTH BAR
        // =====================================================

        if (healthBar != null)
        {
            healthBar.SetHealth(0);
        }


        // =====================================================
        // DEATH ANIMATION
        // =====================================================

        if (animator != null)
        {
            animator.ResetTrigger("Death");

            animator.SetTrigger("Death");
        }


        // =====================================================
        // COLLIDER
        // =====================================================

        Collider col =
            GetComponent<Collider>();

        if (col != null)
        {
            col.enabled = false;
        }


        // =====================================================
        // BOSS CONTROLLER
        // =====================================================

        if (controller != null)
        {
            controller.Die();
        }


        // =====================================================
        // HIDE HEALTH BAR
        // =====================================================

        if (healthBar != null)
        {
            healthBar.HideBarDelayed(5f);
        }


        // =====================================================
        // DESTROY BOSS
        // =====================================================

        Destroy(
            gameObject,
            destroyAfter
        );
    }


    // =========================================================
    // GETTERS
    // =========================================================

    public int CurrentHealth
    {
        get
        {
            return currentHealth;
        }
    }


    public int MaxHealth
    {
        get
        {
            return maxHealth;
        }
    }


    public bool IsDead
    {
        get
        {
            return isDead;
        }
    }
}