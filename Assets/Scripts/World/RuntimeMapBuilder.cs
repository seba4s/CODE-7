using UnityEngine;

/// <summary>
/// Construye un tramo jugable hacia la derecha en tiempo de ejecución:
/// suelo largo, bases y plataformas para saltar.
/// </summary>
public class RuntimeMapBuilder : MonoBehaviour
{
    [Header("Activación")]
    public bool buildOnStart = true;

    [Header("Capas y visual")]
    public string groundLayerName = "Ground";
    public Color groundColor = new Color(0.15f, 0.18f, 0.23f);
    public Color platformColor = new Color(0.23f, 0.28f, 0.35f);

    [Header("Límites de generación")]
    public float startX = -12f;
    public float endX = 120f;
    public float groundY = -1.8f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoInstall()
    {
        if (FindAnyObjectByType<RuntimeMapBuilder>() != null) return;

        var go = new GameObject("RuntimeMapBuilder");
        go.AddComponent<RuntimeMapBuilder>();
        Debug.Log("[RuntimeMapBuilder] Auto-instalado.");
    }

    void Start()
    {
        if (!buildOnStart) return;
        Build();
    }

    public void Build()
    {
        var existing = GameObject.Find("RuntimeMap");
        if (existing != null) return;

        var root = new GameObject("RuntimeMap");
        int groundLayer = LayerMask.NameToLayer(groundLayerName);
        if (groundLayer < 0) groundLayer = 0;

        // Suelo principal continuo
        CreateBlock(root.transform, "MainGround", new Vector2((startX + endX) * 0.5f, groundY - 0.6f), new Vector2(endX - startX, 2.4f), groundLayer, groundColor, 0);

        // Bases (pequeñas plataformas grandes)
        CreateBlock(root.transform, "Base_A", new Vector2(10f, 0.4f), new Vector2(8f, 0.8f), groundLayer, platformColor, 1);
        CreateBlock(root.transform, "Base_B", new Vector2(28f, 1.2f), new Vector2(7f, 0.8f), groundLayer, platformColor, 1);
        CreateBlock(root.transform, "Base_C", new Vector2(46f, 2.1f), new Vector2(9f, 0.8f), groundLayer, platformColor, 1);
        CreateBlock(root.transform, "Base_D", new Vector2(70f, 1.5f), new Vector2(10f, 0.8f), groundLayer, platformColor, 1);
        CreateBlock(root.transform, "Base_E", new Vector2(96f, 2.6f), new Vector2(12f, 0.8f), groundLayer, platformColor, 1);

        // Ruta de salto progresiva a la derecha
        CreateBlock(root.transform, "Jump_01", new Vector2(16f, 2.6f), new Vector2(3.2f, 0.55f), groundLayer, platformColor, 2);
        CreateBlock(root.transform, "Jump_02", new Vector2(21f, 3.8f), new Vector2(3.2f, 0.55f), groundLayer, platformColor, 2);
        CreateBlock(root.transform, "Jump_03", new Vector2(26f, 5.0f), new Vector2(3.2f, 0.55f), groundLayer, platformColor, 2);
        CreateBlock(root.transform, "Jump_04", new Vector2(35f, 3.4f), new Vector2(3.5f, 0.55f), groundLayer, platformColor, 2);
        CreateBlock(root.transform, "Jump_05", new Vector2(40f, 4.7f), new Vector2(3.5f, 0.55f), groundLayer, platformColor, 2);
        CreateBlock(root.transform, "Jump_06", new Vector2(54f, 5.6f), new Vector2(4f, 0.55f), groundLayer, platformColor, 2);
        CreateBlock(root.transform, "Jump_07", new Vector2(61f, 4.1f), new Vector2(4f, 0.55f), groundLayer, platformColor, 2);
        CreateBlock(root.transform, "Jump_08", new Vector2(77f, 3.6f), new Vector2(4.2f, 0.55f), groundLayer, platformColor, 2);
        CreateBlock(root.transform, "Jump_09", new Vector2(85f, 5.0f), new Vector2(4.2f, 0.55f), groundLayer, platformColor, 2);
        CreateBlock(root.transform, "Jump_10", new Vector2(107f, 4.2f), new Vector2(4.5f, 0.55f), groundLayer, platformColor, 2);

        // Estructuras verticales (sensación de base/ruina)
        CreateBlock(root.transform, "Tower_A", new Vector2(32f, 0.8f), new Vector2(1.3f, 4f), groundLayer, groundColor, 0);
        CreateBlock(root.transform, "Tower_B", new Vector2(58f, 0.9f), new Vector2(1.3f, 5f), groundLayer, groundColor, 0);
        CreateBlock(root.transform, "Tower_C", new Vector2(90f, 1.0f), new Vector2(1.3f, 6f), groundLayer, groundColor, 0);

        Debug.Log("[RuntimeMapBuilder] Mapa extendido creado hacia la derecha.");
    }

    static GameObject CreateBlock(Transform parent, string name, Vector2 position, Vector2 size, int layer, Color color, int sortingOrder)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.position = position;
        go.transform.localScale = size;
        go.layer = layer;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = CreateSquareSprite();
        sr.color = color;
        sr.sortingOrder = sortingOrder;

        var col = go.AddComponent<BoxCollider2D>();
        col.size = Vector2.one;

        return go;
    }

    static Sprite CreateSquareSprite()
    {
        const int size = 32;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;

        var pixels = new Color32[size * size];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color32(255, 255, 255, 255);
        tex.SetPixels32(pixels);
        tex.Apply();

        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }
}
