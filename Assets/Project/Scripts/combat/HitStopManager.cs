using System.Collections;
using UnityEngine;

/// <summary>
/// Freezes (or heavily slows) the game for a split second on impact.
/// Created automatically on first use - you don't need to place it in the scene.
/// Call: HitStopManager.Trigger(duration, timeScale)
/// </summary>
public class HitStopManager : MonoBehaviour
{
    private static HitStopManager instance;

    private bool active;
    private float savedTimeScale = 1f;
    private float appliedTimeScale;
    private float endTime;
    private Coroutine routine;

    public static void Trigger(float duration, float timeScale = 0f)
    {
        if (duration <= 0f) return;

        if (instance == null)
        {
            GameObject go = new GameObject("[HitStopManager]");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<HitStopManager>();
        }

        instance.Begin(duration, Mathf.Clamp01(timeScale));
    }

    private void Begin(float duration, float timeScale)
    {
        // Don't start a hit stop while the game is already paused
        if (!active && Time.timeScale <= 0f) return;

        if (!active)
        {
            savedTimeScale = Time.timeScale;
            active = true;
        }

        appliedTimeScale = timeScale;
        Time.timeScale = appliedTimeScale;

        // Overlapping hits extend the freeze instead of stacking
        endTime = Mathf.Max(endTime, Time.unscaledTime + duration);

        if (routine == null)
            routine = StartCoroutine(Run());
    }

    private IEnumerator Run()
    {
        while (Time.unscaledTime < endTime)
            yield return null;

        Restore();
    }

    private void Restore()
    {
        // Only restore if nobody else (e.g. a pause menu) changed the time scale meanwhile
        if (active && Mathf.Approximately(Time.timeScale, appliedTimeScale))
            Time.timeScale = savedTimeScale;

        active = false;
        routine = null;
        endTime = 0f;
    }

    private void OnDisable()
    {
        if (active) Restore();
    }
}
