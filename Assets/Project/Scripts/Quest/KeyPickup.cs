using UnityEngine;

public class KeyPickup : MonoBehaviour
{
    public static bool HasKey;

    [SerializeField] private KeyCode pickupKey = KeyCode.G;
    [SerializeField] private float pickupRange = 3f;
    [SerializeField] private float rotateSpeed = 90f;
    [SerializeField] private string promptText = "Press G to pick up the magical key";

    private BossHealth boss;
    private Renderer[] renderers;
    private Transform player;
    private bool dropped;

    private void Awake()
    {
        HasKey = false;
        boss = GetComponentInParent<BossHealth>();
        renderers = GetComponentsInChildren<Renderer>();
    }

    private void Update()
    {
        if (!dropped)
        {
            if (boss != null && boss.IsDead)
            {
                Drop();
            }
            return;
        }

        transform.Rotate(0f, rotateSpeed * Time.deltaTime, 0f, Space.World);

        if (player == null)
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p != null) player = p.transform;
            else return;
        }

        float distance = Vector3.Distance(player.position, transform.position);

        if (distance <= pickupRange)
        {
            GamePrompt.Show(promptText);

            if (Input.GetKeyDown(pickupKey))
            {
                HasKey = true;
                Destroy(gameObject);
            }
        }
    }

    private void Drop()
    {
        dropped = true;
        transform.SetParent(null);
        SetVisible(true);
    }

    private void SetVisible(bool visible)
    {
        foreach (Renderer r in renderers)
        {
            r.enabled = visible;
        }
    }
}
