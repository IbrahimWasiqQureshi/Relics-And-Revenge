using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Draws a clean outer-silhouette outline around an enemy for the lock-on system.
/// White = selected, Red = locked on.
/// Added automatically to enemies by LockOnManager.
///
/// How it works:
/// 1. A hidden copy of every mesh marks the pixels the character covers (stencil mask).
/// 2. A second hidden copy, slightly inflated, draws the outline color ONLY outside
///    those pixels. So you get just the outer edge, with no lines inside the body.
/// </summary>
public class LockOnOutline : MonoBehaviour
{
    public enum State { None, Selected, Locked }

    [Header("Colors")]
    [SerializeField] private Color selectedColor = Color.white;
    [SerializeField] private Color lockedColor = new Color(1f, 0.1f, 0.1f, 1f);

    [Header("Look")]
    [Tooltip("Outline thickness as a fraction of the distance to the camera.\n" +
             "0.001 = very thin, 0.003 = thin (default), 0.006 = thick.")]
    [Range(0.0005f, 0.02f)]
    [SerializeField] private float outlineWidth = 0.003f;

    private const string ProxyName = "LockOnOutlineProxy";

    private static readonly int ColorId = Shader.PropertyToID("_OutlineColor");
    private static readonly int WidthId = Shader.PropertyToID("_OutlineWidth");

    private Renderer[] sourceRenderers;

    private readonly List<Renderer> maskProxies = new List<Renderer>();
    private readonly List<Renderer> hullProxies = new List<Renderer>();
    private readonly List<Renderer> proxySources = new List<Renderer>();

    private Material maskMaterial;   // shared, never changes
    private Material hullMaterial;   // our own copy, so colors don't affect other enemies

    private State currentState = State.None;
    private bool built;
    private bool visible;
    private float appliedWidth = -1f;

    private void Awake()
    {
        var list = new List<Renderer>();

        foreach (Renderer r in GetComponentsInChildren<Renderer>(true))
        {
            if (r is MeshRenderer || r is SkinnedMeshRenderer)
                list.Add(r);
        }

        sourceRenderers = list.ToArray();

        Material maskTemplate = Resources.Load<Material>("LockOnOutlineMask");
        Material hullTemplate = Resources.Load<Material>("LockOnOutline");

        if (maskTemplate == null || hullTemplate == null)
        {
            Debug.LogError(
                "LockOnOutline: missing Resources/LockOnOutline or Resources/LockOnOutlineMask material.",
                this);
            enabled = false;
            return;
        }

        maskMaterial = maskTemplate;
        hullMaterial = new Material(hullTemplate);
    }

    public void SetState(State newState)
    {
        if (newState == currentState)
            return;

        currentState = newState;

        if (hullMaterial == null)
            return;

        if (newState == State.None)
        {
            SetVisible(false);
            return;
        }

        if (!built)
            BuildProxies();

        hullMaterial.SetColor(
            ColorId,
            newState == State.Locked ? lockedColor : selectedColor
        );

        SetVisible(true);
    }

    private void LateUpdate()
    {
        if (!visible)
            return;

        // Allows changing the thickness live in the Inspector.
        if (!Mathf.Approximately(appliedWidth, outlineWidth))
        {
            hullMaterial.SetFloat(WidthId, outlineWidth);
            appliedWidth = outlineWidth;
        }

        // Hide the outline of parts that are hidden (for example an unequipped weapon).
        for (int i = 0; i < proxySources.Count; i++)
        {
            Renderer source = proxySources[i];
            bool on = source != null && source.enabled && source.gameObject.activeInHierarchy;

            if (maskProxies[i] != null) maskProxies[i].enabled = on;
            if (hullProxies[i] != null) hullProxies[i].enabled = on;
        }
    }

    // -----------------------------------------------------------------
    // BUILD
    // -----------------------------------------------------------------

    private void BuildProxies()
    {
        built = true;

        foreach (Renderer source in sourceRenderers)
        {
            if (source == null)
                continue;

            int subMeshCount;
            Mesh mesh = GetMesh(source, out subMeshCount);

            if (mesh == null || subMeshCount == 0)
                continue;

            // One outline material per sub-mesh, so every part of the mesh gets covered.
            Renderer mask = CreateProxy(source, MakeArray(maskMaterial, subMeshCount));
            Renderer hull = CreateProxy(source, MakeArray(hullMaterial, subMeshCount));

            if (mask == null || hull == null)
                continue;

            proxySources.Add(source);
            maskProxies.Add(mask);
            hullProxies.Add(hull);
        }
    }

    private static Mesh GetMesh(Renderer source, out int subMeshCount)
    {
        Mesh mesh = null;

        SkinnedMeshRenderer skinned = source as SkinnedMeshRenderer;

        if (skinned != null)
        {
            mesh = skinned.sharedMesh;
        }
        else
        {
            MeshFilter filter = source.GetComponent<MeshFilter>();

            if (filter != null)
                mesh = filter.sharedMesh;
        }

        subMeshCount = mesh != null ? mesh.subMeshCount : 0;

        return mesh;
    }

    private static Material[] MakeArray(Material material, int count)
    {
        Material[] array = new Material[count];

        for (int i = 0; i < count; i++)
            array[i] = material;

        return array;
    }

    private Renderer CreateProxy(Renderer source, Material[] materials)
    {
        GameObject go = new GameObject(ProxyName);
        go.layer = source.gameObject.layer;
        go.transform.SetParent(source.transform, false);

        Renderer proxy;

        SkinnedMeshRenderer skinned = source as SkinnedMeshRenderer;

        if (skinned != null)
        {
            SkinnedMeshRenderer p = go.AddComponent<SkinnedMeshRenderer>();
            p.sharedMesh = skinned.sharedMesh;
            p.bones = skinned.bones;
            p.rootBone = skinned.rootBone;
            p.localBounds = skinned.localBounds;
            p.updateWhenOffscreen = skinned.updateWhenOffscreen;
            p.quality = skinned.quality;
            proxy = p;
        }
        else
        {
            MeshFilter filter = source.GetComponent<MeshFilter>();
            go.AddComponent<MeshFilter>().sharedMesh = filter.sharedMesh;
            proxy = go.AddComponent<MeshRenderer>();
        }

        proxy.sharedMaterials = materials;
        proxy.shadowCastingMode = ShadowCastingMode.Off;
        proxy.receiveShadows = false;
        proxy.lightProbeUsage = LightProbeUsage.Off;
        proxy.reflectionProbeUsage = ReflectionProbeUsage.Off;
        proxy.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
        proxy.enabled = false;

        return proxy;
    }

    // -----------------------------------------------------------------
    // SHOW / HIDE
    // -----------------------------------------------------------------

    private void SetVisible(bool show)
    {
        visible = show;

        for (int i = 0; i < maskProxies.Count; i++)
        {
            if (maskProxies[i] != null) maskProxies[i].enabled = show;
            if (hullProxies[i] != null) hullProxies[i].enabled = show;
        }
    }

    private void OnDisable()
    {
        SetVisible(false);
        currentState = State.None;
    }

    private void OnDestroy()
    {
        if (hullMaterial != null)
            Destroy(hullMaterial);
    }
}
