using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Tutorial de 12 fases guiado por LUMA.
/// Controles: A=izq  D=der  W/ESPACIO=salto  SHIFT=dash
///            CLIC IZQ=disparo  CLIC DER (hold)=pulso  E=interactuar
/// Se auto-instala en TutorialScene.
/// </summary>
public class PlayableTutorialDirector : MonoBehaviour
{
    const string PREFS_KEY = "Tutorial_Shown_v1";

    // ── Auto-instalación ─────────────────────────────────────────────
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

    // ── Referencias al jugador ───────────────────────────────────────
    PlayerController2D  _player;
    PlayerHealth        _health;
    PlayerDash          _dash;
    PulseWeapon         _pulse;
    EnemyRuntimeSpawner[] _spawners;

    // ── Estado de fase ────────────────────────────────────────────────
    int   _phase       = 0;
    bool  _advancing;
    float _phaseStart;

    // ── Tracking de objetivos ─────────────────────────────────────────
    bool  _dashUsed;
    bool  _pulseFired;
    int   _dataCollected;
    bool  _terminalActivated;
    bool  _checkpointUsed;
    readonly List<GameObject> _phase6Enemies  = new List<GameObject>();
    readonly List<GameObject> _phase11Enemies = new List<GameObject>();

    // ── Anti-frustración ──────────────────────────────────────────────
    float   _stuckTimer;
    Vector3 _lastPos;
    float   _lastHintTime;
    bool    _arrowVisible;

    // ── UI ────────────────────────────────────────────────────────────
    Canvas      _canvas;
    Text        _lumaText;
    Text        _phaseCounter;
    Text        _keyHintText;
    Text        _objectiveText;
    GameObject  _arrowObj;
    Text        _arrowLabel;
    GameObject  _summaryPanel;

    // ── Posiciones clave (espacio de mundo) ───────────────────────────
    // (ground top = y -1.2;  respawn siempre a y=1f)
    const float RespawnY          = 1f;
    const float P3TargetX         = 20f;   // plataforma 1
    const float P3YThreshold      = 0.3f;  // player.y cuando está encima
    const float P4TargetX         = 30f;   // plataforma 2
    const float P4YThreshold      = 1.3f;
    const float P6ArenaX          = 63f;   // respawn antes de arena 1
    const float P9TerminalX       = 121f;
    const float P10CheckpointX    = 128f;
    const float P11ArenaX         = 135f;  // respawn antes de arena 2
    const float P12ExitX          = 168f;

    // ── Lifecycle ─────────────────────────────────────────────────────

    void Start()
    {
        GrabPlayerRefs();
        DisableSpawners();

        if (_health != null)
        {
            _health.SetInvulnerable(true);
            _health.SetRespawnPoint(new Vector3(0f, RespawnY, 0f));
        }

        BuildUI();
        StartCoroutine(BeginNextFrame());
    }

    IEnumerator BeginNextFrame()
    {
        yield return null;
        StartPhase(1);
    }

    void Update()
    {
        if (_phase < 1 || _advancing) return;

        UpdateStuckDetection();
        CheckPhaseCondition();

        if (GameInput.GetCancelDown())
            SkipTutorial();
    }

    // ── Gestión de fases ──────────────────────────────────────────────

    void StartPhase(int phase)
    {
        _phase      = phase;
        _advancing  = false;
        _phaseStart = Time.time;
        _stuckTimer = 0f;
        _lastPos    = _player != null ? _player.transform.position : Vector3.zero;
        _arrowVisible = false;
        if (_arrowObj != null) _arrowObj.SetActive(false);

        UpdateCounter(phase);
        SetupPhase(phase);
    }

