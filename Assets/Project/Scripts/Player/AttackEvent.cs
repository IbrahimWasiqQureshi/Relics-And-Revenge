using UnityEngine;

public class AttackEvent : MonoBehaviour
{
    public Collider hitbox;
    public Collider kick;

    void Start()
    {
        hitbox.enabled = false;
        kick.enabled = false;
    }

    public void EnableHitbox()
    {
        hitbox.enabled = true;
    }

    public void DisableHitbox()
    {
        hitbox.enabled = false;
        kick.enabled=false;
    }

    public void EnableKick()
    {
        kick.enabled = true;
    }

    public void DisableKick()
    {
        kick.enabled = false;
    }
}
