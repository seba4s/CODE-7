using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Construye el menú principal completamente por código.
/// Añade este componente a un GameObject vacío en la escena "MainMenu".
/// </summary>
public class MainMenuRuntime : MonoBehaviour
{
    Button playButton;
    Button controlsButton;
    Button quitButton;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void AutoInstall()
    {
        SceneManager.sceneLoaded += (scene, _) =>
        {
            if (!GameSceneConfig.IsMenuScene(scene.name)) return;
            if (FindAnyObjectByType<MainMenuRuntime>() != null) return;
            var go = new GameObject("MainMenuRuntime");
            go.AddComponent<MainMenuRuntime>();
        };
    }

    void Start()
    {
        Time.timeScale = 0f;
        BuildMenu();
    }

    void Update()
    {
        if (!Input.GetMouseButtonDown(0)) return;

        TryInvokeManual(playButton, OnPlayClicked);
        TryInvokeManual(controlsButton, OnControlsClicked);
        TryInvokeManual(quitButton, OnQuitClicked);
    }

    void BuildMenu()
    {
        EnsureEventSystem();

        // ── Canvas ──────────────────────────────────────────────
        var canvasGO = new GameObject("MainMenuCanvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        canvasGO.AddComponent<CanvasScaler>().uiScaleMode =
            CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasGO.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
        canvasGO.AddComponent<GraphicRaycaster>();

        // ── Fondo ────────────────────────────────────────────────
        var bg = CreatePanel(canvasGO.transform, "Background",
            new Color(0.05f, 0.05f, 0.1f, 1f));
        Stretch(bg);

        // ── Panel central ────────────────────────────────────────
        var panel = CreatePanel(bg.transform, "Panel", new Color(0f, 0f, 0f, 0.6f));
        var panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.35f, 0.2f);
        panelRect.anchorMax = new Vector2(0.65f, 0.85f);
        panelRect.offsetMin = panelRect.offsetMax = Vector2.zero;

        // Layout vertical automático
        var layout = panel.AddComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.spacing = 20f;
        layout.padding = new RectOffset(40, 40, 40, 40);
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        // ── Título ───────────────────────────────────────────────
        var title = CreateText(panel.transform, "Title", "CODE-7",
            72, Color.white, FontStyle.Bold);
        title.GetComponent<LayoutElement>().minHeight = 120f;

        // ── Subtítulo ────────────────────────────────────────────
        var subtitle = CreateText(panel.transform, "Subtitle",
            "Usa tus habilidades para sobrevivir", 26,
            new Color(0.7f, 0.7f, 0.9f), FontStyle.Italic);
        subtitle.GetComponent<LayoutElement>().minHeight = 50f;

        // ── Espaciador ───────────────────────────────────────────
        var spacer = new GameObject("Spacer");
        spacer.transform.SetParent(panel.transform, false);
        spacer.AddComponent<LayoutElement>().minHeight = 30f;

        // ── Botones ──────────────────────────────────────────────
        playButton = CreateMenuButton(panel.transform, "Jugar",
            new Color(0.2f, 0.6f, 0.2f), OnPlayClicked);

        controlsButton = CreateMenuButton(panel.transform, "Controles",
            new Color(0.2f, 0.4f, 0.7f), OnControlsClicked);

        quitButton = CreateMenuButton(panel.transform, "Salir",
            new Color(0.6f, 0.1f, 0.1f), OnQuitClicked);
    }

    // ── Callbacks ────────────────────────────────────────────────

    void OnPlayClicked()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(GameSceneConfig.CanLoadTutorialScene()
            ? GameSceneConfig.TutorialScene
            : GameSceneConfig.GameplayScene);
    }

    void OnControlsClicked()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(GameSceneConfig.ControlsScene);
    }

    void OnQuitClicked()
    {
        Time.timeScale = 1f;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ── Helpers de UI ────────────────────────────────────────────

    GameObject CreatePanel(Transform parent, string name, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = color;
        return go;
    }

    void Stretch(GameObject go)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    GameObject CreateText(Transform parent, string name, string content,
        int size, Color color, FontStyle style = FontStyle.Normal)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var txt = go.AddComponent<Text>();
        txt.text = content;
        txt.fontSize = size;
        txt.color = color;
        txt.fontStyle = style;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.font = GetUIFont();
        go.AddComponent<LayoutElement>();
        return go;
    }

    Button CreateMenuButton(Transform parent, string label,
        Color bgColor, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject("Btn_" + label);
        go.transform.SetParent(parent, false);

        var le = go.AddComponent<LayoutElement>();
        le.minHeight = 70f;

        var img = go.AddComponent<Image>();
        img.color = bgColor;

        var btn = go.AddComponent<Button>();

        // Estado hover: aclara el color
        var colors = btn.colors;
        colors.highlightedColor = new Color(
            Mathf.Min(bgColor.r + 0.25f, 1f),
            Mathf.Min(bgColor.g + 0.25f, 1f),
            Mathf.Min(bgColor.b + 0.25f, 1f));
        colors.pressedColor = new Color(
            bgColor.r * 0.7f, bgColor.g * 0.7f, bgColor.b * 0.7f);
        btn.colors = colors;

        btn.onClick.AddListener(onClick);

        // Texto del botón
        var txtGO = new GameObject("Label");
        txtGO.transform.SetParent(go.transform, false);
        var txtRect = txtGO.AddComponent<RectTransform>();
        txtRect.anchorMin = Vector2.zero;
        txtRect.anchorMax = Vector2.one;
        txtRect.offsetMin = txtRect.offsetMax = Vector2.zero;
        var txt = txtGO.AddComponent<Text>();
        txt.text = label;
        txt.fontSize = 36;
        txt.color = Color.white;
        txt.fontStyle = FontStyle.Bold;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.font = GetUIFont();
        return btn;
    }

    static Font GetUIFont()
    {
        var f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (f == null) f = Resources.GetBuiltinResource<Font>("Arial.ttf");
        return f;
    }

    static void EnsureEventSystem()
    {
        if (FindAnyObjectByType<EventSystem>() != null) return;

        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<StandaloneInputModule>();
    }

    void TryInvokeManual(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null) return;
        var rect = button.GetComponent<RectTransform>();
        if (rect == null) return;
        if (!RectTransformUtility.RectangleContainsScreenPoint(rect, Input.mousePosition, null)) return;
        action?.Invoke();
    }
}
