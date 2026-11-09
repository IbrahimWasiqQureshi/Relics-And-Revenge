using UnityEngine;

/// <summary>
/// Put this on the player's sword hitbox (same object as WeaponDamage / SwordBloodEffect).
/// On hitting an enemy it flashes the enemy and briefly freezes the game (hit stop).
/// </summary>
[RequireComponent(typeof(Collider))]
public class SwordHitFeedback : MonoBehaviour
{
    [Header("Hit Flash")]
    [SerializeField] private bool enableHitFlash = true;
    [ColorUsage(false)]
    [SerializeField] private Color flashColor = Color.white;
    [Tooltip("How long the enemy stays flashed, in real seconds (not affected by hit stop).")]
    [SerializeField, Min(0f)] private float flashDuration = 0.1f;
    [Tooltip("Solid unlit material used for the flash. Leave the assigned one as is.")]
    [SerializeField] private Material flashMaterial;

    [Header("Hit Stop")]
    [SerializeField] private bool enableHitStop = true;
    [Tooltip("How long the game freezes, in real seconds.")]
    [SerializeField, Min(0f)] private float hitStopDuration = 0.07f;
    [Tooltip("Game speed during the freeze. 0 = full freeze, 0.05 = near-freeze with a tiny bit of motion.")]
    [SerializeField, Range(0f, 1f)] private float hitStopTimeScale = 0.05f;

    [Header("Targeting (should match WeaponDamage)")]
    [SerializeField] private string targetTag = "Enemy";

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(targetTag)) return;

        IDamageable damageable = other.GetComponentInParent<IDamageable>();
        if (damageable == null) return;

        if (enableHitFlash)
        {
            MonoBehaviour mb = damageable as MonoBehaviour;
            GameObject target = mb != null ? mb.gameObject : other.gameObject;
            HitFlashReceiver.Flash(target, flashMaterial, flashColor, flashDuration);
        }

        if (enableHitStop)
            HitStopManager.Trigger(hitStopDuration, hitStopTimeScale);
    }
}
