using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class NarrativeUI : MonoBehaviour
{
    public static NarrativeUI Instance { get; private set; }
    public bool IsVisible => panel != null && panel.activeSelf;

    // Segundos que el mensaje permanece visible antes de ocultarse
    public float displayDuration = 5f;

    public GameObject panel;
    public TMP_Text titleText;
    public TMP_Text bodyText;
    public Image picture;

    Transform player;
    RectTransform panelRt;
    Canvas rootCanvas;
    public float worldYOffset = 2.3f;
    public float screenYOffset = 56f;

    // ── Auto-instalacion ──────────────────────────────────────
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void AutoInstall()
    {
        SceneManager.sceneLoaded += (scene, _) =>
        {
            if (!GameSceneConfig.IsGameplayScene(scene.name)) return;
            if (FindAnyObjectByType<NarrativeUI>() != null) return;

            if (FindAnyObjectByType<EventSystem>() == null)
            {
                var esGo = new GameObject("EventSystem");
                esGo.AddComponent<EventSystem>();
            }

            var canvasGo = new GameObject("NarrativeUICanvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            var uiGo = new GameObject("NarrativeUIPanel");
            uiGo.transform.SetParent(canvasGo.transform, false);

            var ui = uiGo.AddComponent<NarrativeUI>();
            ui.CreateDefaultUI();
        };
    }

    void Awake()
    {
        if (Instance == null) Instance = this;
        rootCanvas = GetComponentInParent<Canvas>();
        EnsureReferences();
        ConfigureLayout();
        FindPlayerTransform();
        if (panel != null) Hide();
    }

    void LateUpdate()
    {
        if (!IsVisible) return;
        if (player == null) FindPlayerTransform();
        UpdatePanelPositionAbovePlayer();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ── API publica ───────────────────────────────────────────

    public void Show(NarrativeEntrySO entry)
    {
        if (entry == null) { Debug.LogWarning("NarrativeUI.Show: entry es null."); return; }
        if (panel == null || titleText == null || bodyText == null)
        {
            Debug.LogError("NarrativeUI: faltan referencias de UI.");
            return;
        }

        panel.SetActive(true);
        titleText.text = entry.title;
        bodyText.text  = entry.body;

        if (picture != null)
        {
            bool hasPic = entry.picture != null;
            picture.gameObject.SetActive(hasPic);
            if (hasPic) picture.sprite = entry.picture;
        }

        // Auto-ocultar despues de displayDuration segundos
        StopAllCoroutines();
        StartCoroutine(AutoHide());
    }

    public void Hide()
    {
        StopAllCoroutines();
        if (panel != null) panel.SetActive(false);
    }

    // ── Privado ───────────────────────────────────────────────

    IEnumerator AutoHide()
    {
        yield return new WaitForSeconds(displayDuration);
        Hide();
    }

    void EnsureReferences()
    {
        if (panel == null) panel = gameObject;
        if (panelRt == null) panelRt = panel.GetComponent<RectTransform>();
        if (titleText == null) titleText = panel.GetComponentInChildren<TMP_Text>(true);
        if (bodyText == null)
        {
            var texts = panel.GetComponentsInChildren<TMP_Text>(true);
            if (texts.Length > 1) bodyText = texts[1];
        }
    }

    void CreateDefaultUI()
    {
        panel = gameObject;
        panelRt = panel.AddComponent<RectTransform>();
        // El panel se reposiciona dinamicamente sobre el jugador
        panelRt.anchorMin        = new Vector2(0f, 0f);
        panelRt.anchorMax        = new Vector2(0f, 0f);
        panelRt.pivot            = new Vector2(0.5f, 0f);
        panelRt.anchoredPosition = new Vector2(960f, 220f);
        panelRt.sizeDelta        = new Vector2(860f, 190f);

        var bg = panel.AddComponent<Image>();
        bg.color = new Color(0.05f, 0.06f, 0.14f, 0.88f);

        // Linea de acento lateral izquierda
        var accent = new GameObject("Accent", typeof(RectTransform));
        accent.transform.SetParent(panel.transform, false);
        var aImg = accent.AddComponent<Image>();
        aImg.color = new Color(0.35f, 0.65f, 1f, 0.9f);
        var aRt = accent.GetComponent<RectTransform>();
        aRt.anchorMin        = new Vector2(0f, 0f);
        aRt.anchorMax        = new Vector2(0f, 1f);
        aRt.pivot            = new Vector2(0f, 0.5f);
        aRt.anchoredPosition = Vector2.zero;
        aRt.sizeDelta        = new Vector2(4f, 0f);

        // Titulo
        var titleGo = new GameObject("Title", typeof(RectTransform));
        titleGo.transform.SetParent(panel.transform, false);
        titleText = titleGo.AddComponent<TextMeshProUGUI>();
        titleText.text      = "Titulo";
        titleText.fontSize  = 28;
        titleText.color     = new Color(0.6f, 0.85f, 1f);
        titleText.fontStyle = FontStyles.Bold;
        titleText.alignment = TextAlignmentOptions.TopLeft;
        var titleRt = titleGo.GetComponent<RectTransform>();
        titleRt.anchorMin        = new Vector2(0f, 1f);
        titleRt.anchorMax        = new Vector2(1f, 1f);
        titleRt.pivot            = new Vector2(0.5f, 1f);
        titleRt.anchoredPosition = Vector2.zero;
        titleRt.offsetMin        = new Vector2(18f, -46f);
        titleRt.offsetMax        = new Vector2(-12f, -10f);

        // Cuerpo
        var bodyGo = new GameObject("Body", typeof(RectTransform));
        bodyGo.transform.SetParent(panel.transform, false);
        bodyText = bodyGo.AddComponent<TextMeshProUGUI>();
        bodyText.text          = "Mensaje";
        bodyText.fontSize      = 24;
        bodyText.color         = new Color(0.85f, 0.85f, 0.85f);
        bodyText.alignment     = TextAlignmentOptions.TopLeft;
        bodyText.overflowMode  = TextOverflowModes.Truncate;
        bodyText.textWrappingMode = TextWrappingModes.Normal;
        var bodyRt = bodyGo.GetComponent<RectTransform>();
        bodyRt.anchorMin        = new Vector2(0f, 0f);
        bodyRt.anchorMax        = new Vector2(1f, 1f);
        bodyRt.pivot            = new Vector2(0.5f, 0.5f);
        bodyRt.offsetMin        = new Vector2(18f, 10f);
        bodyRt.offsetMax        = new Vector2(-12f, -50f);
    }

    void ConfigureLayout()
    {
        if (panel == null) return;

        if (panelRt == null) panelRt = panel.GetComponent<RectTransform>();
        if (panelRt != null)
        {
            panelRt.anchorMin        = new Vector2(0f, 0f);
            panelRt.anchorMax        = new Vector2(0f, 0f);
            panelRt.pivot            = new Vector2(0.5f, 0f);
            panelRt.anchoredPosition = new Vector2(Screen.width * 0.5f, 220f);
            panelRt.sizeDelta        = new Vector2(860f, 190f);
        }
    }

    void FindPlayerTransform()
    {
        var playerObj = GameObject.FindWithTag("Player");
        if (playerObj == null)
        {
            var pc = FindAnyObjectByType<PlayerController2D>();
            if (pc != null) playerObj = pc.gameObject;
        }

        if (playerObj != null)
            player = playerObj.transform;
    }

    void UpdatePanelPositionAbovePlayer()
    {
        if (panelRt == null || player == null) return;

        var cam = Camera.main;
        if (cam == null) return;

        Vector3 worldPos = player.position + Vector3.up * worldYOffset;
        Vector3 screenPos = cam.WorldToScreenPoint(worldPos);

        if (screenPos.z <= 0f) return;

        Vector2 target = new Vector2(screenPos.x, screenPos.y + screenYOffset);
        Vector2 size = panelRt.sizeDelta;

        float clampedX = Mathf.Clamp(target.x, size.x * 0.5f + 12f, Screen.width - size.x * 0.5f - 12f);
        float clampedY = Mathf.Clamp(target.y, 12f, Screen.height - size.y - 12f);

        panelRt.anchoredPosition = new Vector2(clampedX, clampedY);
    }
}
