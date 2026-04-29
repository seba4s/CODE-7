using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Panel de referencia rápida de controles, superpuesto al menú principal.
/// Para el tutorial jugable en partida, ver PlayableTutorialDirector.
/// </summary>
public class TutorialOverlayRuntime : MonoBehaviour
{
    static TutorialOverlayRuntime _instance;

    /// <summary>Muestra el panel de controles encima del menú.</summary>
    public static void ShowStandalone()
    {
        if (_instance != null) return;
        var go = new GameObject("ControlsReference");
        _instance = go.AddComponent<TutorialOverlayRuntime>();
    }

    void Start()
    {
        BuildOverlay();
    }

    void BuildOverlay()
    {
        // Canvas
        var canvasGO = new GameObject("TutorialCanvas");
        canvasGO.transform.SetParent(transform, false);
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGO.AddComponent<GraphicRaycaster>();

        // Fondo semitransparente
        var bg = MakeImage(canvasGO.transform, "Bg", new Color(0f, 0f, 0f, 0.88f));
        Stretch(bg);

        // Panel contenedor
        var panel = MakeImage(bg.transform, "Panel", new Color(0.08f, 0.08f, 0.15f, 1f));
        var panelRT = panel.GetComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(0.15f, 0.08f);
        panelRT.anchorMax = new Vector2(0.85f, 0.92f);
        panelRT.offsetMin = panelRT.offsetMax = Vector2.zero;

        var vl = panel.AddComponent<VerticalLayoutGroup>();
        vl.childAlignment = TextAnchor.UpperCenter;
        vl.spacing = 18f;
        vl.padding = new RectOffset(60, 60, 40, 40);
        vl.childForceExpandWidth = true;
        vl.childForceExpandHeight = false;

        // Título
        AddText(panel.transform, "Título", "CONTROLES", 64,
            new Color(0.9f, 0.8f, 0.2f), FontStyle.Bold, 90f);

        // Línea decorativa
        var line = MakeImage(panel.transform, "Line", new Color(0.9f, 0.8f, 0.2f, 0.5f));
        line.GetComponent<LayoutElement>().minHeight = 3f;

        AddSpacer(panel.transform, 10f);

        // ── Filas de controles ────────────────────────────────────
        // Movimiento
        AddControlRow(panel.transform,
            "Moverse",
            "A / D    o    ← →",
            "Camina y corre en ambas direcciones.");

        // Saltar
        AddControlRow(panel.transform,
            "Saltar",
            "Barra Espaciadora",
            "Presiona dos veces para doble salto.");

        // Deslizarse por la pared
        AddControlRow(panel.transform,
            "Pared",
            "(Acércate a una pared)",
            "Te deslizas lentamente, puedes saltar desde ella.");

        // Dash
        AddControlRow(panel.transform,
            "Dash",
            "Shift   +   ← / →",
            "Impulso rápido. Consume estamina.");

            // Apuntar con mouse
            AddControlRow(panel.transform,
                "Apuntar",
                "Mover el Mouse",
                "El personaje apunta siempre hacia el cursor.");

            // Disparar
            AddControlRow(panel.transform,
                "Disparar",
                "Clic Izquierdo",
                "Disparo de hitscan en dirección del cursor.");

        AddSpacer(panel.transform, 20f);

        // Consejos
        var tipsTitle = AddText(panel.transform, "TipsTitle", "CONSEJOS", 30,
            new Color(0.6f, 0.9f, 0.6f), FontStyle.Bold, 42f);

        AddText(panel.transform, "Tip1",
            "• El dash te vuelve invulnerable durante un instante.",
            24, new Color(0.8f, 0.9f, 0.8f), FontStyle.Normal, 34f);

        AddText(panel.transform, "Tip2",
            "• La estamina se regenera con el tiempo.",
            24, new Color(0.8f, 0.9f, 0.8f), FontStyle.Normal, 34f);

        AddText(panel.transform, "Tip3",
            "• Mueres si tu salud llega a 0 — ¡aparecerás en el último punto de control!",
            24, new Color(0.8f, 0.9f, 0.8f), FontStyle.Normal, 34f);

        AddSpacer(panel.transform, 24f);

        // Botón
        CreateButton(panel.transform, "Volver al Menú",
            new Color(0.2f, 0.4f, 0.7f), OnStartClicked);
    }

    // ── Callback ─────────────────────────────────────────────────

    void OnStartClicked()
    {
        _instance = null;
        Destroy(gameObject);
    }

    // ── Helpers de UI ────────────────────────────────────────────

