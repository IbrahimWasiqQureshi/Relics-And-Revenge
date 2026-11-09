using UnityEngine;

public class CowEating : MonoBehaviour
{
    public Animator animator;

    [Header("Timing")]
    public float idleTime = 3f;
    public float eatingTime = 5f;
    public float startDelay = 0f;

    private float timer;
    private bool eating;

    void Start()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        animator.SetBool("Eating", false);

        // Different starting time for each cow
        timer = startDelay + idleTime;
    }

    void Update()
    {
        timer -= Time.deltaTime;

        if (timer <= 0f)
        {
            if (eating)
            {
                eating = false;
                animator.SetBool("Eating", false);
                timer = idleTime;
            }
            else
            {
                eating = true;
                animator.SetBool("Eating", true);
                timer = eatingTime;
            }
        }
    }
}