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
    Button newGameButton;
    Button continueButton;
    Button upgradesButton;
    Button optionsButton;
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

        TryInvokeManual(newGameButton, OnNewGameClicked);
        TryInvokeManual(continueButton, OnContinueClicked);
        TryInvokeManual(upgradesButton, OnUpgradesClicked);
        TryInvokeManual(optionsButton, OnOptionsClicked);
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
        layout.spacing = 16f;
        layout.padding = new RectOffset(40, 40, 40, 40);
        layout.childControlWidth = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        // ── Botones ──────────────────────────────────────────────
        newGameButton = CreateImageMenuButton(panel.transform, "NuevaPartida", "nueva partida", OnNewGameClicked);
        continueButton = CreateImageMenuButton(panel.transform, "Continuar", "continuar", OnContinueClicked);
        upgradesButton = CreateImageMenuButton(panel.transform, "Mejoras", "mejoras", OnUpgradesClicked);
        optionsButton = CreateImageMenuButton(panel.transform, "Opciones", "opciones", OnOptionsClicked);
        quitButton = CreateImageMenuButton(panel.transform, "Salir", "salir", OnQuitClicked);
    }

    // ── Callbacks ────────────────────────────────────────────────

    void OnNewGameClicked()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(GameSceneConfig.CanLoadTutorialScene()
            ? GameSceneConfig.TutorialScene
            : GameSceneConfig.GameplayScene);
    }

    void OnContinueClicked()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(GameSceneConfig.GameplayScene);
    }

    void OnUpgradesClicked()
    {
        // Placeholder until an upgrades scene/system is added.
        Debug.Log("Mejoras: aun no implementado.");
    }

    void OnOptionsClicked()
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

    Button CreateImageMenuButton(Transform parent, string objectName,
        string resourceSpriteName, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject("Btn_" + objectName);
        go.transform.SetParent(parent, false);

        var le = go.AddComponent<LayoutElement>();
        le.preferredWidth = 620f;
        le.preferredHeight = 110f;
        le.minHeight = 90f;

        var img = go.AddComponent<Image>();
        img.color = Color.white;
        img.sprite = LoadMenuSprite(resourceSpriteName);
        img.preserveAspect = true;

        if (img.sprite != null)
        {
            float aspect = img.sprite.rect.width / img.sprite.rect.height;
            le.preferredHeight = le.preferredWidth / Mathf.Max(aspect, 0.01f);
            le.minHeight = le.preferredHeight;
        }
        else
        {
            img.color = new Color(0.15f, 0.15f, 0.2f, 1f);
        }

        var btn = go.AddComponent<Button>();

        var colors = btn.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.92f, 0.92f, 0.92f, 1f);
        colors.pressedColor = new Color(0.78f, 0.78f, 0.78f, 1f);
        colors.selectedColor = colors.normalColor;
        btn.colors = colors;

        btn.onClick.AddListener(onClick);
        return btn;
    }

    Sprite LoadMenuSprite(string spriteName)
    {
        string resourcePath = "MenuButtons/" + spriteName;

        var sprite = Resources.Load<Sprite>(resourcePath);
        if (sprite != null) return sprite;

        var tex = Resources.Load<Texture2D>(resourcePath);
        if (tex == null) return null;

        return Sprite.Create(
            tex,
            new Rect(0f, 0f, tex.width, tex.height),
            new Vector2(0.5f, 0.5f),
            100f);
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
