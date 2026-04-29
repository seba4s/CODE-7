using UnityEngine;
using UnityEngine.UI;

public static class UIRuntimeStyle
{
    static Sprite roundedButtonSprite;
    static Sprite roundedPanelSprite;

    public static Sprite GetRoundedButtonSprite()
    {
        if (roundedButtonSprite == null)
            roundedButtonSprite = CreateRoundedSprite(128, 30f);

        return roundedButtonSprite;
    }

    public static Sprite GetRoundedPanelSprite()
    {
        if (roundedPanelSprite == null)
            roundedPanelSprite = CreateRoundedSprite(128, 18f);

        return roundedPanelSprite;
    }

    public static void ApplyRoundedButtonStyle(Image image, Color color)
    {
        if (image == null) return;
        image.sprite = GetRoundedButtonSprite();
        image.type = Image.Type.Sliced;
        image.color = color;
    }

    static Sprite CreateRoundedSprite(int size, float radius)
    {
        var texture = new Texture2D(size, size, TextureFormat.ARGB32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        var pixels = new Color32[size * size];
        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        Vector2 half = new Vector2((size - 1) * 0.5f - 2f, (size - 1) * 0.5f - 2f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Abs(x - center.x) - (half.x - radius);
                float dy = Mathf.Abs(y - center.y) - (half.y - radius);
                float outsideX = Mathf.Max(dx, 0f);
                float outsideY = Mathf.Max(dy, 0f);
                float distance = Mathf.Sqrt(outsideX * outsideX + outsideY * outsideY);
                bool inside = distance <= radius;
                pixels[y * size + x] = inside ? new Color32(255, 255, 255, 255) : new Color32(255, 255, 255, 0);
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply();

        var border = Mathf.RoundToInt(radius);
        return Sprite.Create(
            texture,
            new Rect(0, 0, size, size),
            new Vector2(0.5f, 0.5f),
            100f,
            0u,
            SpriteMeshType.FullRect,
            new Vector4(border, border, border, border));
    }
}