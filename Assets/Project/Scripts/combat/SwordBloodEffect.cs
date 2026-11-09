using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Put this on the player's sword hitbox (same object as WeaponDamage).
/// Spawns the blood effect (splash + smoke + splatter) once per hit, at the point where
/// the sword touches the enemy.
/// </summary>
[RequireComponent(typeof(Collider))]
public class SwordBloodEffect : MonoBehaviour
{
    [Header("Effect")]
    [Tooltip("Prefab containing the blood splash, smoke and splatter particle systems.")]
    [SerializeField] private GameObject bloodPrefab;
    [Tooltip("Spawned effect is destroyed after this many seconds. Keep it longer than the ground stains last.")]
    [SerializeField, Min(0.5f)] private float cleanupDelay = 8f;
    [Tooltip("0 = sprays straight away from the player, 1 = strongly upward.")]
    [SerializeField, Range(0f, 1f)] private float upwardBias = 0.35f;
    [Tooltip("Extra offset applied to the spawn position (world space).")]
    [SerializeField] private Vector3 spawnOffset = Vector3.zero;

    [Header("One effect per hit")]
    [Tooltip("An enemy can have several colliders touched by one swing. This stops them spawning extra blood.")]
    [SerializeField, Min(0f)] private float perTargetCooldown = 0.35f;

    [Header("Targeting (should match WeaponDamage)")]
    [SerializeField] private string targetTag = "Enemy";

    private Collider hitbox;
    private Transform attacker;
    private readonly Dictionary<Transform, float> lastHitTime = new Dictionary<Transform, float>();

    private void Awake()
    {
        hitbox = GetComponent<Collider>();
        attacker = transform.root;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (bloodPrefab == null) return;
        if (!other.CompareTag(targetTag)) return;

        // Only count real hits (same rule WeaponDamage uses to deal damage)
        IDamageable damageable = other.GetComponentInParent<IDamageable>();
        if (damageable == null) return;

        MonoBehaviour mb = damageable as MonoBehaviour;
        Transform target = mb != null ? mb.transform : other.transform.root;

        if (!CanSpawnFor(target)) return;

        Vector3 point = GetContactPoint(other) + spawnOffset;

        // Spray away from the attacker, with a slight upward bias
        Vector3 dir = point - attacker.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) dir = attacker.forward;
        dir = (dir.normalized + Vector3.up * upwardBias).normalized;

        GameObject fx = Instantiate(bloodPrefab, point, Quaternion.LookRotation(dir));
        Destroy(fx, cleanupDelay);
    }

    private bool CanSpawnFor(Transform target)
    {
        float now = Time.unscaledTime;

        float last;
        if (lastHitTime.TryGetValue(target, out last) && now - last < perTargetCooldown)
            return false;

        lastHitTime[target] = now;

        // Drop entries for enemies that no longer exist
        if (lastHitTime.Count > 16)
        {
            List<Transform> dead = new List<Transform>();
            foreach (KeyValuePair<Transform, float> kv in lastHitTime)
                if (kv.Key == null) dead.Add(kv.Key);
            for (int i = 0; i < dead.Count; i++)
                lastHitTime.Remove(dead[i]);
        }

        return true;
    }

    // Point where the sword and the enemy touch
    private Vector3 GetContactPoint(Collider other)
    {
        Vector3 onSword = Closest(hitbox, other.bounds.center);
        Vector3 onEnemy = Closest(other, onSword);
        return (onSword + onEnemy) * 0.5f;
    }

    private static Vector3 Closest(Collider c, Vector3 point)
    {
        // Collider.ClosestPoint is not supported on CharacterController / non-convex meshes
        MeshCollider mesh = c as MeshCollider;
        bool unsupported = c is CharacterController || (mesh != null && !mesh.convex);
        return unsupported ? c.bounds.ClosestPoint(point) : c.ClosestPoint(point);
    }
}
