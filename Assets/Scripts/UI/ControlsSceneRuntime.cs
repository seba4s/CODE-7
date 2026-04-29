using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Pantalla completa de controles. Se auto-instala en ControlsScene.
/// Tiene scroll para que el contenido nunca se salga del recuadro
/// y un botón "Volver al Menú" que regresa a MainMenu.
/// </summary>
public class ControlsSceneRuntime : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void AutoInstall()
    {
        SceneManager.sceneLoaded += (scene, _) =>
        {
            if (!GameSceneConfig.IsControlsScene(scene.name)) return;
            if (FindAnyObjectByType<ControlsSceneRuntime>() != null) return;
            new GameObject("ControlsSceneRuntime").AddComponent<ControlsSceneRuntime>();
        };
    }

    void Start()
    {
        EnsureEventSystem();
        BuildPage();
    }

    // ── Construcción de la página ──────────────────────────────

    void BuildPage()
    {
        // Canvas raíz
        var canvasGO = new GameObject("ControlsCanvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGO.AddComponent<GraphicRaycaster>();

        // Fondo completo
        var bg = MakeRect(canvasGO.transform, "Bg");
        Stretch(bg);
        bg.AddComponent<Image>().color = new Color(0.04f, 0.05f, 0.12f, 1f);

        // ── Franja de título (superior fija) ───────────────────
        var header = MakeRect(bg.transform, "Header");
        var hRT = header.GetComponent<RectTransform>();
        hRT.anchorMin = new Vector2(0f, 1f);
        hRT.anchorMax = new Vector2(1f, 1f);
        hRT.pivot     = new Vector2(0.5f, 1f);
        hRT.anchoredPosition = Vector2.zero;
        hRT.sizeDelta = new Vector2(0f, 120f);
        header.AddComponent<Image>().color = new Color(0.08f, 0.08f, 0.18f, 1f);

        var titleTxt = MakeText(header.transform, "Title", "CONTROLES", 60,
            new Color(0.9f, 0.8f, 0.2f), FontStyle.Bold);
        Stretch(titleTxt);

        // Línea dorada bajo el header
        var accentRT = MakeRect(bg.transform, "Accent");
        var aRT = accentRT.GetComponent<RectTransform>();
        aRT.anchorMin = new Vector2(0f, 1f);
        aRT.anchorMax = new Vector2(1f, 1f);
        aRT.pivot     = new Vector2(0.5f, 1f);
        aRT.anchoredPosition = new Vector2(0f, -120f);
        aRT.sizeDelta = new Vector2(0f, 3f);
        accentRT.AddComponent<Image>().color = new Color(0.9f, 0.8f, 0.2f, 0.6f);

        // ── Franja de botón (inferior fija) ────────────────────
        var footer = MakeRect(bg.transform, "Footer");
        var fRT = footer.GetComponent<RectTransform>();
        fRT.anchorMin = new Vector2(0.3f, 0f);
        fRT.anchorMax = new Vector2(0.7f, 0f);
        fRT.pivot     = new Vector2(0.5f, 0f);
        fRT.anchoredPosition = new Vector2(0f, 24f);
        fRT.sizeDelta = new Vector2(0f, 70f);

        var backBtn = footer.AddComponent<Image>();
        backBtn.color = new Color(0.2f, 0.4f, 0.75f, 1f);
        var btn = footer.AddComponent<Button>();
        var colors = btn.colors;
        colors.highlightedColor = new Color(0.35f, 0.55f, 0.95f);
        colors.pressedColor     = new Color(0.15f, 0.3f, 0.6f);
        btn.colors = colors;
        btn.onClick.AddListener(GoBack);

        var btnTxt = MakeText(footer.transform, "BtnTxt", "← Volver al Menú", 32,
            Color.white, FontStyle.Bold);
        Stretch(btnTxt);

        // ── Área de scroll (entre header y footer) ─────────────
        var scrollArea = MakeRect(bg.transform, "ScrollArea");
        var saRT = scrollArea.GetComponent<RectTransform>();
        saRT.anchorMin = new Vector2(0.05f, 0f);
        saRT.anchorMax = new Vector2(0.95f, 1f);
        saRT.offsetMin = new Vector2(0f,  120f);  // encima del footer
        saRT.offsetMax = new Vector2(0f, -130f);  // debajo del header

        var scrollRect = scrollArea.AddComponent<ScrollRect>();
        scrollRect.horizontal     = false;
        scrollRect.vertical       = true;
        scrollRect.scrollSensitivity = 30f;
        scrollRect.movementType   = ScrollRect.MovementType.Clamped;

        // Viewport
        var viewport = MakeRect(scrollArea.transform, "Viewport");
        Stretch(viewport);
        viewport.AddComponent<Image>().color = Color.clear;
        viewport.AddComponent<Mask>().showMaskGraphic = false;
        scrollRect.viewport = viewport.GetComponent<RectTransform>();

        // Content (crece verticalmente con el contenido)
        var content = MakeRect(viewport.transform, "Content");
        var cRT = content.GetComponent<RectTransform>();
        cRT.anchorMin = new Vector2(0f, 1f);
        cRT.anchorMax = new Vector2(1f, 1f);
        cRT.pivot     = new Vector2(0.5f, 1f);
        cRT.anchoredPosition = Vector2.zero;
        cRT.sizeDelta = new Vector2(0f, 0f);
        scrollRect.content = cRT;

        var csf = content.AddComponent<ContentSizeFitter>();
        csf.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;
        csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        var vl = content.AddComponent<VerticalLayoutGroup>();
        vl.childAlignment     = TextAnchor.UpperCenter;
        vl.spacing            = 12f;
        vl.padding            = new RectOffset(60, 60, 30, 30);
        vl.childForceExpandWidth  = true;
        vl.childForceExpandHeight = false;

        // ── Contenido: secciones ──────────────────────────────

        AddSectionTitle(content.transform, "MOVIMIENTO", new Color(0.5f, 0.8f, 1f));
        AddControlRow(content.transform, "Moverse",
            "A / D   ·   ← →",
            "Camina en ambas direcciones.");
        AddControlRow(content.transform, "Saltar",
            "Barra Espaciadora",
            "Presiona dos veces para doble salto.");
        AddControlRow(content.transform, "Pared",
            "(Acércate a una pared)",
            "Te deslizas lentamente; puedes saltar desde ella.");

        AddSpacer(content.transform, 16f);
        AddSectionTitle(content.transform, "COMBATE", new Color(1f, 0.5f, 0.4f));
        AddControlRow(content.transform, "Apuntar",
            "Mover el Mouse",
            "El personaje apunta siempre hacia el cursor.");
        AddControlRow(content.transform, "Disparar",
            "Clic Izquierdo",
            "Disparo hitscan en dirección del cursor.");
        AddControlRow(content.transform, "Dash",
            "Shift  +  ← / →",
            "Impulso rápido. Invulnerable mientras dura. Consume estamina.");

        AddSpacer(content.transform, 16f);
        AddSectionTitle(content.transform, "CONSEJOS", new Color(0.5f, 1f, 0.6f));
        AddTip(content.transform, "El dash te vuelve invulnerable durante un instante.");
        AddTip(content.transform, "La estamina se regenera sola con el tiempo.");
        AddTip(content.transform, "Mueres si tu salud llega a 0 — reapareces en el último punto de control.");

        AddSpacer(content.transform, 20f);
    }

    // ── Helpers de construcción ───────────────────────────────

    void AddSectionTitle(Transform parent, string text, Color color)
    {
        var go = MakeText(parent, "Sec_" + text, text, 36, color, FontStyle.Bold);
        var le = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
        le.minHeight = 52f;
        go.GetComponent<Text>().alignment = TextAnchor.MiddleLeft;
    }

    void AddControlRow(Transform parent, string action, string keys, string desc)
    {
        var row = new GameObject("Row_" + action);
        row.transform.SetParent(parent, false);
        var le = row.AddComponent<LayoutElement>();
        le.minHeight        = 64f;
        le.preferredHeight  = 64f;

        // Fondo alternado suave
        var bg = row.AddComponent<Image>();
        bg.color = new Color(1f, 1f, 1f, 0.03f);

        var hl = row.AddComponent<HorizontalLayoutGroup>();
        hl.childAlignment       = TextAnchor.MiddleLeft;
        hl.spacing              = 16f;
        hl.padding              = new RectOffset(12, 12, 0, 0);
        hl.childForceExpandWidth  = false;
        hl.childForceExpandHeight = true;

        // Columna acción (fija 220 px)
        var colAction = new GameObject("Action");
        colAction.transform.SetParent(row.transform, false);
        colAction.AddComponent<LayoutElement>().preferredWidth = 220f;
        var txtA = colAction.AddComponent<Text>();
        txtA.text      = action;
        txtA.fontSize  = 26;
        txtA.color     = new Color(0.7f, 0.85f, 1f);
        txtA.fontStyle = FontStyle.Bold;
        txtA.alignment = TextAnchor.MiddleLeft;
        txtA.font      = GetUIFont();

        // Columna teclas con fondo (fija 340 px)
        var keyBgGO = new GameObject("KeyBg");
        keyBgGO.transform.SetParent(row.transform, false);
        var keyLE = keyBgGO.AddComponent<LayoutElement>();
        keyLE.preferredWidth = 340f;
        keyLE.minHeight      = 46f;
        keyBgGO.AddComponent<Image>().color = new Color(0.15f, 0.15f, 0.3f, 1f);

        var keyTxt = new GameObject("KeyTxt");
        keyTxt.transform.SetParent(keyBgGO.transform, false);
        var kRT = keyTxt.AddComponent<RectTransform>();
        kRT.anchorMin = Vector2.zero; kRT.anchorMax = Vector2.one;
        kRT.offsetMin = new Vector2(10, 0); kRT.offsetMax = new Vector2(-10, 0);
        var tk = keyTxt.AddComponent<Text>();
        tk.text      = keys;
        tk.fontSize  = 24;
        tk.color     = new Color(1f, 0.95f, 0.5f);
        tk.fontStyle = FontStyle.Bold;
        tk.alignment = TextAnchor.MiddleCenter;
        tk.font      = GetUIFont();

        // Columna descripción (flexible)
        var colDesc = new GameObject("Desc");
        colDesc.transform.SetParent(row.transform, false);
        var descLE = colDesc.AddComponent<LayoutElement>();
        descLE.flexibleWidth = 1f;
        var txtD = colDesc.AddComponent<Text>();
        txtD.text         = desc;
        txtD.fontSize     = 22;
        txtD.color        = new Color(0.75f, 0.75f, 0.75f);
        txtD.alignment    = TextAnchor.MiddleLeft;
        txtD.font         = GetUIFont();
        txtD.resizeTextForBestFit = false;
    }

    void AddTip(Transform parent, string text)
    {
        var go = new GameObject("Tip");
        go.transform.SetParent(parent, false);
        var le = go.AddComponent<LayoutElement>();
        le.minHeight       = 36f;
        le.preferredHeight = 36f;

        var txt = go.AddComponent<Text>();
        txt.text      = "• " + text;
        txt.fontSize  = 22;
        txt.color     = new Color(0.8f, 0.9f, 0.8f);
        txt.alignment = TextAnchor.MiddleLeft;
        txt.font      = GetUIFont();
    }

    GameObject AddSpacer(Transform parent, float height)
    {
        var go = new GameObject("Spacer");
        go.transform.SetParent(parent, false);
        go.AddComponent<LayoutElement>().minHeight = height;
        return go;
    }

    // ── Helpers genéricos ──────────────────────────────────────

    static GameObject MakeRect(Transform parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        return go;
    }

    static GameObject MakeText(Transform parent, string name, string text, int size,
        Color color, FontStyle style)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        var le = go.AddComponent<LayoutElement>();
        le.minHeight = size + 14f;

        var txt = go.AddComponent<Text>();
        txt.text      = text;
        txt.fontSize  = size;
        txt.color     = color;
        txt.fontStyle = style;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.font      = GetUIFont();
        return go;
    }

    static void Stretch(GameObject go)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    static Font GetUIFont() =>
        Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
        ?? Font.CreateDynamicFontFromOSFont("Arial", 16);

    static void EnsureEventSystem()
    {
        if (FindAnyObjectByType<EventSystem>() != null) return;
        var go = new GameObject("EventSystem");
        go.AddComponent<EventSystem>();
        go.AddComponent<StandaloneInputModule>();
    }

    void GoBack()
    {
        SceneManager.LoadScene(GameSceneConfig.MenuScene);
    }

    void Update()
    {
        // Fallback manual para el botón volver con clic
        if (Input.GetMouseButtonDown(0))
        {
            var footer = GameObject.Find("Footer");
            if (footer != null)
            {
                var rt = footer.GetComponent<RectTransform>();
                if (rt != null && RectTransformUtility.RectangleContainsScreenPoint(
                        rt, Input.mousePosition, null))
                    GoBack();
            }
        }
    }
}