    void SetupPhase(int phase)
    {
        switch (phase)
        {
            case 1:
                ShowLuma("Sistema corrompido detectado.\nSoy LUMA, tu guia. ERASER-OMEGA ataca el sistema. Te ensenaré lo basico para sobrevivir.");
                SetHint("");
                SetObjective("Escucha a LUMA");
                if (_health != null) _health.SetInvulnerable(true);
                break;

            case 2:
                ShowLuma("Usa  A  y  D  para moverte. Avanza 5 metros hacia la derecha.");
                SetHint("[ A ] ←     → [ D ]");
                SetObjective("Muevete 5 metros");
                if (_health != null) _health.SetInvulnerable(false);
                break;

            case 3:
                ShowLuma("Plataforma adelante. Usa  W  o  ESPACIO  para saltar y alcanzarla.");
                SetHint("[ W ]  o  [ ESPACIO ]  =  SALTAR");
                SetObjective("Sube a la plataforma baja");
                break;

            case 4:
                ShowLuma("Otra plataforma mas alta. Salta de nuevo.");
                SetHint("[ W ]  o  [ ESPACIO ]  =  SALTAR");
                SetObjective("Sube a la segunda plataforma");
                break;

            case 5:
                ShowLuma("Zona de dash adelante. Presiona  SHIFT  mientras te mueves para hacer DASH.\nDurante el dash eres INVULNERABLE.");
                SetHint("← o →   +   [ SHIFT ]  =  DASH");
                SetObjective("Haz DASH una vez");
                _dashUsed = false;
                break;

            case 6:
                ShowLuma("¡Alerta! Glitch Crawlers detectados.\nApunta con el MOUSE y dispara con CLIC IZQUIERDO.");
                SetHint("MOUSE   +   [ CLIC IZQ ]  =  DISPARAR");
                SetObjective("Elimina los 3 enemigos");
                SpawnPhase6Enemies();
                if (_health != null)
                {
                    _health.SetInvulnerable(false);
                    _health.SetRespawnPoint(new Vector3(P6ArenaX, RespawnY, 0f));
                }
                break;

            case 7:
                ShowLuma("Mantén presionado  CLIC DERECHO  para cargar el Pulso.\nSueltalo para disparar. Mas carga = mas daño.");
                SetHint("[ CLIC DER ] mantener   →   soltar");
                SetObjective("Usa el Pulso cargado");
                SpawnPhase7Enemy();
                _pulseFired = false;
                break;

            case 8:
                ShowLuma("Fragmentos de DATOS detectados.\nAcercate a los hexagonos cyan para recolectarlos automaticamente.");
                SetHint("(acercate a los fragmentos cyan)");
                SetObjective($"Recolecta fragmentos: 0/10");
                _dataCollected = 0;
                SpawnPhase8Data();
                break;

            case 9:
                ShowLuma("Ese es un TERMINAL de reparacion. Acercate y presiona  E  para activarlo.");
                SetHint("[ E ]  =  INTERACTUAR");
                SetObjective("Activa el Terminal");
                SpawnPhase9Terminal();
                _terminalActivated = false;
                break;

            case 10:
                ShowLuma("Esa columna azul es un CHECKPOINT.\nRestaura integridad al 100% y guarda tu progreso.");
                SetHint("(toca la columna azul)");
                SetObjective("Usa el Checkpoint");
                SpawnPhase10Checkpoint();
                _checkpointUsed = false;
                break;

            case 11:
                ShowLuma("Multiples hostiles. Usa TODO lo aprendido:\nmovimiento, salto, dash, disparo y pulso.");
                SetHint("¡Usa todo lo aprendido!");
                SetObjective("Elimina todos los enemigos");
                SpawnPhase11Enemies();
                if (_health != null)
                    _health.SetRespawnPoint(new Vector3(P11ArenaX, RespawnY, 0f));
                break;

            case 12:
                ShowLuma("ERASER-OMEGA esta cerca. Ten cuidado. Buena suerte, CODIGO-7.");
                SetHint("");
                ShowSummaryPanel();
                SpawnTutorialExit();
                break;
        }
    }

