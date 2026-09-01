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

            Vector3 dodgeDirection = Vector3.zero;

            if (Input.GetKey(KeyCode.D))
            {
                dodgeDirection = transform.right;
            }
            else if (Input.GetKey(KeyCode.A))
            {
                dodgeDirection = -transform.right;
            }
            else
            {
                return;
            }

            isDodging = true;

            playerAnim.SetTrigger("Dodge");

            ThirdPersonController movement =
                GetComponent<ThirdPersonController>();

            if (movement != null)
            {
                movement.PerformDodge(
                    dodgeDirection,
                    2.5f,
                    0.35f
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