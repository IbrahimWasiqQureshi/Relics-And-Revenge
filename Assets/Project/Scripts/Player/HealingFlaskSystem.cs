using UnityEngine;
using TMPro;

public class HealingFlaskSystem : MonoBehaviour
{
    [Header("Player References")]
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private Animator animator;

    [Header("Flask Settings")]
    [SerializeField] private int maxFlasks = 3;
    [SerializeField] private int healAmount = 35;

    [Header("Flask Object")]
    [SerializeField] private GameObject flaskPrefab;
    [SerializeField] private Transform flaskHoldPoint;

    [Header("Flask UI")]
    [SerializeField] private GameObject flaskUI;
    [SerializeField] private TMP_Text flaskCountText;

    private int currentFlasks;
    private bool hasFlask = false;
    private bool isDrinking = false;

    private GameObject spawnedFlask;

    private PlayerController playerController;
    private StarterAssets.ThirdPersonController thirdPersonController;

    private void Awake()
    {
        playerController = GetComponent<PlayerController>();
        thirdPersonController = GetComponent<StarterAssets.ThirdPersonController>();
    }

    private void Start()
    {
        currentFlasks = 0;
        hasFlask = false;
        isDrinking = false;

        if (flaskUI != null)
            flaskUI.SetActive(false);

        UpdateFlaskUI();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            TryDrinkFlask();
        }
    }

    public void InterruptHealing()
    {
        if (!isDrinking)
            return;

        Debug.Log("Healing interrupted by enemy attack!");

        // Stop drinking state
        isDrinking = false;

        // Remove flask from hand
        RemoveFlask();

        // Cancel drinking animation trigger
        if (animator != null)
        {
            animator.ResetTrigger("DrinkPotion");
        }

        // Re-enable player movement
        if (playerController != null)
            playerController.enabled = true;

        if (thirdPersonController != null)
            thirdPersonController.enabled = true;
    }

    // =========================================================
    // PICKUP
    // =========================================================

    public void PickUpFlask()
    {
        if (hasFlask)
            return;

        hasFlask = true;
        currentFlasks = maxFlasks;

        if (flaskUI != null)
            flaskUI.SetActive(true);

        UpdateFlaskUI();

        Debug.Log("Healing Flask picked up! Uses: " + currentFlasks);
    }

    // =========================================================
    // DRINK
    // =========================================================

    private void TryDrinkFlask()
    {
        if (!hasFlask)
            return;

        if (isDrinking)
            return;

        if (currentFlasks <= 0)
            return;

        if (playerHealth != null && playerHealth.IsDead())
            return;

        if (playerHealth != null && playerHealth.IsFullHealth())
        {
            Debug.Log("Health is already full.");
            return;
        }

        if (IsSwordEquipped())
        {
            Debug.Log("Cannot drink while sword is equipped.");
            return;
        }

        StartDrinking();
    }

    // =========================================================
    // SWORD CHECK
    // =========================================================

    private bool IsSwordEquipped()
    {
        if (playerController == null)
            return false;

        return playerController.isEquipped || playerController.isEquipping;
    }

    // =========================================================
    // START DRINKING
    // =========================================================

    private void StartDrinking()
    {
        isDrinking = true;

        if (playerController != null)
            playerController.enabled = false;

        if (thirdPersonController != null)
            thirdPersonController.enabled = false;

        if (animator != null)
        {
            animator.ResetTrigger("DrinkPotion");
            animator.SetTrigger("DrinkPotion");
        }

        Debug.Log("Drinking healing flask...");
    }

    // =========================================================
    // ANIMATION EVENT - SPAWN FLASK
    // =========================================================

    public void SpawnFlask()
    {
        if (!isDrinking)
            return;

        if (flaskPrefab == null)
        {
            Debug.LogWarning("Flask Prefab is not assigned.");
            return;
        }

        if (flaskHoldPoint == null)
        {
            Debug.LogWarning("Flask Hold Point is not assigned.");
            return;
        }

        if (spawnedFlask != null)
            return;

        spawnedFlask = Instantiate(
            flaskPrefab,
            flaskHoldPoint.position,
            flaskHoldPoint.rotation,
            flaskHoldPoint
        );

        spawnedFlask.transform.localPosition = Vector3.zero;
        spawnedFlask.transform.localRotation = Quaternion.identity;

        Debug.Log("Flask spawned in right hand.");
    }

    // =========================================================
    // ANIMATION EVENT - HEAL
    // =========================================================

    public void HealFromFlask()
    {
        if (!isDrinking)
            return;

        if (playerHealth == null)
            return;

        if (currentFlasks <= 0)
            return;

        playerHealth.Heal(healAmount);

        currentFlasks--;

        UpdateFlaskUI();

        Debug.Log("Flask used. Remaining: " + currentFlasks);
    }

    // =========================================================
    // ANIMATION EVENT - REMOVE FLASK
    // =========================================================

    public void RemoveFlask()
    {
        if (spawnedFlask != null)
        {
            Destroy(spawnedFlask);
            spawnedFlask = null;
        }

        Debug.Log("Flask removed from hand.");
    }

    // =========================================================
    // ANIMATION EVENT - FINISH DRINKING
    // =========================================================

    public void FinishDrinking()
    {
        RemoveFlask();

        isDrinking = false;

        if (playerController != null)
            playerController.enabled = true;

        if (thirdPersonController != null)
            thirdPersonController.enabled = true;

        Debug.Log("Finished drinking.");
    }

    // =========================================================
    // UI
    // =========================================================

    private void UpdateFlaskUI()
    {
        if (flaskCountText != null)
        {
            // Displays: x0, x1, x2, x3
            flaskCountText.text = "x" + currentFlasks.ToString();
        }
    }

    // =========================================================
    // CHECKPOINT REFILL
    // =========================================================

    public void RefillFlask()
    {
        if (!hasFlask)
            return;

        currentFlasks = maxFlasks;

        UpdateFlaskUI();

        Debug.Log("Healing Flask refilled to " + maxFlasks);
    }

    // =========================================================
    // GETTERS
    // =========================================================

    public bool HasFlask()
    {
        return hasFlask;
    }

    public int GetCurrentFlasks()
    {
        return currentFlasks;
    }
}