using UnityEngine;

// Put this on the AI Enemy.
// It watches the player and turns what it sees into simple numbers (0 to 1).
// The AI brain will read these numbers later.
public class PlayerCombatAnalyzer : MonoBehaviour
{
    [Header("Who to watch")]
    [SerializeField] private GameObject player;
    [SerializeField] private Transform enemy;

    [Header("Time windows (seconds)")]
    [Tooltip("How far back we look for attack, dodge, block and movement habits.")]
    [SerializeField] private float rateWindow = 6f;
    [Tooltip("Short memory. Shows what the player does right now.")]
    [SerializeField] private float fastWindow = 3f;
    [Tooltip("Long memory. Used to notice that the player changed style.")]
    [SerializeField] private float slowWindow = 15f;

    [Header("Tuning")]
    [Tooltip("This many attacks per second counts as 'attacks all the time' (1.0).")]
    [SerializeField] private float maxAttackRate = 1f;
    [Tooltip("This many dodges per second counts as 'dodges all the time' (1.0).")]
    [SerializeField] private float maxDodgeRate = 0.5f;
    [Tooltip("Player must move toward or away from the enemy faster than this (m/s).")]
    [SerializeField] private float moveSpeedThreshold = 0.5f;
    [Tooltip("How many recent actions we remember.")]
    [SerializeField, Range(4, 20)] private int historyLength = 10;

    [Header("Debug")]
    [SerializeField] private bool showDebug = true;

    // ---------- Values the AI can read (all from 0 to 1) ----------
    public float AttackFrequency { get { return Mathf.Clamp01(attackRate / maxAttackRate); } }
    public float DodgeFrequency { get { return Mathf.Clamp01(dodgeRate / maxDodgeRate); } }
    public float BlockTendency { get { return blockAvg; } }
    public float ApproachTendency { get { return approachAvg; } }
    public float RetreatTendency { get { return retreatAvg; } }
    public float Aggression { get { return fastAggressive; } }
    public float Defensiveness { get { return fastDefensive; } }
    public float Repetition { get; private set; }

    public float BehaviorChange
    {
        get
        {
            float change = Mathf.Abs(fastAggressive - slowAggressive) +
                           Mathf.Abs(fastDefensive - slowDefensive);
            return Mathf.Clamp01(change);
        }
    }

    public int HistoryCount { get { return historyCount; } }

    // ---------- Private data ----------
    private ICombatant combatant;

    private CombatActionType[] history;
    private int historyHead;
    private int historyCount;
    private readonly int[] counts = new int[7];

    private float timeInFight;

    private float attackRate;
    private float dodgeRate;
    private float blockAvg;
    private float approachAvg;
    private float retreatAvg;

    private float fastAggressive;
    private float slowAggressive;
    private float fastDefensive;
    private float slowDefensive;

    private int lastAttackType;
    private bool wasBlocking;
    private bool wasDodging;
    private float lastDistance;

    private void Awake()
    {
        history = new CombatActionType[historyLength];

        if (enemy == null)
            enemy = transform;

        if (player != null)
            SetPlayer(player);
    }

    // Call this to tell the analyzer who to watch (used later by training).
    public void SetPlayer(GameObject newPlayer)
    {
        player = newPlayer;
        combatant = player.GetComponent<ICombatant>();

        if (combatant == null)
        {
            Debug.LogError("PlayerCombatAnalyzer: the player has no ICombatant component. " +
                           "Add PlayerCombatantAdapter to the Player.");
            return;
        }

        ResetStats();
    }

    // Call this at the start of every new fight.
    public void ResetStats()
    {
        timeInFight = 0f;

        attackRate = 0f;
        dodgeRate = 0f;
        blockAvg = 0f;
        approachAvg = 0f;
        retreatAvg = 0f;

        fastAggressive = 0f;
        slowAggressive = 0f;
        fastDefensive = 0f;
        slowDefensive = 0f;

        Repetition = 0f;
        historyHead = 0;
        historyCount = 0;

        if (combatant != null)
        {
            lastAttackType = combatant.AttackType;
            wasBlocking = combatant.IsBlocking;
            wasDodging = combatant.IsDodging;
            lastDistance = CurrentDistance();
        }
    }

    // ago = 0 is the newest action, 1 is the one before, and so on.
    public CombatActionType GetRecentAction(int ago)
    {
        if (ago < 0 || ago >= historyCount)
            return CombatActionType.None;

        int index = (historyHead - 1 - ago + history.Length * 2) % history.Length;
        return history[index];
    }

