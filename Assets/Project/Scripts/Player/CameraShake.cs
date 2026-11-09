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
