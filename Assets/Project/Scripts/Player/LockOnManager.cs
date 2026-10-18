using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LockOnManager : MonoBehaviour
{
    [Header("Player")]
    public Transform player;
    public Animator playerAnimator;
    public Camera playerCamera;

    [Header("Lock-On Settings")]
    public float detectionRange = 15f;
    public float maxLockDistance = 20f;

    [Header("Target Selection")]
    [Range(1f, 30f)]
    public float aimAngle = 10f;
    public LayerMask aimLayers = ~0;
    public KeyCode lockOnKey = KeyCode.Tab;

    [Header("Target Dot")]
    public GameObject targetDotPrefab;

    [Header("Target Outline")]
    [Tooltip("White outline on the selected enemy, red once locked on.")]
    public bool useOutline = true;

    private Dictionary<LockOnTarget, LockOnOutline> outlines =
        new Dictionary<LockOnTarget, LockOnOutline>();

    private List<LockOnTarget> outlineScratch =
        new List<LockOnTarget>();

    [Range(0f, 0.5f)]
    public float screenEdgePadding = 0.05f;

    [Header("Current Target")]
    public LockOnTarget currentTarget;

    [Header("Debug")]
    public List<LockOnTarget> availableTargets =
        new List<LockOnTarget>();

    private List<LockOnTarget> visibleTargets =
        new List<LockOnTarget>();

    private Dictionary<LockOnTarget, GameObject> targetDots =
        new Dictionary<LockOnTarget, GameObject>();

    public bool IsLockedOn { get; private set; }

    private int selectedTargetIndex = -1;


    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        if (player == null || playerCamera == null)
            return;

        // FIRST:
        // Clean up dots belonging to destroyed enemies.
        CleanupDestroyedTargets();

        FindVisibleTargets();

        UpdateTargetDots();

        HandleTargetSelection();

        HandleLockOn();

        CheckCurrentTarget();

        UpdateOutlines();
    }


    // =========================================================
    // FIND VISIBLE TARGETS
    // =========================================================

    private void FindVisibleTargets()
    {
        visibleTargets.Clear();

        LockOnTarget[] allTargets =
            FindObjectsOfType<LockOnTarget>();

        foreach (LockOnTarget target in allTargets)
        {
            if (target == null)
                continue;

            if (!target.gameObject.activeInHierarchy)
                continue;

            if (target.transform == player)
                continue;

            float distance =
                Vector3.Distance(
                    player.position,
                    target.transform.position
                );

            if (distance > detectionRange)
                continue;

            if (!IsTargetOnScreen(target))
                continue;

            visibleTargets.Add(target);
        }

        availableTargets.Clear();

        foreach (LockOnTarget target in visibleTargets)
        {
            if (target != null)
            {
                availableTargets.Add(target);
            }
        }

        if (visibleTargets.Count == 0)
        {
            selectedTargetIndex = -1;

            if (!IsLockedOn)
            {
                currentTarget = null;
            }

            return;
        }

        if (selectedTargetIndex >= visibleTargets.Count)
        {
            selectedTargetIndex = 0;
        }
    }


    // =========================================================
    // CAMERA CHECK
    // =========================================================

    private bool IsTargetOnScreen(
        LockOnTarget target)
    {
        Transform targetPoint =
            target.targetPoint;

        if (targetPoint == null)
            targetPoint = target.transform;

        Vector3 screenPosition =
            playerCamera.WorldToViewportPoint(
                targetPoint.position
            );

        if (screenPosition.z <= 0f)
            return false;

        if (screenPosition.x < screenEdgePadding ||
            screenPosition.x > 1f - screenEdgePadding)
        {
            return false;
        }

        if (screenPosition.y < screenEdgePadding ||
            screenPosition.y > 1f - screenEdgePadding)
        {
            return false;
        }

        return true;
    }


    // =========================================================
    // TARGET DOTS
    // =========================================================

    private void UpdateTargetDots()
    {
        if (targetDotPrefab == null)
            return;


        foreach (LockOnTarget target in visibleTargets)
        {
            if (target == null)
                continue;


            if (!targetDots.ContainsKey(target))
            {
                CreateTargetDot(target);
            }


            if (targetDots.ContainsKey(target))
            {
                GameObject dot =
                    targetDots[target];


                if (dot != null)
                {
                    dot.SetActive(true);

                    UpdateDotPosition(
                        target,
                        dot
                    );

                    UpdateDotAppearance(
                        target,
                        dot
                    );
                }
            }
        }


        List<LockOnTarget> allDotTargets =
            new List<LockOnTarget>(
                targetDots.Keys
            );


        foreach (LockOnTarget target in allDotTargets)
        {
            // Target was destroyed.
            if (target == null)
                continue;


            if (!visibleTargets.Contains(target))
            {
                if (targetDots[target] != null)
                {
                    targetDots[target].SetActive(false);
                }
            }
        }
    }


    // =========================================================
    // CREATE TARGET DOT
    // =========================================================

    private void CreateTargetDot(
        LockOnTarget target)
    {
        Transform targetPoint =
            target.targetPoint;

        if (targetPoint == null)
            targetPoint = target.transform;


        GameObject dot =
            Instantiate(
                targetDotPrefab,
                targetPoint.position,
                Quaternion.identity
            );


        dot.name =
            "TargetDot_" +
            target.name;


        targetDots.Add(
            target,
            dot
        );
    }


    // =========================================================
    // UPDATE DOT POSITION
    // =========================================================

    private void UpdateDotPosition(
        LockOnTarget target,
        GameObject dot)
    {
        if (target == null ||
            dot == null)
            return;


        Transform targetPoint =
            target.targetPoint;

        if (targetPoint == null)
            targetPoint = target.transform;


        dot.transform.position =
            targetPoint.position;


        Vector3 direction =
            dot.transform.position -
            playerCamera.transform.position;


        if (direction != Vector3.zero)
        {
            dot.transform.rotation =
                Quaternion.LookRotation(
                    direction
                );
        }
    }


    // =========================================================
    // UPDATE DOT APPEARANCE
    // =========================================================

    private void UpdateDotAppearance(
        LockOnTarget target,
        GameObject dot)
    {
        Image image =
            dot.GetComponentInChildren<Image>();

        if (image == null)
            return;


        if (target == currentTarget)
        {
            image.transform.localScale =
                Vector3.one * 1.4f;
        }
        else
        {
            image.transform.localScale =
                Vector3.one;
        }
    }


    // =========================================================
    // KEYBOARD TARGET SELECTION
    // =========================================================

    private void HandleTargetSelection()
    {
        if (IsLockedOn)
            return;

        currentTarget = GetAimedTarget();
    }

    private LockOnTarget GetAimedTarget()
    {
        Transform cam = playerCamera.transform;

        LockOnTarget best = null;
        float bestAngle = aimAngle;

        foreach (LockOnTarget target in visibleTargets)
        {
            if (target == null)
                continue;

            Transform point = target.targetPoint != null
                ? target.targetPoint
                : target.transform;

            float angle = Vector3.Angle(
                cam.forward,
                point.position - cam.position
            );

            if (angle > bestAngle)
                continue;

            if (!HasClearView(target, point.position))
                continue;

            best = target;
            bestAngle = angle;
        }

        return best;
    }

    private bool HasClearView(LockOnTarget target, Vector3 targetPosition)
    {
        Vector3 origin = playerCamera.transform.position;
        Vector3 toTarget = targetPosition - origin;

        float ignoreDistance =
            Vector3.Distance(origin, player.position) + 1f;

        RaycastHit[] hits = Physics.RaycastAll(
            origin,
            toTarget.normalized,
            toTarget.magnitude,
            aimLayers,
            QueryTriggerInteraction.Ignore
        );

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            if (hit.distance < ignoreDistance)
                continue;

            if (hit.transform.IsChildOf(player.root))
                continue;

            LockOnTarget hitTarget = hit.collider.GetComponentInParent<LockOnTarget>();

            return hitTarget == target;
        }

        return true;
    }


    // =========================================================
    // LOCK / UNLOCK
    // =========================================================

    private void HandleLockOn()
    {
        if (!Input.GetKeyDown(lockOnKey))
            return;


        if (IsLockedOn)
        {
            Unlock();

            return;
        }


        if (currentTarget != null)
        {
            LockOn(currentTarget);
        }
        
    }


    // =========================================================
    // LOCK ON
    // =========================================================

    public void LockOn(
        LockOnTarget target)
    {
        if (target == null)
            return;


        currentTarget = target;

        IsLockedOn = true;


        if (playerAnimator != null)
        {
            playerAnimator.SetBool(
                "IsLockedOn",
                true
            );
        }
    }


    // =========================================================
    // UNLOCK
    // =========================================================

    public void Unlock()
    {
        IsLockedOn = false;

        currentTarget = null;

        selectedTargetIndex = -1;


        if (playerAnimator != null)
        {
            playerAnimator.SetBool(
                "IsLockedOn",
                false
            );

            playerAnimator.SetFloat(
                "MoveX",
                0f
            );

            playerAnimator.SetFloat(
                "MoveY",
                0f
            );
        }
    }


    // =========================================================
    // LOCK-ON MOVEMENT
    // =========================================================




    // =========================================================
    // CHECK CURRENT TARGET
    // =========================================================

    private void CheckCurrentTarget()
    {
        if (!IsLockedOn)
            return;


        // =====================================================
        // CURRENT TARGET WAS DESTROYED
        // =====================================================

        if (currentTarget == null)
        {
            RemoveDeadTarget();

            return;
        }


        // =====================================================
        // TARGET DISABLED
        // =====================================================

        if (!currentTarget.gameObject.activeInHierarchy)
        {
            RemoveDeadTarget();

            return;
        }


        // =====================================================
        // DISTANCE
        // =====================================================

        float distance =
            Vector3.Distance(
                player.position,
                currentTarget.transform.position
            );


        if (distance > maxLockDistance)
        {
            Unlock();

            return;
        }
    }