    private void Update()
    {
        if (combatant == null)
            return;

        float dt = Time.deltaTime;
        if (dt <= 0f)
            return;

        timeInFight += dt;

        int attackType = combatant.AttackType;
        bool blocking = combatant.IsBlocking;
        bool dodging = combatant.IsDodging;

        // 1) Count new actions (an action counts when it STARTS).
        float decay = Mathf.Exp(-dt / rateWindow);
        attackRate *= decay;
        dodgeRate *= decay;

        if (attackType > 0 && attackType != lastAttackType)
        {
            RegisterAction((CombatActionType)attackType);
            attackRate += 1f / rateWindow;
        }

        if (blocking && !wasBlocking)
            RegisterAction(CombatActionType.Block);

        if (dodging && !wasDodging)
        {
            RegisterAction(CombatActionType.Dodge);
            dodgeRate += 1f / rateWindow;
        }

        lastAttackType = attackType;
        wasBlocking = blocking;
        wasDodging = dodging;

        // 2) Is the player walking toward or away from the enemy?
        float distance = CurrentDistance();
        float closingSpeed = (lastDistance - distance) / dt;
        lastDistance = distance;

        bool approaching = closingSpeed > moveSpeedThreshold;
        bool retreating = closingSpeed < -moveSpeedThreshold;

        // 3) Slowly update the averages.
        blockAvg = Average(blockAvg, blocking ? 1f : 0f, dt, rateWindow);
        approachAvg = Average(approachAvg, approaching ? 1f : 0f, dt, rateWindow);
        retreatAvg = Average(retreatAvg, retreating ? 1f : 0f, dt, rateWindow);

        float aggressiveNow = (attackType > 0 || approaching) ? 1f : 0f;
        float defensiveNow = (blocking || dodging || retreating) ? 1f : 0f;

        fastAggressive = Average(fastAggressive, aggressiveNow, dt, fastWindow);
        slowAggressive = Average(slowAggressive, aggressiveNow, dt, slowWindow);
        fastDefensive = Average(fastDefensive, defensiveNow, dt, fastWindow);
        slowDefensive = Average(slowDefensive, defensiveNow, dt, slowWindow);
    }

    // Moves 'current' toward 'target'. A bigger window means slower change.
    // At the start of a fight it reacts faster, so early numbers are not wrong.
    private float Average(float current, float target, float dt, float window)
    {
        float usedWindow = Mathf.Min(window, timeInFight + 0.25f);
        float amount = 1f - Mathf.Exp(-dt / usedWindow);
        return current + (target - current) * amount;
    }

    private float CurrentDistance()
    {
        Vector3 diff = combatant.CombatTransform.position - enemy.position;
        diff.y = 0f;
        return diff.magnitude;
    }

    private void RegisterAction(CombatActionType type)
    {
        history[historyHead] = type;
        historyHead = (historyHead + 1) % history.Length;

        if (historyCount < history.Length)
            historyCount++;

        // Repetition = how much of the history is the same action.
        for (int i = 0; i < counts.Length; i++)
            counts[i] = 0;

        int most = 0;
        for (int i = 0; i < historyCount; i++)
        {
            int c = ++counts[(int)history[i]];
            if (c > most)
                most = c;
        }

        Repetition = historyCount < 4 ? 0f : (float)most / historyCount;
    }

    private void OnGUI()
    {
        if (!showDebug || combatant == null)
            return;

        float top = Screen.height - 230f;
        GUI.Box(new Rect(10f, top, 250f, 220f), "Player Combat Analyzer");

        string text =
            "Attack frequency:  " + AttackFrequency.ToString("F2") + "\n" +
            "Dodge frequency:   " + DodgeFrequency.ToString("F2") + "\n" +
            "Block tendency:    " + BlockTendency.ToString("F2") + "\n" +
            "Approach tendency: " + ApproachTendency.ToString("F2") + "\n" +
            "Retreat tendency:  " + RetreatTendency.ToString("F2") + "\n" +
            "Aggression:        " + Aggression.ToString("F2") + "\n" +
            "Defensiveness:     " + Defensiveness.ToString("F2") + "\n" +
            "Repetition:        " + Repetition.ToString("F2") + "\n" +
            "Behavior change:   " + BehaviorChange.ToString("F2") + "\n" +
            "Last action:       " + GetRecentAction(0) + "\n" +
            "Actions remembered: " + historyCount;

        GUI.Label(new Rect(20f, top + 25f, 235f, 190f), text);
    }
}
