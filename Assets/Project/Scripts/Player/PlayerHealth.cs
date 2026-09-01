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

    private void Awake()
    {
        currentHealth = maxHealth;

        animator = GetComponent<Animator>();
        playerController = GetComponent<PlayerController>();
        thirdPersonController = GetComponent<StarterAssets.ThirdPersonController>();
        characterController = GetComponent<CharacterController>();
    }

    private void Start()
    {
        healthBar.SetMaxHealth(maxHealth);
        healthBar.SetHealth(currentHealth);
    }

    public void TakeDamage(int damage)
    {
        if (isDead || isInvincible)
            return;

        // Blocking
        if (playerController != null &&
            playerController.isBlocking &&
            ShieldBlock.IsBlocking)
        {
            Debug.Log("Attack Blocked");
            return;
        }

        currentHealth -= damage;
        currentHealth = Mathf.Max(currentHealth, 0);

        healthBar.SetHealth(currentHealth);

        Debug.Log("Player HP: " + currentHealth);

        // Player died
        if (currentHealth <= 0)
        {
            Die();
            return;
        }

        // Hit animation
        if (playerController == null || !playerController.isBlocking)
        {
            // Cancel any in-progress action (equip/attack) so a hit can never
            // leave the player permanently stuck if it interrupts that animation
            // before its "finished" animation event has a chance to fire.
            if (playerController != null)
                playerController.CancelActionStates();

            animator.ResetTrigger("Hit");
            animator.SetTrigger("Hit");
        }

        // Temporary invincibility after being hit
        StartCoroutine(InvincibilityFrames());
    }

    private void Die()
    {
        if (isDead)
            return;

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
        // Give the death animation time to play
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

        Debug.Log("Player respawned at checkpoint.");
    }

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