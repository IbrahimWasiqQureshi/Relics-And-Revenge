using UnityEngine;

// Put this on your real Player.
// It lets the AI read what the player is doing, WITHOUT changing PlayerController.
[RequireComponent(typeof(PlayerController))]
public class PlayerCombatantAdapter : MonoBehaviour, ICombatant
{
    private PlayerController cachedController;

    // Looks up PlayerController the first time anyone asks for it.
    // This avoids a bug where the analyzer asks before this script's Awake has run.
    private PlayerController controller
    {
        get
        {
            if (cachedController == null)
                cachedController = GetComponent<PlayerController>();
            return cachedController;
        }
    }

private void Awake()
    {
        // Nothing needed here. 'controller' finds itself the first time it is used.
    }

    public Transform CombatTransform
    {
        get { return transform; }
    }

    public int AttackType
    {
        get
        {
            if (controller.isKicking)
                return 4;

            if (controller.isAttacking)
                return Mathf.Clamp(controller.currentAttack, 1, 3);

            return 0;
        }
    }

    public bool IsBlocking
    {
        get { return controller.isBlocking; }
    }

    public bool IsDodging
    {
        get { return controller.isDodging; }
    }
}
