using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance;

    [Range(0f, 5f)] public float intensity = 0.5f;
    public float duration = 0.2f;
    public float frequency = 2f;
    public NoiseSettings noiseProfile;

    [Header("Shake presets (power, seconds)")]
    [Tooltip("Small shake when your sword hits an enemy.")]
    public Vector2 onHit = new Vector2(0.35f, 0.12f);
    [Tooltip("Medium shake when your shield blocks an attack.")]
    public Vector2 onBlock = new Vector2(0.9f, 0.2f);
    [Tooltip("Strong shake when you take damage.")]
    public Vector2 onDamage = new Vector2(1.3f, 0.3f);

    CinemachineBasicMultiChannelPerlin noise;
    float baseAmplitude;
    float baseFrequency;
    NoiseSettings baseProfile;
    Coroutine routine;

    void Awake()
    {
        Instance = this;
    }

    bool FindNoise()
    {
        if (noise != null) return true;
        noise = FindFirstObjectByType<CinemachineBasicMultiChannelPerlin>();
        if (noise == null) return false;
        baseProfile = noise.NoiseProfile;
        baseAmplitude = noise.AmplitudeGain;
        baseFrequency = noise.FrequencyGain;
        return true;
    }

    public void ShakeOnHit() { Shake(onHit.x, onHit.y); }
    public void ShakeOnBlock() { Shake(onBlock.x, onBlock.y); }
    public void ShakeOnDamage() { Shake(onDamage.x, onDamage.y); }

    public void Shake()
    {
        Shake(intensity, duration);
    }

    public void Shake(float power, float time)
    {
        if (!FindNoise()) return;
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(DoShake(power, time));
    }

    IEnumerator DoShake(float power, float time)
    {
        if (noiseProfile != null) noise.NoiseProfile = noiseProfile;
        noise.FrequencyGain = frequency;
        float t = 0f;
        while (t < time)
        {
            float fade = 1f - (t / time);
            noise.AmplitudeGain = baseAmplitude + power * fade;
            t += Time.unscaledDeltaTime;
            yield return null;
        }
        noise.AmplitudeGain = baseAmplitude;
        noise.FrequencyGain = baseFrequency;
        noise.NoiseProfile = baseProfile;
    }
}
