using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    [Header("Checkpoint")]
    [SerializeField] private Transform respawnPoint;

    private bool isActivated = false;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        ActivateCheckpoint(other);
    }

    private void ActivateCheckpoint(Collider player)
    {
        isActivated = true;

        if (CheckpointManager.Instance != null)
        {
            CheckpointManager.Instance.SetCheckpoint(respawnPoint);
        }

        HealingFlaskSystem flaskSystem =
            player.GetComponent<HealingFlaskSystem>();

        if (flaskSystem != null)
        {
            flaskSystem.RefillFlask();
        }

        Debug.Log("Checkpoint Activated");
    }
}