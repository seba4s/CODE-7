using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Level1MissionDirector : MonoBehaviour
{
    public static Level1MissionDirector Instance { get; private set; }

    public const int RequiredData = 80;
    public const int RequiredTerminals = 3;

    public int CurrentData { get; private set; }
    public int ActivatedTerminals { get; private set; }
    public bool HiddenFolderFound { get; private set; }
    public bool ExitUnlocked => CurrentData >= RequiredData && ActivatedTerminals >= RequiredTerminals;

    Text missionText;
    bool levelCompleted;
    bool waitingForContinue;
    Text endTitleText;
    Text endBodyText;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void AutoInstall()
    {
        SceneManager.sceneLoaded += (scene, _) =>
        {
            if (!GameSceneConfig.IsGameplayScene(scene.name)) return;
            if (FindAnyObjectByType<Level1MissionDirector>() != null) return;

            var go = new GameObject("Level1MissionDirector");
            go.AddComponent<Level1MissionDirector>();
        };
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    void Start()
    {
        BuildMissionUI();
        RefreshMissionUI();
    }

    void Update()
    {
        if (!waitingForContinue) return;

        if (GameInput.GetInteractDown())
            ContinueToNextLevel();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public int AddData(int amount)
    {
        if (amount <= 0) return CurrentData;

        CurrentData = Mathf.Min(RequiredData, CurrentData + amount);
        RefreshMissionUI();
        return CurrentData;
    }

    public int ActivateTerminal()
    {
        ActivatedTerminals = Mathf.Min(RequiredTerminals, ActivatedTerminals + 1);
        RefreshMissionUI();
        return ActivatedTerminals;
    }

    public void MarkHiddenFolderFound()
    {
        HiddenFolderFound = true;
        RefreshMissionUI();
    }

    public string BuildObjectiveSummary()
    {
        string hidden = HiddenFolderFound ? "Encontrada" : "Pendiente";
        string exit = ExitUnlocked ? "DESBLOQUEADO" : "BLOQUEADO";
        return $"Datos {CurrentData}/{RequiredData}   Terminales {ActivatedTerminals}/{RequiredTerminals}   Carpeta oculta {hidden}   Puerto {exit}";
    }

    public void ShowLevelMessage(string title, string body)
    {
        if (NarrativeUI.Instance == null) return;

        var entry = ScriptableObject.CreateInstance<NarrativeEntrySO>();
        entry.id = $"lvl1_{title}_{Time.frameCount}";
        entry.type = NarrativeEntryType.Chat;
        entry.title = title;
        entry.body = body;
        NarrativeUI.Instance.Show(entry);
    }

    public void CompleteLevelAndOpenTransition()
    {
        if (levelCompleted) return;
        levelCompleted = true;
        waitingForContinue = true;

        BuildEndScreen();

        if (endTitleText != null)
            endTitleText.text = "NIVEL 1 COMPLETADO // PRIMER FORMATEO";

        if (endBodyText != null)
        {
            string ramTarget = Application.CanStreamedLevelBeLoaded("RamScene")
                ? "Pulsa [E] para ir al Nivel 2: Memoria RAM"
                : "Pulsa [E] para volver al Menu (RamScene aun no existe en Build Settings)";

            endBodyText.text = "Registro de Arranque restaurado.\nLa primera foto de Sebastian fue recuperada.\n" + ramTarget;
        }
    }

    void ContinueToNextLevel()
    {
        waitingForContinue = false;

        if (Application.CanStreamedLevelBeLoaded("RamScene"))
        {
            SceneManager.LoadScene("RamScene");
            return;
        }

        SceneManager.LoadScene(GameSceneConfig.MenuScene);
    }

    void BuildMissionUI()
    {
        if (GameObject.Find("Level1MissionHUD") != null) return;

        var canvasGo = new GameObject("Level1MissionHUD");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 995;

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        var panel = new GameObject("MissionPanel", typeof(RectTransform));
        panel.transform.SetParent(canvasGo.transform, false);
        var panelRt = panel.GetComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(1f, 1f);
        panelRt.anchorMax = new Vector2(1f, 1f);
        panelRt.pivot = new Vector2(1f, 1f);
        panelRt.anchoredPosition = new Vector2(-20f, -18f);
        panelRt.sizeDelta = new Vector2(760f, 78f);

        var bg = panel.AddComponent<Image>();
        bg.color = new Color(0.03f, 0.07f, 0.11f, 0.86f);

        var textGo = new GameObject("MissionText", typeof(RectTransform));
        textGo.transform.SetParent(panel.transform, false);
        var textRt = textGo.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = new Vector2(18f, 10f);
        textRt.offsetMax = new Vector2(-18f, -10f);

        missionText = textGo.AddComponent<Text>();
        missionText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
            ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
        missionText.fontSize = 22;
        missionText.alignment = TextAnchor.MiddleLeft;
        missionText.horizontalOverflow = HorizontalWrapMode.Wrap;
        missionText.verticalOverflow = VerticalWrapMode.Overflow;
        missionText.color = Color.white;
    }

    void BuildEndScreen()
    {
        if (GameObject.Find("Level1EndScreen") != null) return;

        var canvasGo = new GameObject("Level1EndScreen");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1300;

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        var shade = new GameObject("Shade", typeof(RectTransform));
        shade.transform.SetParent(canvasGo.transform, false);
        var shadeRt = shade.GetComponent<RectTransform>();
        shadeRt.anchorMin = Vector2.zero;
        shadeRt.anchorMax = Vector2.one;
        shadeRt.offsetMin = Vector2.zero;
        shadeRt.offsetMax = Vector2.zero;
        var shadeImg = shade.AddComponent<Image>();
        shadeImg.color = new Color(0f, 0f, 0f, 0.7f);

        var panel = new GameObject("Panel", typeof(RectTransform));
        panel.transform.SetParent(canvasGo.transform, false);
        var panelRt = panel.GetComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0.5f, 0.5f);
        panelRt.anchorMax = new Vector2(0.5f, 0.5f);
        panelRt.pivot = new Vector2(0.5f, 0.5f);
        panelRt.anchoredPosition = Vector2.zero;
        panelRt.sizeDelta = new Vector2(1020f, 360f);
        var panelImg = panel.AddComponent<Image>();
        panelImg.color = new Color(0.04f, 0.1f, 0.16f, 0.95f);

        var titleGo = new GameObject("Title", typeof(RectTransform));
        titleGo.transform.SetParent(panel.transform, false);
        var titleRt = titleGo.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0f, 1f);
        titleRt.anchorMax = new Vector2(1f, 1f);
        titleRt.pivot = new Vector2(0.5f, 1f);
        titleRt.offsetMin = new Vector2(28f, -120f);
        titleRt.offsetMax = new Vector2(-28f, -20f);

        endTitleText = titleGo.AddComponent<Text>();
        endTitleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
            ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
        endTitleText.fontSize = 42;
        endTitleText.alignment = TextAnchor.MiddleCenter;
        endTitleText.color = new Color(0.7f, 0.95f, 1f);

        var bodyGo = new GameObject("Body", typeof(RectTransform));
        bodyGo.transform.SetParent(panel.transform, false);
        var bodyRt = bodyGo.GetComponent<RectTransform>();
        bodyRt.anchorMin = new Vector2(0f, 0f);
        bodyRt.anchorMax = new Vector2(1f, 1f);
        bodyRt.offsetMin = new Vector2(40f, 24f);
        bodyRt.offsetMax = new Vector2(-40f, -130f);

        endBodyText = bodyGo.AddComponent<Text>();
        endBodyText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
            ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
        endBodyText.fontSize = 30;
        endBodyText.alignment = TextAnchor.MiddleCenter;
        endBodyText.color = Color.white;
    }

    void RefreshMissionUI()
    {
        if (missionText == null) return;
        missionText.text = BuildObjectiveSummary();
    }
}