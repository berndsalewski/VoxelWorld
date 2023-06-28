using UnityEngine;

/// <summary>
/// provides proper ui scaling for imgui ui on retina screena.
/// </summary>
static public class UIScaler
{
    static readonly public int scaleFactor = Screen.dpi > 200 && !Application.isEditor ? 2 : 1;

    static readonly public GUIStyle scaledStyle = new GUIStyle()
    {
        fontSize = 14 * scaleFactor,
        alignment = TextAnchor.UpperLeft,
        normal = new GUIStyleState()
        {
            textColor = Color.white,
            background = Texture2D.grayTexture
        },
        padding = new RectOffset(5, 0, 5, 0),
    };

    static public Rect GetScaledRect(int x, int y, int width, int height)
    {
        return new Rect(x * scaleFactor, y * scaleFactor, width * scaleFactor, height * scaleFactor);
    }
}