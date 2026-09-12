using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using StarterAssets;

public class PlayerController : MonoBehaviour
{
    // =========================================================
    // REFERENCES
    // =========================================================
    [SerializeField]
    private GameObject trail;



    [SerializeField]
    private Animator playerAnim;


    // =========================================================
    // EQUIP / UNEQUIP
    // =========================================================

    [SerializeField]
    private GameObject sword;

    [SerializeField]
    private GameObject swordOnShoulder;

    public bool isEquipping;
    public bool isEquipped;


    // =========================================================
    // BLOCK
    // =========================================================

    public bool isBlocking;


    // =========================================================
    // KICK
    // =========================================================

    public bool isKicking;


    // =========================================================
    // ATTACK
    // =========================================================

    public bool isAttacking;

    private float timeSinceAttack;

    public int currentAttack = 0;

    [Header("Combat Movement")]
    [SerializeField]
    private float attackMoveMultiplier = 0.5f;

    public float AttackMoveMultiplier
    {
        get { return attackMoveMultiplier; }
    }


    // =========================================================
    // DODGE
    // =========================================================

    

    private LockOnManager lockOnManager;
public bool isDodging;


    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        timeSinceAttack += Time.deltaTime;

        Attack();
        Equip();
        Block();
        Kick();
        Dodge();
    }


    // =========================================================
    // EQUIP
    // =========================================================

    private void Equip()
    {
        if (Input.GetKeyDown(KeyCode.R) &&
            playerAnim.GetBool("Grounded"))
        {
            if (isEquipping || isDodging)
                return;

            isEquipping = true;

            playerAnim.SetTrigger("Equip");
        }
    }


    // =========================================================
    // WEAPON
    // =========================================================

    public void ActiveWeapon()
    {
        if (!isEquipped)
        {
            sword.SetActive(true);
            swordOnShoulder.SetActive(false);

            isEquipped = true;
        }
        else
        {
            sword.SetActive(false);
            swordOnShoulder.SetActive(true);

            isEquipped = false;
        }
    }


    // =========================================================
    // EQUIP FINISHED
    // =========================================================

    public void Equipped()
    {
        isEquipping = false;
    }


    // =========================================================
    // BLOCK
    // =========================================================

    private void Block()
    {
        if (Input.GetKey(KeyCode.Mouse1) &&
            playerAnim.GetBool("Grounded"))
        {
            if (isDodging)
                return;

            playerAnim.SetBool("Block", true);
            isBlocking = true;
        }
        else
        {
            playerAnim.SetBool("Block", false);
            isBlocking = false;
        }
    }


    // =========================================================
    // KICK
    // =========================================================

    public void Kick()
    {
        if (Input.GetKey(KeyCode.LeftControl) &&
            playerAnim.GetBool("Grounded"))
        {
            if (isDodging)
                return;

            playerAnim.SetBool("Kick", true);
            isKicking = true;
        }
        else
        {
            playerAnim.SetBool("Kick", false);
            isKicking = false;
        }
    }


    // =========================================================
    // ATTACK
    // =========================================================

    private void Attack()
    {
        if (Input.GetMouseButtonDown(0) &&
            playerAnim.GetBool("Grounded") &&
            timeSinceAttack > 0.8f)
        {
            if (!isEquipped)
                return;

            if (isEquipping || isDodging)
                return;

            currentAttack++;

            isAttacking = true;

            if (currentAttack > 3)
                currentAttack = 1;

            if (timeSinceAttack > 1.0f)
                currentAttack = 1;

            playerAnim.SetTrigger(
                "Attack" + currentAttack
            );

            timeSinceAttack = 0;
        }
    }


    // =========================================================
    // ATTACK FINISHED
    // =========================================================

    public void ResetAttack()
    {
        isAttacking = false;
    }


    // =========================================================
    // DODGE
    // =========================================================

private void Dodge()
    {
        if (Input.GetKeyDown(KeyCode.LeftAlt) &&
            playerAnim.GetBool("Grounded"))
        {
            if (isEquipping ||
                isAttacking ||
                isBlocking ||
                isKicking ||
                isDodging)
            {
                return;
            }

            if (lockOnManager == null)
            {
                lockOnManager = FindObjectOfType<LockOnManager>();
            }

            // =================================================
            // LOCK-ON ONLY
            //
            // Dodge rolling is only available while locked onto
            // an enemy - no free-roam dodge.
            // =================================================

            if (lockOnManager == null ||
                !lockOnManager.IsLockedOn ||
                lockOnManager.currentTarget == null)
            {
                return;
            }

            if (!Input.GetKey(KeyCode.D) &&
                !Input.GetKey(KeyCode.A))
            {
                return;
            }

            // =================================================
            // TANGENT AROUND THE TARGET
            //
            // Instead of dodging relative to whichever way the
            // player happens to be facing (transform.right), the
            // direction is derived fresh from the vector to the
            // locked target. This guarantees "D" always dodges
            // to the same side around the enemy and "A" always
            // dodges to the other side, no matter the player's
            // current facing - and it keeps the roll moving along
            // the enemy's circle of rotation (a strafe around the
            // target) rather than in an arbitrary straight line.
            // =================================================

            Transform targetPoint =
                lockOnManager.currentTarget.targetPoint != null
                    ? lockOnManager.currentTarget.targetPoint
                    : lockOnManager.currentTarget.transform;

            Vector3 toTarget =
                targetPoint.position -
                transform.position;

            toTarget.y = 0f;

            if (toTarget.sqrMagnitude < 0.0001f)
                return;

            toTarget.Normalize();

            Vector3 tangentRight =
                Vector3.Cross(
                    Vector3.up,
                    toTarget
                ).normalized;

            Vector3 dodgeDirection =
                Input.GetKey(KeyCode.D)
                    ? tangentRight
                    : -tangentRight;

            // =================================================
            // FACE THE ROLL DIRECTION
            //
            // Souls-style: snap to face the direction we're about
            // to dive into. "Standing Dive Forward" is a forward
            // roll animation, so the character needs to be facing
            // dodgeDirection for the dive to read correctly.
            //
            // RotateTowardsLockOnTarget() takes over again as soon
            // as isDodging clears and smoothly turns the player
            // back to face the target - so this rotation is only
            // ever temporary.
            // =================================================

            transform.rotation =
                Quaternion.LookRotation(
                    dodgeDirection,
                    Vector3.up
                );

            isDodging = true;

            playerAnim.SetTrigger("Dodge");

            ThirdPersonController movement =
                GetComponent<ThirdPersonController>();

            if (movement != null)
            {
                movement.PerformDodge(
                    dodgeDirection,
                    3.0f,
                    0.7f
                );
            }
        }
    }


    // =========================================================
    // DODGE FINISHED
    // =========================================================

    public void ResetDodge()
    {
        isDodging = false;
    }


    // =========================================================
    // CANCEL ACTIONS WHEN HIT
    // =========================================================

public void CancelActionStates()
    {
        isEquipping = false;
        isAttacking = false;

        if (isDodging)
        {
            ThirdPersonController movement =
                GetComponent<ThirdPersonController>();

            if (movement != null)
            {
                movement.StopDodge();
            }
        }

        isDodging = false;
    }

    
    public void EnableTrail()
    {
        trail.gameObject.SetActive(true);
    }

    public void DisableTrail()
    {
        trail.gameObject.SetActive(false);
    }
}