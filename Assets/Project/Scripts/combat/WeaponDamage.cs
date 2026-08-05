using UnityEngine;

public class WeaponDamage : MonoBehaviour
{
    [SerializeField] private int damage = 20;

    [SerializeField] private string targetTag = "Enemy";

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(targetTag))
            return;

        IDamageable damageable = other.GetComponent<IDamageable>();

        if (damageable != null)
        {
            damageable.TakeDamage(damage);
        }
    }
}