    void CheckPhaseCondition()
    {
        if (_player == null) return;
        Vector3 pos = _player.transform.position;

        switch (_phase)
        {
            case 1:
                if (Time.time - _phaseStart > 4f)
                    CompletePhase("Bien. Sigues respondiendo.");
                break;

            case 2:
                if (pos.x > 5f)
                    CompletePhase("Bien. Sistema de movimiento: ONLINE.");
                break;

            case 3:
                if (_player.IsGrounded && pos.y > P3YThreshold && pos.x > P3TargetX)
                    CompletePhase("Correcto. El salto te permite alcanzar zonas altas.");
                break;

            case 4:
                if (_player.IsGrounded && pos.y > P4YThreshold && pos.x > P4TargetX)
                    CompletePhase("Perfecto. Altitud maxima alcanzada.");
                break;

            case 5:
                if (_dash != null && _dash.IsDashing)
                    _dashUsed = true;
                if (_dashUsed && (_dash == null || !_dash.IsDashing))
                    CompletePhase("Perfecto. Recuerda: el dash tiene cooldown de 0.8s. Usalo sabiamente.");
                break;

            case 6:
                _phase6Enemies.RemoveAll(e => e == null);
                if (_phase6Enemies.Count == 0 && Time.time - _phaseStart > 1f)
                    CompletePhase("Area despejada. Bien hecho, CODIGO-7.");
                if (_health != null && _health.currentHP < 40)
                    TryShowHint("¡Daño recibido! Usa DASH para esquivar los proximos golpes.");
                break;

            case 7:
                if (_pulseFired)
                    CompletePhase("Impacto confirmado. El pulso tiene cooldown de 8s. Reservalo para emergencias.");
                break;

            case 8:
                if (_dataCollected >= 10)
                    CompletePhase("Fragmentos recuperados. Sigue recolectando en la mision real.");
                SetObjective($"Recolecta fragmentos: {_dataCollected}/10");
                break;

            case 9:
                if (_terminalActivated)
                    CompletePhase("Terminal activado. Necesitas 3 terminales para abrir la salida del nivel.");
                break;

            case 10:
                if (_checkpointUsed)
                    CompletePhase("Integridad restaurada al 100%. Si mueres, reapareces aqui.");
                break;

            case 11:
                _phase11Enemies.RemoveAll(e => e == null);
                if (_phase11Enemies.Count == 0 && Time.time - _phaseStart > 1f)
                    CompletePhase("Area asegurada. Estas listo para la mision real.");
                if (_health != null && _health.currentHP < 40)
                    TryShowHint("¡Cuidado! Usa DASH para esquivar y sigue moviendote.");
                break;

            case 12:
                // Exit trigger calls OnExitReached(); auto-finish after 6s fallback.
                if (Time.time - _phaseStart > 8f)
                    FinishTutorial();
                break;
        }
    }

    void CompletePhase(string lumaFeedback)
    {
        if (_advancing) return;
        _advancing = true;
        StartCoroutine(AdvanceRoutine(lumaFeedback));
    }

    IEnumerator AdvanceRoutine(string feedback)
    {
        ShowLuma(feedback);
        if (_arrowObj != null) _arrowObj.SetActive(false);
        yield return new WaitForSeconds(2f);
        int next = _phase + 1;
        if (next <= 12)
            StartPhase(next);
        else
            FinishTutorial();
    }

    // ── Stuck detection & arrow ───────────────────────────────────────

    void UpdateStuckDetection()
    {
        if (_player == null) return;
        Vector3 pos = _player.transform.position;

        if (Vector3.Distance(pos, _lastPos) > 0.4f)
        {
            _stuckTimer   = 0f;
            _lastPos      = pos;
            _arrowVisible = false;
        }
        else
        {
            _stuckTimer += Time.deltaTime;
        }

        bool shouldShowArrow = _stuckTimer > 15f;
        if (shouldShowArrow != _arrowVisible)
        {
            _arrowVisible = shouldShowArrow;
            if (_arrowObj != null) _arrowObj.SetActive(_arrowVisible);
        }

        if (_arrowVisible)
        {
            UpdateArrowDirection(pos);
            TryRepeatHint();
        }
    }

    void UpdateArrowDirection(Vector3 playerPos)
    {
        Vector3 target = GetPhaseTarget();
        if (_arrowLabel == null) return;
        Vector3 dir = target - playerPos;
        if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
            _arrowLabel.text = dir.x > 0 ? "→" : "←";
        else
            _arrowLabel.text = dir.y > 0 ? "↑" : "↓";
    }

    Vector3 GetPhaseTarget()
    {
        switch (_phase)
        {
            case 2:  return new Vector3(8f,  0f, 0f);
            case 3:  return new Vector3(22f, 2f, 0f);
            case 4:  return new Vector3(33f, 3f, 0f);
            case 5:  return new Vector3(52f, 0f, 0f);
            case 6:  return new Vector3(74f, 0f, 0f);
            case 7:  return new Vector3(91f, 0f, 0f);
            case 8:  return new Vector3(108f, 0f, 0f);
            case 9:  return new Vector3(P9TerminalX, 0f, 0f);
            case 10: return new Vector3(P10CheckpointX, 0f, 0f);
            case 11: return new Vector3(148f, 0f, 0f);
            case 12: return new Vector3(P12ExitX, 0f, 0f);
            default: return Vector3.right * 10f;
        }
    }

