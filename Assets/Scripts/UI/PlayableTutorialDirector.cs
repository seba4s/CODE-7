using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Tutorial jugable paso a paso.
/// El jugador debe ejecutar cada acción para avanzar al siguiente paso.
/// Se auto-instala en la escena de juego cuando MainMenuRuntime lo solicita.
/// También aparece automáticamente la primera vez que se juega.
/// </summary>
public class PlayableTutorialDirector : MonoBehaviour
{
    // ── Auto-instalación ──────────────────────────────────────────
    const string PREFS_KEY = "Tutorial_Shown_v1";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void AutoInstall()
    {
        SceneManager.sceneLoaded += (scene, _) =>
        {
            if (!GameSceneConfig.IsTutorialScene(scene.name)) return;
            if (FindAnyObjectByType<PlayableTutorialDirector>() != null) return;
            new GameObject("PlayableTutorialDirector").AddComponent<PlayableTutorialDirector>();
        };
    }

    // ── Datos de cada paso ────────────────────────────────────────
    struct TutStep
    {
        public string title;
        public string description;
        public string keyHint;
        public bool   requiresHold;
        public float  holdDuration;
    }

    // ── Estado interno ────────────────────────────────────────────
    TutStep[] _steps;
    int       _current = -1;
    bool      _isRunning;
    bool      _singleFired;
    bool      _holdActive;
    float     _holdFill;

    // ── Referencias al jugador ────────────────────────────────────
    PlayerHealth          _health;
    PlayerDash            _dash;
    EnemyRuntimeSpawner[] _spawners;

    // ── Referencias UI ────────────────────────────────────────────
    Canvas _canvas;
    Text   _counterText;
    Text   _titleText;
    Text   _descText;
    Text   _hintText;
    Image  _progressFill;
    Text   _feedbackText;

    // ── Ciclo de vida ─────────────────────────────────────────────

    void Start()
    {
        GrabPlayerRefs();
        DisableEnemies();
        if (_health != null) _health.SetInvulnerable(true);

        BuildUI();

        _steps = new TutStep[]
        {
            new TutStep
            {
                title        = "MOVERSE",
                description  = "Usa  A / D  o las flechas  ← →  para caminar.",
                keyHint      = "[ A ]     ←  →     [ D ]",
                requiresHold = true,
                holdDuration = 1.5f
            },
            new TutStep
            {
                title        = "SALTAR",
                description  = "Presiona la barra espaciadora para saltar.\nPresiona dos veces para doble salto.",
                keyHint      = "[ BARRA ESPACIADORA ]",
                requiresHold = false,
            },
            new TutStep
            {
                title        = "APUNTAR",
                description  = "Mueve el mouse para apuntar hacia cualquier dirección.\nEl personaje siempre apunta al cursor.",
                keyHint      = "Mueve el Mouse",
                requiresHold = true,
                holdDuration = 1.2f
            },
            new TutStep
            {
                title        = "DISPARAR",
                description  = "Haz clic izquierdo para disparar un rayo hacia donde apunta el cursor.",
                keyHint      = "[ CLIC IZQUIERDO ]",
                requiresHold = false,
            },
            new TutStep
            {
                title        = "DASH",
                description  = "Mantén una dirección presionada y luego pulsa Shift.\nConsume estamina y te hace invulnerable un instante.",
                keyHint      = "[ ← / → ]  +  [ SHIFT ]",
                requiresHold = false,
            },
        };

        _isRunning = true;
        StartCoroutine(TutorialLoop());
    }

    // ── Detección de input (Update) ───────────────────────────────

    void Update()
    {
        if (!_isRunning || _current < 0 || _current >= _steps.Length) return;

        switch (_current)
        {
            case 0: // Moverse — mantener tecla
                _holdActive = Mathf.Abs(Input.GetAxisRaw("Horizontal")) > 0.1f;
                break;

            case 1: // Saltar — pulsación única
                if (Input.GetButtonDown("Jump")) _singleFired = true;
                break;

            case 2: // Apuntar — mover mouse de forma sostenida
                _holdActive = Mathf.Abs(Input.GetAxis("Mouse X")) > 0.15f
                           || Mathf.Abs(Input.GetAxis("Mouse Y")) > 0.15f;
                break;

            case 3: // Disparar — clic izquierdo
                if (Input.GetMouseButtonDown(0)) _singleFired = true;
                break;

            case 4: // Dash — detectar que el dash está activo
                if (_dash != null && _dash.IsDashing) _singleFired = true;
                break;
        }

        // Saltar tutorial con ESC
        if (Input.GetKeyDown(KeyCode.Escape))
            SkipTutorial();
    }

    // ── Bucle principal ───────────────────────────────────────────

    IEnumerator TutorialLoop()
    {
        yield return null; // espera un frame para que la escena termine de cargar

        for (_current = 0; _current < _steps.Length; _current++)
        {
            _singleFired = false;
            _holdActive  = false;
            _holdFill    = 0f;

            ShowStep(_steps[_current], _current + 1, _steps.Length);

            if (_steps[_current].requiresHold)
                yield return RunHoldStep(_steps[_current].holdDuration);
            else
                yield return RunSingleStep();

            yield return ShowFeedback("¡ Bien hecho !  ✓", new Color(0.3f, 1f, 0.45f), 0.9f);
        }

        CompleteTutorial();
        yield return ShowEndScreen();
        Destroy(gameObject);
    }

