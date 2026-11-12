using UnityEngine;

public class ShieldBlock : MonoBehaviour
{
    public static bool IsBlocking = false;
    public static ShieldBlock Instance { get; private set; }

    [Header("Sparks")]
    [Tooltip("Particle system that gets cloned where the enemy weapon hits the shield.")]
    [SerializeField] private ParticleSystem sparks;
    [Tooltip("Size of the shield sparks. 1 = normal, 2 = twice as big, 0.5 = half the size.")]
    [SerializeField, Min(0.05f)] private float sparkSize = 1f;
    [Tooltip("Extra offset applied to the spark position (world space).")]
    [SerializeField] private Vector3 sparkOffset = Vector3.zero;
    [Tooltip("Fallback cleanup time for the spawned sparks.")]
    [SerializeField, Min(0.1f)] private float cleanupDelay = 3f;

    [Header("Targeting")]
    [SerializeField] private string enemyWeaponTag = "EnemyWeapon";

    private Collider shieldCollider;
    private Vector3 lastContactPoint;
    private bool hasContact;

    private PlayerController playerController;
    private PlayerHealth playerHealth;

    private void Awake()
    {
        Instance = this;
        shieldCollider = GetComponent<Collider>();
        playerController = GetComponentInParent<PlayerController>();
        playerHealth = GetComponentInParent<PlayerHealth>();
    }

    private void OnEnable()
    {
        Instance = this;
    }

    // The shield collider is only on while the player holds block
    private void Update()
    {
        bool blocking = playerController != null && playerController.isBlocking;

        if (shieldCollider != null && shieldCollider.enabled != blocking)
        {
            shieldCollider.enabled = blocking;

            if (!blocking)
                IsBlocking = false;
        }
    }

    private void OnDisable()
    {
        if (Instance == this) Instance = null;
        IsBlocking = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(enemyWeaponTag)) return;

        IsBlocking = true;
        UpdateContact(other);

        Debug.Log("BLOCKED");

        // Recoil animation, sparks and invincible time
        if (playerController != null && playerController.isBlocking && playerHealth != null)
            playerHealth.OnBlockedHit();

        // Sword blowback on the enemy
        if (playerController != null && playerController.isBlocking)
            EnemyBlockReaction.Play(other);
    }

    private void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag(enemyWeaponTag)) return;

        UpdateContact(other);
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(enemyWeaponTag))
        {
            IsBlocking = false;
        }
    }

    // Called by PlayerHealth when an attack was actually blocked
    public void PlayBlockSparks()
    {
        if (sparks == null) return;

        Vector3 point = (hasContact ? lastContactPoint : transform.position) + sparkOffset;

        ParticleSystem fx = Instantiate(sparks, point, Quaternion.identity);
        fx.gameObject.SetActive(true);

        ApplySparkSize(fx);

        ParticleSystem.MainModule main = fx.main;
        main.stopAction = ParticleSystemStopAction.Destroy;

        fx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        fx.Play(true);

        Destroy(fx.gameObject, cleanupDelay);
    }

    // Makes the spawned sparks bigger or smaller, whatever scaling mode their particle systems use
    private void ApplySparkSize(ParticleSystem fx)
    {
        if (Mathf.Approximately(sparkSize, 1f))
            return;

        fx.transform.localScale *= sparkSize;

        foreach (ParticleSystem ps in fx.GetComponentsInChildren<ParticleSystem>(true))
        {
            ParticleSystem.MainModule main = ps.main;

            if (main.scalingMode == ParticleSystemScalingMode.Shape)
            {
                main.startSizeMultiplier *= sparkSize;
            }
            else if (main.scalingMode == ParticleSystemScalingMode.Local && ps.transform != fx.transform)
            {
                ps.transform.localScale *= sparkSize;
            }
        }
    }

    private void UpdateContact(Collider other)
    {
        if (shieldCollider == null) return;

        Vector3 onShield = Closest(shieldCollider, other.bounds.center);
        Vector3 onWeapon = Closest(other, onShield);
        lastContactPoint = (onShield + onWeapon) * 0.5f;
        hasContact = true;
    }

    private static Vector3 Closest(Collider c, Vector3 point)
    {
        // Collider.ClosestPoint is not supported on CharacterController / non-convex meshes
        MeshCollider mesh = c as MeshCollider;
        bool unsupported = c is CharacterController || (mesh != null && !mesh.convex);
        return unsupported ? c.bounds.ClosestPoint(point) : c.ClosestPoint(point);
    }
}
