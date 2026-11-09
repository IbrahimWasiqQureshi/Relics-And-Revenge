using UnityEngine;
using UnityEditor;
using System.IO;

public class SceneViewScreenshot
{
    [MenuItem("Tools/HQ Scene Screenshot _F12")]
    static void Capture()
    {
        SceneView sv = SceneView.lastActiveSceneView;
        if (sv == null)
        {
            Debug.LogError("No Scene view found");
            return;
        }

        Camera cam = sv.camera;
        int w = 3840;
        int h = 2160;

        RenderTexture rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
        rt.antiAliasing = 8;

        RenderTexture oldTarget = cam.targetTexture;
        float oldAspect = cam.aspect;

        cam.targetTexture = rt;
        cam.aspect = (float)w / h;
        cam.Render();

        RenderTexture.active = rt;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        tex.Apply();

        cam.targetTexture = oldTarget;
        cam.aspect = oldAspect;
        RenderTexture.active = null;
        Object.DestroyImmediate(rt);

        string path = Path.Combine(Application.dataPath, "../SceneShot_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png");
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);

        Debug.Log("Saved: " + path);
    }
}