private void UpdateOutlines()
    {
        if (!useOutline)
            return;

        // Which enemy gets the outline?
        // - The enemy you selected with Q/E or locked onto.
        // - If nothing is selected yet, the enemy that Tab would lock onto.
        LockOnTarget highlighted = currentTarget;

        

        // Make sure every visible enemy has an outline component.
        foreach (LockOnTarget target in visibleTargets)
        {
            if (target == null || outlines.ContainsKey(target))
                continue;

            LockOnOutline outline = target.GetComponent<LockOnOutline>();

            if (outline == null)
                outline = target.gameObject.AddComponent<LockOnOutline>();

            outlines.Add(target, outline);
        }

        // Update every enemy we know about (this also clears the outline
        // of enemies that are no longer selected or no longer visible).
        outlineScratch.Clear();
        outlineScratch.AddRange(outlines.Keys);

        foreach (LockOnTarget target in outlineScratch)
        {
            LockOnOutline outline = outlines[target];

            if (target == null || outline == null)
            {
                outlines.Remove(target);
                continue;
            }

            if (target == highlighted)
            {
                outline.SetState(
                    IsLockedOn
                        ? LockOnOutline.State.Locked
                        : LockOnOutline.State.Selected
                );
            }
            else
            {
                outline.SetState(LockOnOutline.State.None);
            }
        }
    }



    // =========================================================
    // CLEANUP DESTROYED TARGETS
    // =========================================================
    //
    // THIS IS THE IMPORTANT FIX.
    //
    // When Unity destroys a Knight, the LockOnTarget reference
    // becomes null. Therefore we cannot do:
    //
    // targetDots[currentTarget]
    //
    // because currentTarget is already null.
    //
    // Instead we search the entire dictionary.
    // =========================================================

    private void CleanupDestroyedTargets()
    {
        List<LockOnTarget> deadTargets =
            new List<LockOnTarget>();


        foreach (KeyValuePair<
            LockOnTarget,
            GameObject> pair
            in targetDots)
        {
            LockOnTarget target =
                pair.Key;

            GameObject dot =
                pair.Value;


            // =================================================
            // TARGET WAS DESTROYED
            // =================================================

            if (target == null)
            {
                if (dot != null)
                {
                    Destroy(dot);
                }

                deadTargets.Add(target);

                continue;
            }


            // =================================================
            // TARGET GAMEOBJECT WAS DISABLED
            // =================================================

            if (!target.gameObject.activeInHierarchy)
            {
                if (dot != null)
                {
                    Destroy(dot);
                }

                deadTargets.Add(target);
            }
        }


        // =====================================================
        // REMOVE DEAD DICTIONARY ENTRIES
        // =====================================================

        foreach (LockOnTarget deadTarget in deadTargets)
        {
            targetDots.Remove(deadTarget);
        }


        // =====================================================
        // IF CURRENT TARGET DIED
        // =====================================================

        if (currentTarget == null)
        {
            if (IsLockedOn)
            {
                IsLockedOn = false;

                selectedTargetIndex = -1;


                if (playerAnimator != null)
                {
                    playerAnimator.SetBool(
                        "IsLockedOn",
                        false
                    );

                    playerAnimator.SetFloat(
                        "MoveX",
                        0f
                    );

                    playerAnimator.SetFloat(
                        "MoveY",
                        0f
                    );
                }
            }

            currentTarget = null;
        }
    }


    // =========================================================
    // REMOVE DEAD TARGET
    // =========================================================

    private void RemoveDeadTarget()
    {
        // =====================================================
        // DESTROY ALL INVALID DOTS
        // =====================================================

        CleanupDestroyedTargets();


        // =====================================================
        // CLEAR LOCK
        // =====================================================

        IsLockedOn = false;

        currentTarget = null;

        selectedTargetIndex = -1;


        // =====================================================
        // RESET ANIMATOR
        // =====================================================

        if (playerAnimator != null)
        {
            playerAnimator.SetBool(
                "IsLockedOn",
                false
            );

            playerAnimator.SetFloat(
                "MoveX",
                0f
            );

            playerAnimator.SetFloat(
                "MoveY",
                0f
            );
        }
    }


    // =========================================================
    // DEBUG
    // =========================================================

    private void OnDrawGizmosSelected()
    {
        if (player == null)
            return;


        Gizmos.DrawWireSphere(
            player.position,
            detectionRange
        );


        Gizmos.DrawWireSphere(
            player.position,
            maxLockDistance
        );
    }
}