    void TryRepeatHint()
    {
        if (Time.time - _lastHintTime < 18f) return;
        _lastHintTime = Time.time;

        switch (_phase)
        {
            case 2:  ShowLuma("Usa  A  y  D  para avanzar hacia la derecha."); break;
            case 3:  ShowLuma("Pulsa  W  o  ESPACIO  para saltar a la plataforma."); break;
            case 4:  ShowLuma("Pulsa  W  o  ESPACIO  para saltar a la plataforma mas alta."); break;
            case 5:  ShowLuma("Presiona SHIFT mientras te mueves para hacer dash."); break;
            case 6:  ShowLuma("Apunta con el mouse y dispara con clic izquierdo."); break;
            case 7:  ShowLuma("Mantén presionado el clic derecho, luego sueltalo para disparar."); break;
            case 8:  ShowLuma("Acercate a los hexagonos cyan flotantes."); break;
            case 9:  ShowLuma("Acercate al terminal rojo y presiona  E."); break;
            case 10: ShowLuma("Toca la columna azul para activar el checkpoint."); break;
            case 11: ShowLuma("Elimina todos los enemigos del area."); break;
        }
    }

    void TryShowHint(string msg)
    {
        if (Time.time - _lastHintTime < 8f) return;
        _lastHintTime = Time.time;
        ShowLuma(msg);
    }

    // ── Spawn de objetos tutorial ─────────────────────────────────────

    void SpawnPhase6Enemies()
    {
        _phase6Enemies.Clear();
        float[] xs = { 68f, 73f, 78f };
        foreach (float x in xs)
        {
            var e = SpawnEnemy(new Vector2(x, RespawnY), hp: 10, patrol: 1.5f, chase: 2.5f);
            _phase6Enemies.Add(e);
        }
    }

    void SpawnPhase7Enemy()
    {
        // One tougher enemy that teaches the player to use the charged pulse
        SpawnEnemy(new Vector2(91f, RespawnY), hp: 50, patrol: 1f, chase: 1.8f);
    }

    void SpawnPhase8Data()
    {
        float[] xs = { 101f, 103f, 105f, 107f, 109f, 111f, 104f, 108f, 112f, 114f };
        float[] ys = { 0.5f, 0.8f, 0.3f, 0.7f, 0.5f, 0.8f,  1.2f, 1.0f, 0.4f, 0.7f };
        for (int i = 0; i < xs.Length; i++)
            SpawnDataFragment(new Vector2(xs[i], ys[i]));
    }

