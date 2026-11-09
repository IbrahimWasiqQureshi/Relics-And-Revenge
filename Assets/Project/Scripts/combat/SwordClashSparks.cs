using UnityEngine;

/// <summary>
/// Put this on the player's sword hitbox (same object as WeaponDamage).
/// When the player's swinging sword meets an enemy's swinging sword, a spark burst
/// is spawned at the point where the two blades touch - the same effect used for shield blocks.
/// </summary>
[RequireComponent(typeof(Collider))]
public class SwordClashSparks : MonoBehaviour
{
    [Header("Sparks")]
    [Tooltip("Particle system that gets cloned at the clash point. By default this is the same one the shield uses.")]
    [SerializeField] private ParticleSystem sparks;
    [Tooltip("Extra offset applied to the spark position (world space).")]
    [SerializeField] private Vector3 sparkOffset = Vector3.zero;
    [Tooltip("Minimum time between sparks, so one clash only sparks once.")]
    [SerializeField, Min(0f)] private float cooldown = 0.2f;
    [Tooltip("Fallback cleanup time for the spawned sparks.")]
    [SerializeField, Min(0.1f)] private float cleanupDelay = 3f;

    [Header("Targeting")]
    [SerializeField] private string enemyWeaponTag = "EnemyWeapon";

    private Collider hitbox;
    private float lastSparkTime = -10f;

    private void Awake()
    {
        hitbox = GetComponent<Collider>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (sparks == null) return;
        if (!other.CompareTag(enemyWeaponTag)) return;

        // Several colliders can enter at once; only spark once per clash
        if (Time.unscaledTime - lastSparkTime < cooldown) return;
        lastSparkTime = Time.unscaledTime;

        Vector3 point = GetClashPoint(other) + sparkOffset;

        ParticleSystem fx = Instantiate(sparks, point, Quaternion.identity);
        fx.gameObject.SetActive(true);

        ParticleSystem.MainModule main = fx.main;
        main.stopAction = ParticleSystemStopAction.Destroy;

        fx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        fx.Play(true);

        Destroy(fx.gameObject, cleanupDelay);
    }

    // Midpoint between the two blades at the spot where they touch
    private Vector3 GetClashPoint(Collider other)
    {
        Vector3 onMine = Closest(hitbox, other.bounds.center);
        Vector3 onTheirs = Closest(other, onMine);
        return (onMine + onTheirs) * 0.5f;
    }

    private static Vector3 Closest(Collider c, Vector3 point)
    {
        // Collider.ClosestPoint is not supported on CharacterController / non-convex meshes
        MeshCollider mesh = c as MeshCollider;
        bool unsupported = c is CharacterController || (mesh != null && !mesh.convex);
        return unsupported ? c.bounds.ClosestPoint(point) : c.ClosestPoint(point);
    }
}