    // -- Paso que requiere mantener presionado (barra llena = completo)
    IEnumerator RunHoldStep(float totalTime)
    {
        while (_holdFill < 1f)
        {
            float delta = _holdActive
                ? Time.deltaTime / totalTime
                : -Time.deltaTime * 2f / totalTime;

            _holdFill = Mathf.Clamp01(_holdFill + delta);
            SetProgressBar(_holdFill);
            yield return null;
        }
        SetProgressBar(1f);
    }

    // -- Paso que requiere una sola pulsación (el hint parpadea)
    IEnumerator RunSingleStep()
    {
        SetProgressBar(0f);
        float pulse = 0f;
        while (!_singleFired)
        {
            pulse += Time.deltaTime * Mathf.PI * 2.2f;
            if (_hintText != null)
            {
                float t = (Mathf.Sin(pulse) + 1f) * 0.5f;
                _hintText.color = Color.Lerp(new Color(1f, 0.85f, 0.2f), Color.white, t);
            }
            yield return null;
        }
        if (_hintText != null) _hintText.color = new Color(1f, 0.85f, 0.2f);
        SetProgressBar(1f);
    }

    IEnumerator ShowFeedback(string msg, Color color, float seconds)
    {
        if (_feedbackText != null) { _feedbackText.text = msg; _feedbackText.color = color; }
        yield return new WaitForSeconds(seconds);
        if (_feedbackText != null) _feedbackText.text = "";
    }

    IEnumerator ShowEndScreen()
    {
        ShowStep(new TutStep
        {
            title       = "¡ TUTORIAL COMPLETADO !",
            description = "¡Ya dominas los controles básicos!\nEl juego comienza ahora.  ¡Buena suerte!",
            keyHint     = ""
        }, 0, 0);
        SetProgressBar(1f);
        if (_progressFill != null) _progressFill.color = new Color(0.3f, 0.6f, 1f);
        yield return new WaitForSeconds(3f);
        if (_canvas != null) Destroy(_canvas.gameObject);
        SceneManager.LoadScene(GameSceneConfig.GameplayScene);
    }

    // ── Completar / saltar ────────────────────────────────────────

    void CompleteTutorial()
    {
        PlayerPrefs.SetInt(PREFS_KEY, 1);
        PlayerPrefs.Save();
        _isRunning   = false;
        if (_health != null) _health.SetInvulnerable(false);
        EnableEnemies();
    }

    void SkipTutorial()
    {
        StopAllCoroutines();
        CompleteTutorial();
        if (_canvas != null) Destroy(_canvas.gameObject);
        SceneManager.LoadScene(GameSceneConfig.GameplayScene);
        Destroy(gameObject);
    }

    void OnDestroy()
    {
        // Garantiza que el jugador no quede invulnerable si el objeto se destruye de otra forma
        if (_health != null) _health.SetInvulnerable(false);
        _isRunning = false;
    }

    // ── Enemigos ──────────────────────────────────────────────────

    void GrabPlayerRefs()
    {
        var pc = FindAnyObjectByType<PlayerController2D>();
        if (pc != null)
        {
            _health = pc.GetComponent<PlayerHealth>();
            _dash   = pc.GetComponent<PlayerDash>();
        }
        _spawners = FindObjectsByType<EnemyRuntimeSpawner>();
    }

    void DisableEnemies()
    {
        if (_spawners != null)
            foreach (var s in _spawners) s.enabled = false;
        foreach (var e in FindObjectsByType<EnemySquareAI>())
            e.gameObject.SetActive(false);
    }

    void EnableEnemies()
    {
        if (_spawners != null)
            foreach (var s in _spawners) s.enabled = true;
    }

    // ── UI ────────────────────────────────────────────────────────

    void ShowStep(TutStep step, int idx, int total)
    {
        if (_counterText  != null)
            _counterText.text = total > 0 ? $"PASO {idx} / {total}   ·   [ ESC ] Saltar" : "";
        if (_titleText    != null) _titleText.text    = step.title;
        if (_descText     != null) _descText.text     = step.description ?? "";
        if (_hintText     != null) _hintText.text     = step.keyHint ?? "";
        if (_feedbackText != null) _feedbackText.text = "";
    }

    void SetProgressBar(float fill)
    {
        _holdFill = fill;
        if (_progressFill != null) _progressFill.fillAmount = fill;
    }

