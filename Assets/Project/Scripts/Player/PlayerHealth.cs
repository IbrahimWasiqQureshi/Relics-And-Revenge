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

    [SerializeField]
    private float invincibleTime = 0.5f;

    private bool isInvincible = false;

    private Animator animator;

    [SerializeField] private SkinnedMeshRenderer playerRenderer;

    private void Awake()
    {
        currentHealth = maxHealth;
        animator = GetComponent<Animator>();
    }

    private void Start()
    {
        healthBar.SetMaxHealth(maxHealth);
        healthBar.SetHealth(currentHealth);
    }

    public void TakeDamage(int damage)
    {
        if (isDead)
            return;

        PlayerController player = GetComponent<PlayerController>();

        if (player.isBlocking && ShieldBlock.IsBlocking)
        {
            Debug.Log("Attack Blocked");
            return;
        }

        currentHealth -= damage;

        currentHealth = Mathf.Max(currentHealth, 0);

        healthBar.SetHealth(currentHealth);

        Debug.Log("Player HP: " + currentHealth);

        if (currentHealth <= 0)
        {
            Die();
            return;
        }

        // Don't play hit animation while blocking
        if (player == null || !player.isBlocking)
        {
            animator.ResetTrigger("Hit");
            animator.SetTrigger("Hit");
        }
    }

    private void Die()
    {
        isDead = true;

        animator.applyRootMotion = true;
        isInvincible = true;
        animator.SetTrigger("Death");

        GetComponent<PlayerController>().enabled = false;
        GetComponent<StarterAssets.ThirdPersonController>().enabled = false;

        StartCoroutine(DisableCharacterControllerAfterAnimation());
    }

    private IEnumerator InvincibilityFrames()
    {
        isInvincible = true;

        float elapsed = 0f;

        while (elapsed < invincibleTime)
        {
            playerRenderer.enabled = false;

            yield return new WaitForSeconds(0.08f);

            playerRenderer.enabled = true;

            yield return new WaitForSeconds(0.08f);

            elapsed += 0.16f;
        }

        playerRenderer.enabled = true;

        isInvincible = false;
    }

    private IEnumerator DisableCharacterControllerAfterAnimation()
    {
        yield return new WaitForSeconds(0.2f);

        GetComponent<CharacterController>().enabled = false;
    }
}