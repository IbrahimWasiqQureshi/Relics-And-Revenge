using UnityEngine;

public class PlayerHeadLookAt : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private LockOnManager lockOnManager;
    [SerializeField] private Animator animator;

    [Header("Look Weight")]
    [Tooltip("How quickly the head-look effect fades in/out when locking on/off.")]
    [SerializeField] private float weightSpeed = 6f;

    [Range(0f, 1f)]
    [SerializeField] private float headWeight = 0.6f;

    [Range(0f, 1f)]
    [SerializeField] private float bodyWeight = 0.15f;

    [Range(0f, 1f)]
    [SerializeField] private float eyesWeight = 0.4f;

    [Range(0f, 1f)]
    [SerializeField] private float clampWeight = 0.5f;

    private float currentWeight = 0f;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        if (lockOnManager == null)
            lockOnManager = FindObjectOfType<LockOnManager>();
    }

    private void OnAnimatorIK(int layerIndex)
    {
        if (animator == null)
            return;

        if (lockOnManager == null)
            lockOnManager = FindObjectOfType<LockOnManager>();

        bool locked =
            lockOnManager != null &&
            lockOnManager.IsLockedOn &&
            lockOnManager.currentTarget != null;

        float targetWeight = locked ? 1f : 0f;

        currentWeight = Mathf.Lerp(
            currentWeight,
            targetWeight,
            Time.deltaTime * weightSpeed
        );

        animator.SetLookAtWeight(
            currentWeight,
            bodyWeight,
            headWeight,
            eyesWeight,
            clampWeight
        );

        if (currentWeight > 0.01f && locked)
        {
            Transform targetPoint =
                lockOnManager.currentTarget.targetPoint != null
                    ? lockOnManager.currentTarget.targetPoint
                    : lockOnManager.currentTarget.transform;

            animator.SetLookAtPosition(targetPoint.position);
        }
    }
}