    void SpawnPhase9Terminal()
    {
        var go = new GameObject("TutorialTerminal");
        go.transform.position  = new Vector3(P9TerminalX, -0.4f, 0f);
        go.transform.localScale = new Vector3(1.2f, 2.2f, 1f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite       = MakeSquareSprite();
        sr.color        = new Color(0.7f, 0.12f, 0.12f);
        sr.sortingOrder = 4;

        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size      = Vector2.one;

        AddWorldLabel(go.transform, "[E]", new Vector2(0f, 0.9f), new Color(0.3f, 1f, 0.85f));

        var trig = go.AddComponent<TutTerminalTrigger>();
        trig.director = this;
    }

    void SpawnPhase10Checkpoint()
    {
        var go = new GameObject("TutorialCheckpoint");
        go.transform.position  = new Vector3(P10CheckpointX, 0f, 0f);
        go.transform.localScale = new Vector3(1.5f, 3f, 1f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite       = MakeSquareSprite();
        sr.color        = new Color(0.15f, 0.45f, 0.9f, 0.85f);
        sr.sortingOrder = 4;

        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size      = Vector2.one;

        AddWorldLabel(go.transform, "CHECKPOINT", new Vector2(0f, 0.85f), new Color(0.5f, 0.8f, 1f));

        var trig = go.AddComponent<TutCheckpointTrigger>();
        trig.director = this;
    }

    void SpawnPhase11Enemies()
    {
        _phase11Enemies.Clear();

        // 4 Glitch Crawlers (terrestres)
        float[] groundXs = { 137f, 143f, 152f, 158f };
        foreach (float x in groundXs)
        {
            var e = SpawnEnemy(new Vector2(x, RespawnY), hp: 20, patrol: 2f, chase: 3.2f);
            _phase11Enemies.Add(e);
        }

        // 2 Error Ghosts (voladores – gravedad cero)
        Vector2[] airPos = { new Vector2(140f, 3f), new Vector2(154f, 3.5f) };
        foreach (var p in airPos)
        {
            var e = SpawnEnemy(p, hp: 15, patrol: 1.8f, chase: 2.8f, flying: true);
            _phase11Enemies.Add(e);
        }
    }

    void SpawnTutorialExit()
    {
        var go = new GameObject("TutorialExit");
        go.transform.position  = new Vector3(P12ExitX, 0f, 0f);
        go.transform.localScale = new Vector3(2.5f, 4f, 1f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite       = MakeSquareSprite();
        sr.color        = new Color(0.14f, 0.55f, 0.82f, 0.9f);
        sr.sortingOrder = 4;

        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size      = Vector2.one;

        AddWorldLabel(go.transform, "INICIO\nMISION", new Vector2(0f, 0.9f), Color.white);

        var trig = go.AddComponent<TutExitTrigger>();
        trig.director = this;
    }

    // ── Helpers de spawn ──────────────────────────────────────────────

    GameObject SpawnEnemy(Vector2 pos, int hp, float patrol, float chase, bool flying = false)
    {
        var go = new GameObject("TutorialEnemy");
        int eLayer = LayerMask.NameToLayer("Enemy");
        go.layer = eLayer >= 0 ? eLayer : 0;
        go.transform.position   = pos;
        go.transform.localScale = Vector3.one * 0.65f;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite       = MakeSquareSprite();
        sr.color        = flying ? new Color(0.55f, 0.2f, 0.9f) : new Color(0.9f, 0.2f, 0.2f);
        sr.sortingOrder = 2;

        go.AddComponent<BoxCollider2D>().size = Vector2.one;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.constraints            = RigidbodyConstraints2D.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        var ai = go.AddComponent<EnemySquareAI>(); // Awake() sets gravityScale=3, so override after
        ai.maxHp         = hp;
        ai.contactDamage = 8;
        ai.patrolSpeed   = patrol;
        ai.chaseSpeed    = chase;
        ai.patrolRange   = 4f;
        ai.detectRange   = 7f;
        ai.attackRange   = 0.6f;

        if (flying) rb.gravityScale = 0f; // must come after EnemySquareAI.Awake()

        var marker = go.AddComponent<TutEnemyMarker>();
        marker.director = this;

        return go;
    }

    void SpawnDataFragment(Vector2 pos)
    {
        var go = new GameObject("TutorialDataFrag");
        go.transform.position   = pos;
        go.transform.localScale = Vector3.one * 0.4f;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite       = MakeSquareSprite();
        sr.color        = new Color(0.2f, 1f, 0.85f);
        sr.sortingOrder = 5;

        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius    = 0.7f;

        var frag = go.AddComponent<TutDataFragTrigger>();
        frag.director = this;
    }

    static Sprite MakeSquareSprite()
    {
        const int sz = 32;
        var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        var px = new Color32[sz * sz];
        for (int i = 0; i < px.Length; i++) px[i] = new Color32(255, 255, 255, 255);
        tex.SetPixels32(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f), sz);
    }

    static void AddWorldLabel(Transform parent, string text, Vector2 localPos, Color color)
    {
        var go = new GameObject("Label");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(localPos.x, localPos.y, 0f);
        var tm = go.AddComponent<TextMesh>();
        tm.text          = text;
        tm.fontSize      = 36;
        tm.characterSize = 0.075f;
        tm.anchor        = TextAnchor.MiddleCenter;
        tm.alignment     = TextAlignment.Center;
        tm.color         = color;
    }

    // ── Public callbacks (llamados por triggers) ───────────────────────

    public void OnDataCollected()
    {
        _dataCollected++;
        TryShowHint($"Dato recuperado. Progreso: {_dataCollected}/10");
    }

    public void OnTerminalActivated()
    {
        _terminalActivated = true;
        var t = GameObject.Find("TutorialTerminal");
        if (t != null) { var sr = t.GetComponent<SpriteRenderer>(); if (sr != null) sr.color = new Color(0.15f, 1f, 0.72f); }
    }

    public void OnCheckpointTouched()
    {
        _checkpointUsed = true;
        if (_health != null)
        {
            _health.currentHP = _health.maxHP;
            _health.SetRespawnPoint(new Vector3(P10CheckpointX - 2f, RespawnY, 0f));
        }
    }

    public void OnExitReached()
    {
        if (_phase == 12) FinishTutorial();
    }

    // PulseWeapon fires this via delegate
    void OnPulseFiredCallback() => _pulseFired = true;

    // ── Player refs ────────────────────────────────────────────────────

    void GrabPlayerRefs()
    {
        var pc = FindAnyObjectByType<PlayerController2D>();
        if (pc == null) return;

        _player = pc;
        _health = pc.GetComponent<PlayerHealth>();
        _dash   = pc.GetComponent<PlayerDash>();

        _pulse = pc.GetComponent<PulseWeapon>();
        if (_pulse == null) _pulse = pc.gameObject.AddComponent<PulseWeapon>();
        _pulse.OnPulseFired += OnPulseFiredCallback;

        _spawners = FindObjectsByType<EnemyRuntimeSpawner>(FindObjectsSortMode.None);
    }

    void DisableSpawners()
    {
        if (_spawners != null)
            foreach (var s in _spawners) if (s != null) s.enabled = false;

        foreach (var e in FindObjectsByType<EnemySquareAI>(FindObjectsSortMode.None))
            if (e != null) e.gameObject.SetActive(false);
    }

    // ── Completar / saltar ─────────────────────────────────────────────

    void FinishTutorial()
    {
        if (_advancing) return;
        _advancing = true;
        StartCoroutine(FinishRoutine());
    }

    IEnumerator FinishRoutine()
    {
        ShowLuma("TUTORIAL COMPLETADO — MISION INICIADA");
        SetHint("");
        SetObjective("");
        yield return new WaitForSeconds(3f);
        Finalize();
        SceneManager.LoadScene(GameSceneConfig.GameplayScene);
        Destroy(gameObject);
    }

    void SkipTutorial()
    {
        StopAllCoroutines();
        Finalize();
        SceneManager.LoadScene(GameSceneConfig.GameplayScene);
        Destroy(gameObject);
    }

    void Finalize()
    {
        PlayerPrefs.SetInt(PREFS_KEY, 1);
        PlayerPrefs.Save();
        if (_health != null) _health.SetInvulnerable(false);
        if (_spawners != null)
            foreach (var s in _spawners) if (s != null) s.enabled = true;
        if (_pulse != null) _pulse.OnPulseFired -= OnPulseFiredCallback;
    }

    void OnDestroy()
    {
        if (_health != null) _health.SetInvulnerable(false);
        if (_pulse != null) _pulse.OnPulseFired -= OnPulseFiredCallback;
    }

    // ── UI Builder ────────────────────────────────────────────────────

    void BuildUI()
    {
        var cvGO = new GameObject("TutorialCanvas");
        _canvas = cvGO.AddComponent<Canvas>();
        _canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 150;
        var scaler = cvGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        cvGO.AddComponent<GraphicRaycaster>();

        // ─ Phase counter (top-left) ─
        var counterGO = MakeRT(cvGO.transform, "Counter",
            min: new Vector2(0f, 1f), max: new Vector2(0f, 1f),
            pivot: new Vector2(0f, 1f),
            oMin: new Vector2(20f, -44f), oMax: new Vector2(560f, -10f));
        counterGO.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);
        _phaseCounter = AddText(counterGO.transform, "CounterTxt", "", 20,
            new Color(0.55f, 0.8f, 1f), FontStyle.Normal, TextAnchor.MiddleLeft,
            Vector2.zero, Vector2.one, new Vector2(14f, 4f), new Vector2(-14f, -4f));

        // ─ LUMA panel (bottom-left) ─
        var lumaPanel = MakeRT(cvGO.transform, "LumaPanel",
            min: new Vector2(0f, 0f), max: new Vector2(0f, 0f),
            pivot: new Vector2(0f, 0f),
            oMin: new Vector2(20f, 20f), oMax: new Vector2(540f, 170f));
        lumaPanel.AddComponent<Image>().color = new Color(0.04f, 0.06f, 0.16f, 0.93f);

        // Accent line (left edge)
        var accent = MakeRT(lumaPanel.transform, "Accent",
            min: new Vector2(0f, 0f), max: new Vector2(0f, 1f),
            pivot: new Vector2(0f, 0.5f),
            oMin: Vector2.zero, oMax: new Vector2(4f, 0f));
        accent.AddComponent<Image>().color = new Color(0.3f, 0.85f, 1f);

        // "LUMA" label
        AddText(lumaPanel.transform, "LumaName", "LUMA", 22,
            new Color(0.3f, 0.9f, 1f), FontStyle.Bold, TextAnchor.UpperLeft,
            new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(14f, -36f), new Vector2(-10f, -8f));

        // Dialogue text
        _lumaText = AddText(lumaPanel.transform, "LumaDlg", "", 19,
            new Color(0.88f, 0.88f, 0.88f), FontStyle.Normal, TextAnchor.UpperLeft,
            Vector2.zero, Vector2.one,
            new Vector2(14f, 8f), new Vector2(-10f, -42f));
        _lumaText.horizontalOverflow = HorizontalWrapMode.Wrap;
        _lumaText.verticalOverflow   = VerticalWrapMode.Overflow;

        // ─ Key hint panel (bottom-right) ─
        var hintPanel = MakeRT(cvGO.transform, "HintPanel",
            min: new Vector2(1f, 0f), max: new Vector2(1f, 0f),
            pivot: new Vector2(1f, 0f),
            oMin: new Vector2(-440f, 20f), oMax: new Vector2(-20f, 110f));
        hintPanel.AddComponent<Image>().color = new Color(0.08f, 0.10f, 0.24f, 0.92f);
        _keyHintText = AddText(hintPanel.transform, "HintTxt", "", 22,
            new Color(1f, 0.9f, 0.3f), FontStyle.Bold, TextAnchor.MiddleCenter,
            Vector2.zero, Vector2.one, new Vector2(12f, 6f), new Vector2(-12f, -6f));

        // ─ Objective panel (top-right) ─
        var objPanel = MakeRT(cvGO.transform, "ObjPanel",
            min: new Vector2(1f, 1f), max: new Vector2(1f, 1f),
            pivot: new Vector2(1f, 1f),
            oMin: new Vector2(-440f, -200f), oMax: new Vector2(-20f, -50f));
        objPanel.AddComponent<Image>().color = new Color(0.04f, 0.06f, 0.16f, 0.88f);

        AddText(objPanel.transform, "ObjTitle", "OBJETIVO", 18,
            new Color(0.55f, 0.9f, 0.55f), FontStyle.Bold, TextAnchor.UpperLeft,
            new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(12f, -28f), new Vector2(-8f, -4f));

        _objectiveText = AddText(objPanel.transform, "ObjTxt", "", 20,
            Color.white, FontStyle.Normal, TextAnchor.UpperLeft,
            Vector2.zero, Vector2.one, new Vector2(12f, 8f), new Vector2(-8f, -32f));
        _objectiveText.horizontalOverflow = HorizontalWrapMode.Wrap;
        _objectiveText.verticalOverflow   = VerticalWrapMode.Overflow;

        // ─ Direction arrow (center, hidden by default) ─
        _arrowObj = new GameObject("Arrow");
        _arrowObj.transform.SetParent(cvGO.transform, false);
        var arRT = _arrowObj.AddComponent<RectTransform>();
        arRT.anchorMin       = arRT.anchorMax = new Vector2(0.5f, 0.5f);
        arRT.pivot           = new Vector2(0.5f, 0.5f);
        arRT.anchoredPosition = new Vector2(0f, -210f);
        arRT.sizeDelta       = new Vector2(70f, 70f);
        _arrowObj.AddComponent<Image>().color = new Color(1f, 0.85f, 0.1f, 0.82f);

        _arrowLabel = new GameObject("ArrowLbl").AddComponent<Text>();
        _arrowLabel.transform.SetParent(_arrowObj.transform, false);
        var alRT = _arrowLabel.GetComponent<RectTransform>();
        alRT.anchorMin = Vector2.zero; alRT.anchorMax = Vector2.one;
        alRT.offsetMin = alRT.offsetMax = Vector2.zero;
        _arrowLabel.text      = "→";
        _arrowLabel.fontSize  = 52;
        _arrowLabel.color     = Color.black;
        _arrowLabel.fontStyle = FontStyle.Bold;
        _arrowLabel.alignment = TextAnchor.MiddleCenter;
        _arrowLabel.font      = GetFont();
        _arrowObj.SetActive(false);
    }

    void ShowSummaryPanel()
    {
        if (_summaryPanel != null) return;
        _summaryPanel = new GameObject("SummaryPanel");
        _summaryPanel.transform.SetParent(_canvas.transform, false);
        var rt = _summaryPanel.AddComponent<RectTransform>();
        rt.anchorMin       = new Vector2(0.5f, 0.5f);
        rt.anchorMax       = new Vector2(0.5f, 0.5f);
        rt.pivot           = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta       = new Vector2(780f, 360f);
        _summaryPanel.AddComponent<Image>().color = new Color(0.04f, 0.08f, 0.16f, 0.96f);

        AddText(_summaryPanel.transform, "STitle", "MISION PRINCIPAL", 32,
            new Color(0.4f, 0.9f, 1f), FontStyle.Bold, TextAnchor.UpperCenter,
            new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(20f, -56f), new Vector2(-20f, -8f));

        AddText(_summaryPanel.transform, "SBody",
            "1.  Recolectar  80  DATOS\n2.  Activar  3  TERMINALES\n3.  Llegar al  PUERTO DE SALIDA",
            26, Color.white, FontStyle.Normal, TextAnchor.UpperLeft,
            Vector2.zero, Vector2.one,
            new Vector2(40f, 30f), new Vector2(-40f, -80f));

        AddText(_summaryPanel.transform, "SHint",
            "Avanza hasta el portal azul para comenzar la mision",
            20, new Color(0.6f, 0.8f, 1f), FontStyle.Normal, TextAnchor.LowerCenter,
            new Vector2(0f, 0f), new Vector2(1f, 0f),
            new Vector2(20f, 12f), new Vector2(-20f, 40f));
    }

    // ── UI Helpers ────────────────────────────────────────────────────

    void ShowLuma(string text)    { if (_lumaText      != null) _lumaText.text      = text; }
    void SetHint(string hint)     { if (_keyHintText   != null) _keyHintText.text   = hint; }
    void SetObjective(string obj) { if (_objectiveText != null) _objectiveText.text = obj;  }

    void UpdateCounter(int phase)
    {
        if (_phaseCounter != null)
            _phaseCounter.text = $"Tutorial: {phase} / 12     [ ESC ] Saltar";
    }

    static GameObject MakeRT(Transform parent, string name,
        Vector2 min, Vector2 max, Vector2 pivot,
        Vector2 oMin, Vector2 oMax)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = min; rt.anchorMax = max; rt.pivot = pivot;
        rt.offsetMin = oMin; rt.offsetMax = oMax;
        return go;
    }