    void AddControlRow(Transform parent, string action, string keys, string desc)
    {
        var row = new GameObject("Row_" + action);
        row.transform.SetParent(parent, false);
        var le = row.AddComponent<LayoutElement>();
        le.minHeight = 70f;
        var hl = row.AddComponent<HorizontalLayoutGroup>();
        hl.childAlignment = TextAnchor.MiddleLeft;
        hl.spacing = 20f;
        hl.childForceExpandWidth = false;
        hl.childForceExpandHeight = true;

        // Columna acción
        var colAction = MakeFlex(row.transform, "ColAction", 0.22f);
        var txtAction = colAction.AddComponent<Text>();
        txtAction.text = action;
        txtAction.fontSize = 28;
        txtAction.color = new Color(0.7f, 0.85f, 1f);
        txtAction.fontStyle = FontStyle.Bold;
        txtAction.alignment = TextAnchor.MiddleLeft;
        txtAction.font = GetUIFont();

        // Columna teclas — fondo resaltado
        var keyBg = MakeImage(row.transform, "KeyBg", new Color(0.15f, 0.15f, 0.3f, 1f));
        var keyBgLE = keyBg.GetComponent<LayoutElement>();
        keyBgLE.preferredWidth = 320f;
        keyBgLE.minHeight = 50f;
        var txtKey = new GameObject("TxtKey");
        txtKey.transform.SetParent(keyBg.transform, false);
        var tkRT = txtKey.AddComponent<RectTransform>();
        tkRT.anchorMin = Vector2.zero; tkRT.anchorMax = Vector2.one;
        tkRT.offsetMin = new Vector2(10, 0); tkRT.offsetMax = new Vector2(-10, 0);
        var tk = txtKey.AddComponent<Text>();
        tk.text = keys;
        tk.fontSize = 26;
        tk.color = new Color(1f, 0.95f, 0.5f);
        tk.fontStyle = FontStyle.Bold;
        tk.alignment = TextAnchor.MiddleCenter;
        tk.font = GetUIFont();

        // Columna descripción
        var colDesc = MakeFlex(row.transform, "ColDesc", 1f);
        var txtDesc = colDesc.AddComponent<Text>();
        txtDesc.text = desc;
        txtDesc.fontSize = 22;
        txtDesc.color = new Color(0.75f, 0.75f, 0.75f);
        txtDesc.alignment = TextAnchor.MiddleLeft;
        txtDesc.font = GetUIFont();
    }

    GameObject MakeFlex(Transform parent, string name, float flexWidth)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var le = go.AddComponent<LayoutElement>();
        le.flexibleWidth = flexWidth;
        return go;
    }

    GameObject MakeImage(Transform parent, string name, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        var img = go.AddComponent<Image>();
        img.color = color;
        go.AddComponent<LayoutElement>();
        return go;
    }

    void Stretch(GameObject go)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    GameObject AddText(Transform parent, string name, string content,
        int size, Color color, FontStyle style, float minH)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var le = go.AddComponent<LayoutElement>();
        le.minHeight = minH;
        var txt = go.AddComponent<Text>();
        txt.text = content;
        txt.fontSize = size;
        txt.color = color;
        txt.fontStyle = style;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.font = GetUIFont();
        return go;
    }

    void AddSpacer(Transform parent, float height)
    {
        var go = new GameObject("Spacer");
        go.transform.SetParent(parent, false);
        go.AddComponent<LayoutElement>().minHeight = height;
    }

    void CreateButton(Transform parent, string label, Color bgColor,
        UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject("Btn_" + label);
        go.transform.SetParent(parent, false);
        var le = go.AddComponent<LayoutElement>();
        le.minHeight = 80f;
        le.preferredWidth = 600f;
        var img = go.AddComponent<Image>();
        UIRuntimeStyle.ApplyRoundedButtonStyle(img, bgColor);
        var btn = go.AddComponent<Button>();
        var colors = btn.colors;
        colors.highlightedColor = new Color(
            Mathf.Min(bgColor.r + 0.2f, 1f),
            Mathf.Min(bgColor.g + 0.2f, 1f),
            Mathf.Min(bgColor.b + 0.2f, 1f));
        btn.colors = colors;
        btn.onClick.AddListener(onClick);

        var txtGO = new GameObject("Label");
        txtGO.transform.SetParent(go.transform, false);
        var tr = txtGO.AddComponent<RectTransform>();
        tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one;
        tr.offsetMin = tr.offsetMax = Vector2.zero;
        var txt = txtGO.AddComponent<Text>();
        txt.text = label;
        txt.fontSize = 38;
        txt.color = Color.white;
        txt.fontStyle = FontStyle.Bold;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.font = GetUIFont();
    }

    static Font GetUIFont()
    {
        var f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (f == null) f = Resources.GetBuiltinResource<Font>("Arial.ttf");
        return f;
    }
}
