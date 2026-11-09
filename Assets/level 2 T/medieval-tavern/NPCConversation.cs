using UnityEngine;

public class NPCConversation : MonoBehaviour
{
    public Animator npc1Animator;
    public Animator npc2Animator;

    public float npc1TalkTime = 4f;
    public float npc2TalkTime = 4f;

    private float timer;
    private bool npc1Turn = true;

    void Start()
    {
        StartNPC1Turn();
    }

    void Update()
    {
        timer -= Time.deltaTime;

        if (timer <= 0f)
        {
            if (npc1Turn)
            {
                StartNPC2Turn();
            }
            else
            {
                StartNPC1Turn();
            }
        }
    }

    void StartNPC1Turn()
    {
        npc1Turn = true;
        timer = npc1TalkTime;

        npc1Animator.SetBool("IsTalking", true);
        npc2Animator.SetBool("IsTalking", false);
    }

    void StartNPC2Turn()
    {
        npc1Turn = false;
        timer = npc2TalkTime;

        npc1Animator.SetBool("IsTalking", false);
        npc2Animator.SetBool("IsTalking", true);
    }
}