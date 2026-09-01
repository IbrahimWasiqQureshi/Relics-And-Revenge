using UnityEngine;

public class CheckpointManager : MonoBehaviour
{
    public static CheckpointManager Instance { get; private set; }

    private Transform currentCheckpoint;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SetCheckpoint(Transform checkpoint)
    {
        currentCheckpoint = checkpoint;
        Debug.Log("New checkpoint saved.");
    }

    public void RespawnPlayer(GameObject player)
    {
        if (currentCheckpoint == null)
        {
            Debug.LogWarning("No checkpoint has been activated.");
            return;
        }

        CharacterController controller =
            player.GetComponent<CharacterController>();

        if (controller != null)
            controller.enabled = false;

        player.transform.position = currentCheckpoint.position;
        player.transform.rotation = currentCheckpoint.rotation;

        if (controller != null)
            controller.enabled = true;
    }
}