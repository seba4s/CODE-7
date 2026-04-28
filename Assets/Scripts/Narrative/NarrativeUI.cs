using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class NarrativeUI : MonoBehaviour
{
    public static NarrativeUI Instance { get; private set; }
    public static bool IsGamePaused { get; private set; }  // Variable global de pausa
    public bool IsVisible => panel != null && panel.activeSelf;

    public GameObject panel;
    public TMP_Text titleText;
    public TMP_Text bodyText;
    public Image picture;
    public Button closeButton;

    // ── Auto-instalación ────────────────────────────────────
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoInstall()
    {
        if (FindAnyObjectByType<NarrativeUI>() != null) return;

        // Crear EventSystem si no existe (necesario para clicks de UI)
        if (FindAnyObjectByType<EventSystem>() == null)
        {
            var esGo = new GameObject("EventSystem");
            esGo.AddComponent<EventSystem>();
            Debug.Log("[NarrativeUI] EventSystem creado.");
        }

        var canvasGo = new GameObject("NarrativeUICanvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        canvasGo.AddComponent<CanvasScaler>();
        canvasGo.AddComponent<GraphicRaycaster>();

        var uiGo = new GameObject("NarrativeUIPanel");
        uiGo.transform.SetParent(canvasGo.transform, false);

        var ui = uiGo.AddComponent<NarrativeUI>();
        ui.CreateDefaultUI();
        
        Debug.Log("[NarrativeUI] Auto-instalado en la escena.");
    }

    void Awake()
    {
        if (Instance == null) Instance = this;

        EnsureReferences();
        ConfigureReadableLayout();

        if (panel != null)
            Hide();
    }

    // Fallback: detectar click manualmente sobre el botón sin depender de EventSystem
    void Update()
    {
        if (!IsVisible) return;

        if (Input.GetMouseButtonDown(0) && closeButton != null)
        {
            var rt = closeButton.GetComponent<RectTransform>();
            if (rt != null && RectTransformUtility.RectangleContainsScreenPoint(rt, Input.mousePosition, null))
            {
                Debug.Log("[NarrativeUI] Click detectado sobre botón Cerrar (manual).");
                Hide();
            }
        }
    }

    void OnDestroy()
    {
        if (closeButton != null)
            closeButton.onClick.RemoveListener(Hide);

        if (Instance == this) Instance = null;
    }

    public void Show(NarrativeEntrySO entry)
    {
        if (entry == null)
        {
            Debug.LogWarning("NarrativeUI.Show: entry es null.");
            return;
        }

        if (panel == null || titleText == null || bodyText == null)
        {
            Debug.LogError("NarrativeUI: faltan referencias de UI en el Inspector.");
            return;
        }

        ConfigureReadableLayout();

        panel.SetActive(true);
        titleText.text = entry.title;
        bodyText.text = entry.body;

        if (picture != null)
        {
            bool hasPic = entry.picture != null;
            picture.gameObject.SetActive(hasPic);
            if (hasPic) picture.sprite = entry.picture;
        }

        // Parar al jugador
        var playerRb = GetPlayerRigidbody();
        if (playerRb != null) playerRb.linearVelocity = Vector2.zero;

        // Mostrar cursor para poder hacer click en el botón
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        IsGamePaused = true;
    }

    public void Hide()
    {
        if (panel == null) return;
        panel.SetActive(false);
        IsGamePaused = false;
    }

    Rigidbody2D GetPlayerRigidbody()
    {
        var pc = FindAnyObjectByType<PlayerController2D>();
        return pc != null ? pc.GetComponent<Rigidbody2D>() : null;
    }

    void EnsureReferences()
    {
        if (panel == null)
            panel = gameObject;

        if (titleText == null)
            titleText = panel.GetComponentInChildren<TMP_Text>(true);

        if (bodyText == null)
        {
            var texts = panel.GetComponentsInChildren<TMP_Text>(true);
            if (texts.Length > 1) bodyText = texts[1];
        }

        if (closeButton == null)
            closeButton = panel.GetComponentInChildren<Button>(true);
    }

    void CreateDefaultUI()
    {
        panel = gameObject;
        var panelRt = panel.AddComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0.5f, 0.5f);
        panelRt.anchorMax = new Vector2(0.5f, 0.5f);
        panelRt.pivot = new Vector2(0.5f, 0.5f);
        panelRt.anchoredPosition = Vector2.zero;
        panelRt.sizeDelta = new Vector2(780f, 420f);

        var bg = panel.AddComponent<Image>();
        bg.color = new Color(0.1f, 0.1f, 0.1f, 0.85f);

        // Título
        var titleGo = new GameObject("Title", typeof(RectTransform));
        titleGo.transform.SetParent(panel.transform, false);
        titleText = titleGo.AddComponent<TextMeshProUGUI>();
        titleText.text = "Título";
        titleText.fontSize = 34;
        titleText.color = Color.white;
        titleText.alignment = TextAlignmentOptions.TopLeft;
        var titleRt = titleGo.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0f, 1f);
        titleRt.anchorMax = new Vector2(1f, 1f);
        titleRt.pivot = new Vector2(0.5f, 1f);
        titleRt.offsetMin = new Vector2(24f, -88f);
        titleRt.offsetMax = new Vector2(-120f, -24f);

        // Body
        var bodyGo = new GameObject("Body", typeof(RectTransform));
        bodyGo.transform.SetParent(panel.transform, false);
        bodyText = bodyGo.AddComponent<TextMeshProUGUI>();
        bodyText.text = "Mensaje";
        bodyText.fontSize = 28;
        bodyText.color = Color.white;
        bodyText.alignment = TextAlignmentOptions.TopLeft;
        bodyText.overflowMode = TextOverflowModes.Overflow;
        var bodyRt = bodyGo.GetComponent<RectTransform>();
        bodyRt.anchorMin = new Vector2(0f, 0f);
        bodyRt.anchorMax = new Vector2(1f, 1f);
        bodyRt.pivot = new Vector2(0.5f, 0.5f);
        bodyRt.offsetMin = new Vector2(24f, 24f);
        bodyRt.offsetMax = new Vector2(-24f, -96f);

        // Botón Cerrar
        var buttonGo = new GameObject("CloseButton", typeof(RectTransform));
        buttonGo.transform.SetParent(panel.transform, false);
        closeButton = buttonGo.AddComponent<Button>();
        closeButton.interactable = true;
        closeButton.transition = Selectable.Transition.ColorTint;
        
        var buttonRt = buttonGo.GetComponent<RectTransform>();
        buttonRt.anchorMin = new Vector2(1f, 1f);
        buttonRt.anchorMax = new Vector2(1f, 1f);
        buttonRt.pivot = new Vector2(1f, 1f);
        buttonRt.anchoredPosition = new Vector2(-24f, -24f);
        buttonRt.sizeDelta = new Vector2(140f, 48f);

        var buttonImage = buttonGo.AddComponent<Image>();
        buttonImage.color = new Color(0.3f, 0.3f, 0.3f, 0.8f);
        buttonImage.raycastTarget = true;  // ¡IMPORTANTE! Sin esto el click no llega al botón

        var buttonText = new GameObject("Text", typeof(RectTransform));
        buttonText.transform.SetParent(buttonGo.transform, false);
        var btnTxt = buttonText.AddComponent<TextMeshProUGUI>();
        btnTxt.text = "Cerrar";
        btnTxt.fontSize = 20;
        btnTxt.color = Color.white;
        btnTxt.alignment = TextAlignmentOptions.Center;
        var txtRt = buttonText.GetComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.offsetMin = Vector2.zero;
        txtRt.offsetMax = Vector2.zero;

        // Conectar listener al botón inmediatamente
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(Hide);
            Debug.Log("[NarrativeUI] Botón Cerrar conectado en CreateDefaultUI().");
        }
    }

    void ConfigureReadableLayout()
    {
        if (panel == null) return;

        var panelRt = panel.GetComponent<RectTransform>();
        if (panelRt != null)
        {
            panelRt.anchorMin = new Vector2(0.5f, 0.5f);
            panelRt.anchorMax = new Vector2(0.5f, 0.5f);
            panelRt.pivot = new Vector2(0.5f, 0.5f);
            panelRt.anchoredPosition = Vector2.zero;
            panelRt.sizeDelta = new Vector2(780f, 420f);
        }

        if (titleText != null)
        {
            titleText.textWrappingMode = TextWrappingModes.Normal;
            titleText.fontSize = 34f;
            titleText.alignment = TextAlignmentOptions.TopLeft;

            var rt = titleText.rectTransform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.offsetMin = new Vector2(24f, -88f);
            rt.offsetMax = new Vector2(-120f, -24f);
        }

        if (bodyText != null)
        {
            bodyText.textWrappingMode = TextWrappingModes.Normal;
            bodyText.overflowMode = TextOverflowModes.Overflow;
            bodyText.fontSize = 28f;
            bodyText.alignment = TextAlignmentOptions.TopLeft;

            var rt = bodyText.rectTransform;
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(24f, 24f);
            rt.offsetMax = new Vector2(-24f, -96f);
        }

        if (closeButton != null)
        {
            var buttonRt = closeButton.GetComponent<RectTransform>();
            if (buttonRt != null)
            {
                buttonRt.anchorMin = new Vector2(1f, 1f);
                buttonRt.anchorMax = new Vector2(1f, 1f);
                buttonRt.pivot = new Vector2(1f, 1f);
                buttonRt.anchoredPosition = new Vector2(-24f, -24f);
                buttonRt.sizeDelta = new Vector2(140f, 48f);
            }

            var tmp = closeButton.GetComponentInChildren<TMP_Text>(true);
            if (tmp != null)
            {
                tmp.text = "Cerrar";
                tmp.fontSize = 24f;
                tmp.alignment = TextAlignmentOptions.Center;
            }

            var legacy = closeButton.GetComponentInChildren<Text>(true);
            if (legacy != null)
                legacy.text = "Cerrar";
        }
    }
}