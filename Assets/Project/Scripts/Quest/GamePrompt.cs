using UnityEngine;

public class GamePrompt : MonoBehaviour
{
    private static GamePrompt instance;
    private string text;
    private int lastFrame = -10;
    private GUIStyle style;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Create()
    {
        if (instance != null) return;
        var go = new GameObject("GamePrompt");
        instance = go.AddComponent<GamePrompt>();
    }

    public static void Show(string message)
    {
        if (instance == null) Create();
        instance.text = message;
        instance.lastFrame = Time.frameCount;
    }

    private void OnGUI()
    {
        if (string.IsNullOrEmpty(text)) return;
        if (Time.frameCount - lastFrame > 1) return;

        if (style == null)
        {
            style = new GUIStyle(GUI.skin.label);
            style.alignment = TextAnchor.MiddleCenter;
            style.fontSize = 26;
            style.fontStyle = FontStyle.Bold;
            style.normal.textColor = Color.white;
        }

        Rect r = new Rect(0, Screen.height * 0.72f, Screen.width, 60);
        GUI.color = Color.black;
        GUI.Label(new Rect(r.x + 2, r.y + 2, r.width, r.height), text, style);
        GUI.color = Color.white;
        GUI.Label(r, text, style);
    }
}