    static Text AddText(Transform parent, string name, string content,
        int size, Color color, FontStyle style, TextAnchor align,
        Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = aMin; rt.anchorMax = aMax;
        rt.offsetMin = oMin; rt.offsetMax = oMax;
        var t = go.AddComponent<Text>();
        t.text = content; t.fontSize = size; t.color = color;
        t.fontStyle = style; t.alignment = align;
        t.font = GetFont();
        return t;
    }

    static Font GetFont()
    {
        var f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        return f != null ? f : Resources.GetBuiltinResource<Font>("Arial.ttf");
    }
}

// ── Componentes auxiliares ────────────────────────────────────────────────

public class TutEnemyMarker : MonoBehaviour
{
    public PlayableTutorialDirector director;
    // Enemy dies via Destroy → director's List detects via null check.
}

public class TutDataFragTrigger : MonoBehaviour
{
    public PlayableTutorialDirector director;
    bool _used;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (_used) return;
        if (other.GetComponent<PlayerController2D>() == null &&
            other.GetComponentInParent<PlayerController2D>() == null) return;
        _used = true;
        director?.OnDataCollected();
        Destroy(gameObject);
    }
}

public class TutTerminalTrigger : MonoBehaviour
{
    public PlayableTutorialDirector director;
    bool _inside;
    bool _activated;

    void Update()
    {
        if (!_inside || _activated) return;
        if (!GameInput.GetKeyDown(KeyCode.E)) return;
        _activated = true;
        director?.OnTerminalActivated();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<PlayerController2D>() != null ||
            other.GetComponentInParent<PlayerController2D>() != null)
            _inside = true;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.GetComponent<PlayerController2D>() != null ||
            other.GetComponentInParent<PlayerController2D>() != null)
            _inside = false;
    }
}

public class TutCheckpointTrigger : MonoBehaviour
{
    public PlayableTutorialDirector director;
    bool _used;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (_used) return;
        if (other.GetComponent<PlayerController2D>() == null &&
            other.GetComponentInParent<PlayerController2D>() == null) return;
        _used = true;
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = new Color(0.3f, 0.65f, 1f, 1f);
        director?.OnCheckpointTouched();
    }
}

public class TutExitTrigger : MonoBehaviour
{
    public PlayableTutorialDirector director;
    bool _triggered;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (_triggered) return;
        if (other.GetComponent<PlayerController2D>() == null &&
            other.GetComponentInParent<PlayerController2D>() == null) return;
        _triggered = true;
        director?.OnExitReached();
    }
}
