using UnityEngine;

public class WeaponCollider : MonoBehaviour
{
    private Collider weaponCollider;

    private void Awake()
    {
        weaponCollider = GetComponent<Collider>();

        if (weaponCollider != null)
            weaponCollider.enabled = false;
    }

    public void EnableWeapon()
    {
        if (weaponCollider != null)
            weaponCollider.enabled = true;
    }

    public void DisableWeapon()
    {
        if (weaponCollider != null)
            weaponCollider.enabled = false;
    }

    public void ForceDisable()
    {
        if (weaponCollider != null)
            weaponCollider.enabled = false;
    }


    //=================================================
    // WEAPON HIT
    //=================================================

    private void OnTriggerEnter(Collider other)
    {
        // Check for NPC on the object we hit
        NPCController npc = other.GetComponent<NPCController>();

        // If NPC is on a parent object
        if (npc == null)
        {
            npc = other.GetComponentInParent<NPCController>();
        }

        if (npc != null)
        {
            Debug.Log(
                "WEAPON HIT NPC: " +
                npc.gameObject.name
            );

            npc.TakeHit(this);
        }
    }
}