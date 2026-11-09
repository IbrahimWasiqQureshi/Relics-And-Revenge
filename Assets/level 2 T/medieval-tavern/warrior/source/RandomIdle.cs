using UnityEngine;

public class RandomIdle : MonoBehaviour
{
    public Animator animator;

    [Header("Start Delay")]
    public float minStartDelay = 0f;
    public float maxStartDelay = 10f;

    void Start()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        float delay = Random.Range(minStartDelay, maxStartDelay);
        Invoke(nameof(StartAnimation), delay);
    }

    void StartAnimation()
    {
        animator.Play("Scene", 0, 0f);
    }
}