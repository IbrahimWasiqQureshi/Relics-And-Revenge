using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class HealthBarUI : MonoBehaviour
{
    [SerializeField] private Slider slider;
    [SerializeField] private Image fillImage;
    [SerializeField] private Gradient gradient;
    [SerializeField] private float smoothSpeed = 8f;
    [SerializeField] private CanvasGroup canvasGroup;
    private Coroutine hideCoroutine;
    [SerializeField] private float hideDelay = 5f;

    private float targetHealth;

    private void Start()
    {
        targetHealth = slider.value;
    }

    private void Update()
    {
        slider.value = Mathf.MoveTowards(
            slider.value,
            targetHealth,
            smoothSpeed * 100f * Time.deltaTime);

        fillImage.color = gradient.Evaluate(slider.normalizedValue);
    }

    public void SetMaxHealth(float maxHealth)
    {
        slider.maxValue = maxHealth;
        slider.value = maxHealth;
        targetHealth = maxHealth;
    }

    public void SetHealth(float currentHealth)
    {
        targetHealth = currentHealth;
    }

    public void ShowBar()
    {
        if (canvasGroup == null)
            return;

        canvasGroup.alpha = 1f;

        if (hideCoroutine != null)
            StopCoroutine(hideCoroutine);

        hideCoroutine = StartCoroutine(AutoHide());
    }

    public void HideBar()
    {
        if (canvasGroup != null)
            canvasGroup.alpha = 0f;
    }

    public void HideBarDelayed(float delay)
    {
        StartCoroutine(HideRoutine(delay));
    }

    private IEnumerator HideRoutine(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (canvasGroup != null)
            canvasGroup.alpha = 0f;
    }

    private IEnumerator AutoHide()
    {
        yield return new WaitForSeconds(hideDelay);

        if (canvasGroup != null)
            canvasGroup.alpha = 0f;
    }
}