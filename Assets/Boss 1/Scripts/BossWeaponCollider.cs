using UnityEngine;

/// <summary>
/// Sword hitbox for the boss. Same pattern as the project's existing
/// WeaponCollider, but targets IDamageable on the Player instead of
/// NPCController, and uses the EnableWeapon/DisableWeapon naming the boss
/// spec calls for. Toggled exclusively via Animation Events placed on attack
/// clips - never enabled by AI/code logic directly.
/// </summary>
[RequireComponent(typeof(Collider))]
public class BossWeaponCollider : MonoBehaviour
{
    [SerializeField] private int damage = 25;
    [SerializeField] private string targetTag = "Player";

    private Collider weaponCollider;
    private bool hasHitThisSwing;

    private void Awake()
    {
        weaponCollider = GetComponent<Collider>();
        weaponCollider.isTrigger = true;
        weaponCollider.enabled = false;
    }

    /// Animation Event: call at the start of the swing's active/damage frames.
    public void EnableWeapon()
    {
        hasHitThisSwing = false;
        weaponCollider.enabled = true;
    }

    /// Animation Event, or forced by BossController on hit/dodge/death/phase
    /// change: call to immediately stop the weapon from dealing damage.
    public void DisableWeapon()
    {
        weaponCollider.enabled = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasHitThisSwing) return;
        if (!other.CompareTag(targetTag)) return;

        IDamageable damageable = other.GetComponentInParent<IDamageable>();
        if (damageable == null) return;

        damageable.TakeDamage(damage);
        hasHitThisSwing = true;
    }
}
