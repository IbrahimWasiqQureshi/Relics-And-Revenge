using UnityEngine;

public class LockOnTarget : MonoBehaviour
{
    public Transform targetPoint;

    // True once the enemy has died. Dead enemies can no longer be selected or locked onto.
    public bool IsDead { get; private set; }

    // Call this the moment the enemy dies: it releases the lock-on right away.
    public void OnTargetDied()
    {
        IsDead = true;

        LockOnManager manager = FindFirstObjectByType<LockOnManager>();

        if (manager != null)
            manager.ReleaseIfTarget(this);
    }
}