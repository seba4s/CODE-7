using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
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
        if (!GameInput.GetMouseButtonDown(0)) return;

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

        var bgImage = bg.GetComponent<Image>();
        var backgroundSprite = LoadSpriteFromResources("UI/MainMenu/fondo juego");
        if (backgroundSprite != null)
        {
            bgImage.sprite = backgroundSprite;
            bgImage.color = Color.white;
            bgImage.type = Image.Type.Simple;
            bgImage.preserveAspect = false;
        }

        var logoSprite = LoadSpriteFromResources("UI/MainMenu/menu codex recovered");
        if (logoSprite != null)
        {
            var logo = CreateImage(bg.transform, "GameLogo", logoSprite, true);
            var logoRect = logo.GetComponent<RectTransform>();
            logoRect.anchorMin = logoRect.anchorMax = new Vector2(0.5f, 0.86f);
            logoRect.anchoredPosition = new Vector2(0f, 10f);
            logoRect.sizeDelta = GetFittedSize(logoSprite, 980f, 280f);
        }

        // ── Grupo de botones ─────────────────────────────────────
        var panel = new GameObject("ButtonsGroup");
        panel.transform.SetParent(bg.transform, false);
        var panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.37f, 0.24f);
        panelRect.anchorMax = new Vector2(0.63f, 0.56f);
        panelRect.offsetMin = panelRect.offsetMax = Vector2.zero;

        // Layout vertical automático
        var layout = panel.AddComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.spacing = 22f;
        layout.padding = new RectOffset(24, 24, 12, 12);
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

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

    GameObject CreateImage(Transform parent, string name, Sprite sprite, bool preserveAspect)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.sprite = sprite;
        img.preserveAspect = preserveAspect;
        img.color = Color.white;
        return go;
    }

    Button CreateMenuButton(Transform parent, string label,
        Color bgColor, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject("Btn_" + label);
        go.transform.SetParent(parent, false);

        var le = go.AddComponent<LayoutElement>();
        le.minHeight = 82f;

        var img = go.AddComponent<Image>();
        UIRuntimeStyle.ApplyRoundedButtonStyle(img, bgColor);

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

    static Sprite LoadSpriteFromResources(string resourcePath)
    {
        var texture = Resources.Load<Texture2D>(resourcePath);
        if (texture != null)
        {
            return Sprite.Create(
                texture,
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f);
        }

        var sprite = Resources.Load<Sprite>(resourcePath);
        if (sprite != null) return sprite;

        return null;
    }

    static Vector2 GetFittedSize(Sprite sprite, float maxWidth, float maxHeight)
    {
        if (sprite == null || sprite.rect.height <= 0f)
            return new Vector2(maxWidth, maxHeight);

        float aspect = sprite.rect.width / sprite.rect.height;
        float width = maxWidth;
        float height = width / aspect;

        if (height > maxHeight)
        {
            height = maxHeight;
            width = height * aspect;
        }

        return new Vector2(width, height);
    }

    static void EnsureEventSystem()
    {
        if (FindAnyObjectByType<EventSystem>() != null) return;

        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<InputSystemUIInputModule>();
    }

    void TryInvokeManual(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null) return;
        var rect = button.GetComponent<RectTransform>();
        if (rect == null) return;
        if (!RectTransformUtility.RectangleContainsScreenPoint(rect, GameInput.GetPointerPosition(), null)) return;
        action?.Invoke();
    }
}
