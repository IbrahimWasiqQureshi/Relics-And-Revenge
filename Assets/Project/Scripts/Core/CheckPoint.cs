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

        ActivateCheckpoint();
    }

    private void ActivateCheckpoint()
    {
        isActivated = true;

        if (CheckpointManager.Instance != null)
        {
            CheckpointManager.Instance.SetCheckpoint(respawnPoint);
        }

        Debug.Log("Checkpoint Activated");
    }
}