    void BuildUI()
    {
        // Canvas
        var cvGO = new GameObject("TutorialCanvas");
        _canvas = cvGO.AddComponent<Canvas>();
        _canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 150;
        var scaler = cvGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode        = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        cvGO.AddComponent<GraphicRaycaster>();

        // Panel anclado en la parte inferior central de la pantalla
        var panel = new GameObject("TutPanel");
        panel.transform.SetParent(cvGO.transform, false);
        panel.AddComponent<Image>().color = new Color(0.04f, 0.05f, 0.14f, 0.93f);
        var pRT = panel.GetComponent<RectTransform>();
        pRT.anchorMin        = new Vector2(0.05f, 0f);
        pRT.anchorMax        = new Vector2(0.95f, 0f);
        pRT.pivot            = new Vector2(0.5f, 0f);
        pRT.anchoredPosition = new Vector2(0f, 20f);
        pRT.sizeDelta        = new Vector2(0f, 290f);

        // Línea de acento superior
        var accentLine = new GameObject("AccentLine");
        accentLine.transform.SetParent(panel.transform, false);
        accentLine.AddComponent<Image>().color = new Color(0.35f, 0.65f, 1f, 0.9f);
        var alRT = accentLine.GetComponent<RectTransform>();
        alRT.anchorMin = new Vector2(0f, 1f);
        alRT.anchorMax = new Vector2(1f, 1f);
        alRT.pivot     = new Vector2(0.5f, 1f);
        alRT.anchoredPosition = Vector2.zero;
        alRT.sizeDelta        = new Vector2(0f, 4f);
        accentLine.AddComponent<LayoutElement>().ignoreLayout = true;

        var vl = panel.AddComponent<VerticalLayoutGroup>();
        vl.childAlignment     = TextAnchor.UpperCenter;
        vl.spacing            = 7f;
        vl.padding            = new RectOffset(50, 50, 16, 14);
        vl.childForceExpandWidth  = true;
        vl.childForceExpandHeight = false;

        // Contador de paso
        _counterText = AddVLText(panel.transform, "Counter", "", 20,
            new Color(0.55f, 0.75f, 1f), FontStyle.Normal, TextAnchor.MiddleLeft, 26f);

        // Título del paso
        _titleText = AddVLText(panel.transform, "Title", "", 44,
            Color.white, FontStyle.Bold, TextAnchor.MiddleCenter, 54f);

        // Descripción
        _descText = AddVLText(panel.transform, "Desc", "", 26,
            new Color(0.82f, 0.82f, 0.82f), FontStyle.Normal, TextAnchor.MiddleCenter, 66f);
        _descText.horizontalOverflow = HorizontalWrapMode.Wrap;
        _descText.verticalOverflow   = VerticalWrapMode.Overflow;

        // Caja de hint (tecla visual)
        var hintBg = new GameObject("HintBg");
        hintBg.transform.SetParent(panel.transform, false);
        hintBg.AddComponent<Image>().color = new Color(0.12f, 0.12f, 0.28f);
        hintBg.AddComponent<LayoutElement>().minHeight = 50f;

        _hintText = new GameObject("HintText").AddComponent<Text>();
        _hintText.transform.SetParent(hintBg.transform, false);
        _hintText.text      = "";
        _hintText.fontSize  = 28;
        _hintText.color     = new Color(1f, 0.88f, 0.25f);
        _hintText.fontStyle = FontStyle.Bold;
        _hintText.alignment = TextAnchor.MiddleCenter;
        _hintText.font      = GetUIFont();
        var hintRT = _hintText.GetComponent<RectTransform>();
        hintRT.anchorMin = Vector2.zero;
        hintRT.anchorMax = Vector2.one;
        hintRT.offsetMin = new Vector2(16, 0);
        hintRT.offsetMax = new Vector2(-16, 0);

        // Barra de progreso (fondo)
        var barBg = new GameObject("BarBg");
        barBg.transform.SetParent(panel.transform, false);
        barBg.AddComponent<Image>().color = new Color(0.08f, 0.08f, 0.08f);
        barBg.AddComponent<LayoutElement>().minHeight = 14f;

        // Barra de progreso (relleno)
        var barFill = new GameObject("BarFill");
        barFill.transform.SetParent(barBg.transform, false);
        _progressFill            = barFill.AddComponent<Image>();
        _progressFill.color      = new Color(0.25f, 0.85f, 0.35f);
        _progressFill.type       = Image.Type.Filled;
        _progressFill.fillMethod = Image.FillMethod.Horizontal;
        _progressFill.fillAmount = 0f;
        var fillRT = barFill.GetComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero;
        fillRT.anchorMax = Vector2.one;
        fillRT.offsetMin = fillRT.offsetMax = Vector2.zero;

        // Texto de feedback ("¡Bien hecho!")
        _feedbackText = AddVLText(panel.transform, "Feedback", "", 30,
            new Color(0.3f, 1f, 0.45f), FontStyle.Bold, TextAnchor.MiddleCenter, 36f);
    }

    Text AddVLText(Transform parent, string name, string content, int size,
        Color color, FontStyle style, TextAnchor align, float minH)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<LayoutElement>().minHeight = minH;
        var t = go.AddComponent<Text>();
        t.text      = content;
        t.fontSize  = size;
        t.color     = color;
        t.fontStyle = style;
        t.alignment = align;
        t.font      = GetUIFont();
        return t;
    }

    static Font GetUIFont()
    {
        var f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (f == null) f = Resources.GetBuiltinResource<Font>("Arial.ttf");
        return f;
    }
}
