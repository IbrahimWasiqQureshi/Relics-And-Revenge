using UnityEngine;
using System.Collections;

public class PlayerHealth : MonoBehaviour, IDamageable
{
    [SerializeField] private HealthBarUI healthBar;

    [Header("Health")]
    [SerializeField] private int maxHealth = 100;

    private int currentHealth;
    private bool isDead = false;

    [Header("Invincibility")]
    [SerializeField] private float invincibleTime = 0.5f;

    private bool isInvincible = false;

    private Animator animator;

    [SerializeField] private SkinnedMeshRenderer playerRenderer;

    private PlayerController playerController;
    private StarterAssets.ThirdPersonController thirdPersonController;
    private CharacterController characterController;

    // Healing system
    private HealingFlaskSystem healingFlaskSystem;

    private void Awake()
    {
        currentHealth = maxHealth;

        animator = GetComponent<Animator>();
        playerController = GetComponent<PlayerController>();
        thirdPersonController = GetComponent<StarterAssets.ThirdPersonController>();
        characterController = GetComponent<CharacterController>();

        healingFlaskSystem = GetComponent<HealingFlaskSystem>();
    }

    private void Start()
    {
        healthBar.SetMaxHealth(maxHealth);
        healthBar.SetHealth(currentHealth);
    }

    // =========================================================
    // DAMAGE
    // =========================================================

    public void TakeDamage(int damage)
    {
        if (isDead || isInvincible)
            return;

        // =====================================================
        // BLOCKING
        // =====================================================

        if (playerController != null &&
            playerController.isBlocking &&
            ShieldBlock.IsBlocking)
        {
            Debug.Log("Attack Blocked");
            return;
        }

        // =====================================================
        // INTERRUPT HEALING
        // =====================================================

        // If the player is drinking, getting hit cancels it.
        if (healingFlaskSystem != null)
        {
            healingFlaskSystem.InterruptHealing();
        }

        // =====================================================
        // APPLY DAMAGE
        // =====================================================

        currentHealth -= damage;
        currentHealth = Mathf.Max(currentHealth, 0);

        healthBar.SetHealth(currentHealth);

        Debug.Log("Player HP: " + currentHealth);

        // =====================================================
        // DEATH
        // =====================================================

        if (currentHealth <= 0)
        {
            Die();
            return;
        }

        // =====================================================
        // HIT ANIMATION
        // =====================================================

        if (playerController != null)
            playerController.CancelActionStates();

        if (animator != null)
        {
            // Make sure healing animation trigger is not waiting
            animator.ResetTrigger("DrinkPotion");

            // Play hit animation
            animator.ResetTrigger("Hit");
            animator.SetTrigger("Hit");
        }

        // =====================================================
        // TEMPORARY INVINCIBILITY
        // =====================================================

        StartCoroutine(InvincibilityFrames());
    }

    // =========================================================
    // HEALING
    // =========================================================

    public void Heal(int amount)
    {
        if (isDead)
            return;

        currentHealth += amount;
        currentHealth = Mathf.Min(currentHealth, maxHealth);

        healthBar.SetHealth(currentHealth);

        Debug.Log("Player healed. HP: " + currentHealth);
    }

    public bool IsFullHealth()
    {
        return currentHealth >= maxHealth;
    }

    public bool IsDead()
    {
        return isDead;
    }

    // =========================================================
    // DEATH
    // =========================================================

    private void Die()
    {
        if (isDead)
            return;

        // Make absolutely sure healing is cancelled on death
        if (healingFlaskSystem != null)
        {
            healingFlaskSystem.InterruptHealing();
        }

        isDead = true;
        isInvincible = true;

        animator.applyRootMotion = true;
        animator.SetTrigger("Death");

        // Disable player movement
        if (playerController != null)
            playerController.enabled = false;

        if (thirdPersonController != null)
            thirdPersonController.enabled = false;

        // Disable CharacterController after death animation starts
        StartCoroutine(DisableCharacterControllerAfterAnimation());

        // Wait for death animation, then respawn
        StartCoroutine(RespawnAfterDeath());
    }

    private IEnumerator RespawnAfterDeath()
    {
        yield return new WaitForSeconds(2f);

        if (CheckpointManager.Instance != null)
        {
            CheckpointManager.Instance.RespawnPlayer(gameObject);

            ResetPlayerAfterRespawn();
        }
        else
        {
            Debug.LogWarning("CheckpointManager not found. Player cannot respawn.");
        }
    }

    private void ResetPlayerAfterRespawn()
    {
        currentHealth = maxHealth;
        healthBar.SetHealth(currentHealth);

        isDead = false;
        isInvincible = false;

        // Reset animation system
        animator.ResetTrigger("Death");
        animator.ResetTrigger("Hit");
        animator.ResetTrigger("DrinkPotion");

        animator.applyRootMotion = false;
        animator.Rebind();
        animator.Update(0f);

        // Re-enable CharacterController
        if (characterController != null)
            characterController.enabled = true;

        // Re-enable player movement
        if (playerController != null)
            playerController.enabled = true;

        if (thirdPersonController != null)
            thirdPersonController.enabled = true;

        // Refill healing flask
        if (healingFlaskSystem != null)
            healingFlaskSystem.RefillFlask();

        Debug.Log("Player respawned at checkpoint.");
    }

    // =========================================================
    // INVINCIBILITY
    // =========================================================

    private IEnumerator InvincibilityFrames()
    {
        isInvincible = true;

        float elapsed = 0f;

        while (elapsed < invincibleTime)
        {
            if (playerRenderer != null)
                playerRenderer.enabled = false;

            yield return new WaitForSeconds(0.08f);

            if (playerRenderer != null)
                playerRenderer.enabled = true;

            yield return new WaitForSeconds(0.08f);

            elapsed += 0.16f;
        }

        if (playerRenderer != null)
            playerRenderer.enabled = true;

        isInvincible = false;
    }

    private IEnumerator DisableCharacterControllerAfterAnimation()
    {
        yield return new WaitForSeconds(0.2f);

        if (characterController != null)
            characterController.enabled = false;
    }
}