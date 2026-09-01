using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class BossHealthBarUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image healthFill;
    [SerializeField] private Image damageFill;
    [SerializeField] private TMP_Text bossNameText;

    [Header("Boss Name")]
    [SerializeField] private string bossName = "KING OF DARKNESS";

    [Header("Health")]
    [SerializeField] private float maxHealth = 1000f;

    [Header("Health Colors")]
    [SerializeField] private Color fullHealthColor = Color.green;
    [SerializeField] private Color mediumHealthColor = Color.yellow;
    [SerializeField] private Color lowHealthColor = Color.red;

    [Header("Damage Indicator")]
    [SerializeField] private Color damageColor = new Color(1f, 0.55f, 0f, 1f);

    [SerializeField] private bool smoothHealth = true;

    [SerializeField] private float smoothSpeed = 8f;

    [SerializeField] private float damageDelay = 0.25f;

    [SerializeField] private float damageCatchupSpeed = 2.5f;

    private float currentHealth;

    private float displayedHealth;

    private float displayedDamageHealth;

    private RectTransform healthFillRect;

    private RectTransform damageFillRect;

    private Coroutine hideCoroutine;

    private Coroutine damageCoroutine;


    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        currentHealth = maxHealth;

        displayedHealth = maxHealth;

        displayedDamageHealth = maxHealth;

        // -----------------------------------------------------
        // HEALTH FILL
        // -----------------------------------------------------

        if (healthFill != null)
        {
            healthFillRect =
                healthFill.GetComponent<RectTransform>();

            healthFillRect.localScale =
                Vector3.one;
        }

        // -----------------------------------------------------
        // DAMAGE FILL
        // -----------------------------------------------------

        if (damageFill != null)
        {
            damageFillRect =
                damageFill.GetComponent<RectTransform>();

            damageFillRect.localScale =
                Vector3.one;

            damageFill.color =
                damageColor;
        }

        // -----------------------------------------------------
        // BOSS NAME
        // -----------------------------------------------------

        if (bossNameText != null)
        {
            bossNameText.text =
                bossName;
        }

        UpdateHealthBar();
    }


    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        if (healthFill == null)
            return;

        // -----------------------------------------------------
        // GET HEALTH RECT
        // -----------------------------------------------------

        if (healthFillRect == null)
        {
            healthFillRect =
                healthFill.GetComponent<RectTransform>();
        }

        if (healthFillRect == null)
            return;

        // -----------------------------------------------------
        // GET DAMAGE RECT
        // -----------------------------------------------------

        if (damageFill != null &&
            damageFillRect == null)
        {
            damageFillRect =
                damageFill.GetComponent<RectTransform>();
        }

        // -----------------------------------------------------
        // SMOOTH HEALTH
        // -----------------------------------------------------

        if (smoothHealth)
        {
            displayedHealth =
                Mathf.Lerp(
                    displayedHealth,
                    currentHealth,
                    smoothSpeed *
                    Time.deltaTime
                );

            if (Mathf.Abs(
                displayedHealth -
                currentHealth
            ) < 0.1f)
            {
                displayedHealth =
                    currentHealth;
            }
        }
        else
        {
            displayedHealth =
                currentHealth;
        }

        UpdateHealthBar();
    }


    // =========================================================
    // SET MAX HEALTH
    // =========================================================

    public void SetMaxHealth(int health)
    {
        maxHealth =
            Mathf.Max(
                1,
                health
            );

        currentHealth =
            maxHealth;

        displayedHealth =
            maxHealth;

        displayedDamageHealth =
            maxHealth;

        if (healthFillRect == null &&
            healthFill != null)
        {
            healthFillRect =
                healthFill.GetComponent<RectTransform>();
        }

        if (damageFillRect == null &&
            damageFill != null)
        {
            damageFillRect =
                damageFill.GetComponent<RectTransform>();
        }

        // -----------------------------------------------------
        // RESET RED BAR
        // -----------------------------------------------------

        if (healthFillRect != null)
        {
            healthFillRect.localScale =
                Vector3.one;
        }

        // -----------------------------------------------------
        // RESET ORANGE BAR
        // -----------------------------------------------------

        if (damageFillRect != null)
        {
            damageFillRect.localScale =
                Vector3.one;
        }

        UpdateHealthBar();
    }


    // =========================================================
    // SET HEALTH
    // =========================================================

    public void SetHealth(int health)
    {
        float previousHealth =
            currentHealth;

        currentHealth =
            Mathf.Clamp(
                health,
                0,
                Mathf.RoundToInt(maxHealth)
            );

        // -----------------------------------------------------
        // DAMAGE DETECTED
        // -----------------------------------------------------

        if (currentHealth < previousHealth)
        {
            StartDamageIndicator();
        }

        // -----------------------------------------------------
        // NO SMOOTH HEALTH
        // -----------------------------------------------------

        if (!smoothHealth)
        {
            displayedHealth =
                currentHealth;
        }

        Debug.Log(
            "BOSS UI HEALTH: " +
            currentHealth +
            "/" +
            maxHealth
        );

        UpdateHealthBar();
    }


    // =========================================================
    // START DAMAGE INDICATOR
    // =========================================================

    private void StartDamageIndicator()
    {
        if (damageFill == null)
            return;

        if (damageFillRect == null)
        {
            damageFillRect =
                damageFill.GetComponent<RectTransform>();
        }

        // -----------------------------------------------------
        // ORANGE STARTS AT CURRENT DISPLAYED HEALTH
        // -----------------------------------------------------

        float startingDamageHealth =
            Mathf.Max(
                displayedDamageHealth,
                currentHealth
            );

        displayedDamageHealth =
            startingDamageHealth;

        UpdateDamageFill();

        // -----------------------------------------------------
        // RESTART DAMAGE COROUTINE
        // -----------------------------------------------------

        if (damageCoroutine != null)
        {
            StopCoroutine(
                damageCoroutine
            );
        }

        damageCoroutine =
            StartCoroutine(
                DamageCatchup()
            );
    }


    // =========================================================
    // DAMAGE CATCHUP
    // =========================================================

    private IEnumerator DamageCatchup()
    {
        // -----------------------------------------------------
        // SMALL DELAY
        // -----------------------------------------------------

        yield return new WaitForSeconds(
            damageDelay
        );

        // -----------------------------------------------------
        // ORANGE FOLLOWS RED HEALTH
        // -----------------------------------------------------

        while (
            Mathf.Abs(
                displayedDamageHealth -
                displayedHealth
            ) > 0.1f
        )
        {
            displayedDamageHealth =
                Mathf.Lerp(
                    displayedDamageHealth,
                    displayedHealth,
                    damageCatchupSpeed *
                    Time.deltaTime
                );

            UpdateDamageFill();

            yield return null;
        }

        displayedDamageHealth =
            displayedHealth;

        UpdateDamageFill();

        damageCoroutine = null;
    }


    // =========================================================
    // UPDATE HEALTH BAR
    // =========================================================

    private void UpdateHealthBar()
    {
        if (healthFill == null)
            return;

        if (maxHealth <= 0)
            return;

        // -----------------------------------------------------
        // HEALTH PERCENT
        // -----------------------------------------------------

        float healthPercent =
            displayedHealth /
            maxHealth;

        healthPercent =
            Mathf.Clamp01(
                healthPercent
            );

        // -----------------------------------------------------
        // RED HEALTH BAR
        // -----------------------------------------------------

        if (healthFillRect != null)
        {
            Vector3 scale =
                healthFillRect.localScale;

            scale.x =
                healthPercent;

            scale.x =
                Mathf.Clamp01(
                    scale.x
                );

            healthFillRect.localScale =
                scale;
        }

        // -----------------------------------------------------
        // ORANGE DAMAGE BAR
        // -----------------------------------------------------

        UpdateDamageFill();

        // -----------------------------------------------------
        // HEALTH COLOR
        // -----------------------------------------------------

        float currentPercent =
            currentHealth /
            maxHealth;

        currentPercent =
            Mathf.Clamp01(
                currentPercent
            );

        if (currentPercent > 0.66f)
        {
            healthFill.color =
                fullHealthColor;
        }
        else if (currentPercent > 0.33f)
        {
            healthFill.color =
                mediumHealthColor;
        }
        else
        {
            healthFill.color =
                lowHealthColor;
        }
    }


    // =========================================================
    // UPDATE DAMAGE FILL
    // =========================================================

    private void UpdateDamageFill()
    {
        if (damageFill == null)
            return;

        if (damageFillRect == null)
        {
            damageFillRect =
                damageFill.GetComponent<RectTransform>();
        }

        if (damageFillRect == null)
            return;

        if (maxHealth <= 0)
            return;

        float damagePercent =
            displayedDamageHealth /
            maxHealth;

        damagePercent =
            Mathf.Clamp01(
                damagePercent
            );

        Vector3 scale =
            damageFillRect.localScale;

        scale.x =
            damagePercent;

        scale.x =
            Mathf.Clamp01(
                scale.x
            );

        damageFillRect.localScale =
            scale;

        damageFill.color =
            damageColor;
    }


    // =========================================================
    // SHOW BAR
    // =========================================================

    public void ShowBar()
    {
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(
                true
            );
        }

        if (hideCoroutine != null)
        {
            StopCoroutine(
                hideCoroutine
            );

            hideCoroutine = null;
        }
    }


    // =========================================================
    // HIDE BAR
    // =========================================================

    public void HideBar()
    {
        if (hideCoroutine != null)
        {
            StopCoroutine(
                hideCoroutine
            );

            hideCoroutine = null;
        }

        gameObject.SetActive(
            false
        );
    }


    // =========================================================
    // HIDE BAR DELAYED
    // =========================================================

    public void HideBarDelayed(float delay)
    {
        if (hideCoroutine != null)
        {
            StopCoroutine(
                hideCoroutine
            );
        }

        hideCoroutine =
            StartCoroutine(
                HideAfterDelay(
                    delay
                )
            );
    }


    // =========================================================
    // HIDE AFTER DELAY
    // =========================================================

    private IEnumerator HideAfterDelay(float delay)
    {
        yield return new WaitForSeconds(
            delay
        );

        gameObject.SetActive(
            false
        );

        hideCoroutine = null;
    }
}