using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Added automatically to whatever gets hit. Temporarily swaps every mesh on the
/// object to a solid-colour material, then restores the original materials.
/// Works with any shader (including Shader Graph shaders with no colour property).
/// </summary>
public class HitFlashReceiver : MonoBehaviour
{
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    private struct Entry
    {
        public Renderer renderer;
        public Material[] original;
    }

    private readonly List<Entry> entries = new List<Entry>();
    private MaterialPropertyBlock block;
    private Coroutine routine;
    private bool flashing;

    public static void Flash(GameObject target, Material flashMaterial, Color color, float duration)
    {
        if (target == null || flashMaterial == null || duration <= 0f) return;

        HitFlashReceiver receiver = target.GetComponent<HitFlashReceiver>();
        if (receiver == null) receiver = target.AddComponent<HitFlashReceiver>();

        receiver.StartFlash(flashMaterial, color, duration);
    }

    private void StartFlash(Material flashMaterial, Color color, float duration)
    {
        if (!isActiveAndEnabled) return;
        if (block == null) block = new MaterialPropertyBlock();

        if (!flashing)
        {
            Capture();
            Apply(flashMaterial, color, true);
            flashing = true;
        }
        else
        {
            // Hit again mid-flash: just refresh colour and timer
            Apply(flashMaterial, color, false);
        }

        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(EndAfter(duration));
    }

    private void Capture()
    {
        entries.Clear();
        foreach (Renderer r in GetComponentsInChildren<Renderer>())
        {
            if (!(r is MeshRenderer) && !(r is SkinnedMeshRenderer)) continue;
            entries.Add(new Entry { renderer = r, original = r.sharedMaterials });
        }
    }

    private void Apply(Material flashMaterial, Color color, bool swapMaterials)
    {
        for (int i = 0; i < entries.Count; i++)
        {
            Renderer r = entries[i].renderer;
            if (r == null) continue;

            if (swapMaterials)
            {
                Material[] mats = new Material[entries[i].original.Length];
                for (int m = 0; m < mats.Length; m++) mats[m] = flashMaterial;
                r.sharedMaterials = mats;
            }

            r.GetPropertyBlock(block);
            block.SetColor(ColorId, color);
            r.SetPropertyBlock(block);
        }
    }

    private IEnumerator EndAfter(float duration)
    {
        // Unscaled so the flash lasts the same real time even during hit stop
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        Restore();
        routine = null;
    }

    private void Restore()
    {
        for (int i = 0; i < entries.Count; i++)
        {
            Renderer r = entries[i].renderer;
            if (r == null) continue;
            r.sharedMaterials = entries[i].original;
            r.SetPropertyBlock(null);
        }

        entries.Clear();
        flashing = false;
    }

    private void OnDisable()
    {
        if (flashing) Restore();
        routine = null;
    }
}
