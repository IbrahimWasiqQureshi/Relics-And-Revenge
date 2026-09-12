using UnityEngine;
using TMPro;

public class HealingFlaskPickup : MonoBehaviour
{
    [Header("Pickup Settings")]
    [SerializeField] private KeyCode pickupKey = KeyCode.R;

    [Header("Prompt UI")]
    [SerializeField] private GameObject promptUI;
    [SerializeField] private TMP_Text promptText;
    [SerializeField] private string promptMessage = "Press R to pick up healing flask\nHeals you 3 times";

    private HealingFlaskSystem flaskSystem;
    private bool playerInRange = false;

    private void Start()
    {
        if (promptText != null)
            promptText.text = promptMessage;

        if (promptUI != null)
            promptUI.SetActive(false);
    }

    private void Update()
    {
        if (!playerInRange)
            return;

        if (flaskSystem == null)
            return;

        if (flaskSystem.HasFlask())
        {
            HidePrompt();
            return;
        }

        if (Input.GetKeyDown(pickupKey))
        {
            flaskSystem.PickUpFlask();
            HidePrompt();
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        HealingFlaskSystem system = other.GetComponent<HealingFlaskSystem>();

        if (system == null)
            return;

        if (system.HasFlask())
            return;

        flaskSystem = system;
        playerInRange = true;

        ShowPrompt();
    }

    private void OnTriggerExit(Collider other)
    {
        HealingFlaskSystem system = other.GetComponent<HealingFlaskSystem>();

        if (system == null)
            return;

        playerInRange = false;
        flaskSystem = null;

        HidePrompt();
    }

    private void ShowPrompt()
    {
        if (promptUI != null)
            promptUI.SetActive(true);
    }

    private void HidePrompt()
    {
        if (promptUI != null)
            promptUI.SetActive(false);
    }
}
