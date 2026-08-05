using UnityEngine;

public class WeaponCollider : MonoBehaviour
{
    private Collider weaponCollider;

    private void Awake()
    {
        weaponCollider = GetComponent<Collider>();

        weaponCollider.enabled = false;
    }

    public void EnableWeapon()
    {
        weaponCollider.enabled = true;
    }

    public void DisableWeapon()
    {
        weaponCollider.enabled = false;
    }
}