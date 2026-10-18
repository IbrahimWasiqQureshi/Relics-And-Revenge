using System.Collections;
using UnityEngine;

public class DoorLock : MonoBehaviour
{
    [SerializeField] private KeyCode openKey = KeyCode.G;
    [SerializeField] private float interactRange = 3f;
    [SerializeField] private float openAngle = 90f;
    [SerializeField] private float openDuration = 1.5f;
    [SerializeField] private string lockedText = "You need the magical key first";
    [SerializeField] private string openText = "Press G to open the door";

    private Collider doorCollider;
    private Transform player;
    private bool opened;

    private void Awake()
    {
        doorCollider = GetComponent<Collider>();
    }

    private void Update()
    {
        if (opened) return;

        if (player == null)
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p != null) player = p.transform;
            else return;
        }

        Vector3 closest = doorCollider != null
            ? doorCollider.ClosestPoint(player.position)
            : transform.position;

        if (Vector3.Distance(player.position, closest) > interactRange) return;

        if (!KeyPickup.HasKey)
        {
            GamePrompt.Show(lockedText);
            return;
        }

        GamePrompt.Show(openText);

        if (Input.GetKeyDown(openKey))
        {
            opened = true;
            StartCoroutine(OpenDoor());
        }
    }

    private IEnumerator OpenDoor()
    {
        Bounds b = doorCollider.bounds;
        bool widthAlongX = b.size.x > b.size.z;
        Vector3 widthAxis = widthAlongX ? Vector3.right : Vector3.forward;
        Vector3 normal = widthAlongX ? Vector3.forward : Vector3.right;
        float halfWidth = (widthAlongX ? b.size.x : b.size.z) * 0.5f;

        float side = Mathf.Sign(Vector3.Dot(player.position - b.center, normal));
        if (side == 0f) side = 1f;

        Vector3 viewDir = -side * normal;
        Vector3 left = -Vector3.Cross(Vector3.up, viewDir);
        Vector3 hinge = b.center + left * halfWidth;
        hinge.y = transform.position.y;

        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;

        doorCollider.enabled = false;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / openDuration;
            float angle = -openAngle * Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));
            Quaternion q = Quaternion.AngleAxis(angle, Vector3.up);
            transform.position = hinge + q * (startPos - hinge);
            transform.rotation = q * startRot;
            yield return null;
        }

        doorCollider.enabled = true;
    }
}
