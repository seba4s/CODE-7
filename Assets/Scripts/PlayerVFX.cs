using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Efectos visuales cyberpunk del personaje:
///   - Dash: trail cyan + afterimages moradas que se desvanecen
///   - Salto: explosión de partículas en los pies
///   - Aterrizaje: burst + sacudida de cámara
///   - Caminar: chispas en los pies
///   - Disparo: destello en el cuerpo + sacudida de cámara
/// SE AUTO-INSTALA en el jugador al dar Play.
/// </summary>
public class PlayerVFX : MonoBehaviour
{
    // ── Paleta cyberpunk ───────────────────────────────────────────
    static readonly Color CyanBright  = new Color(0.05f, 1.00f, 1.00f, 1.0f);
    static readonly Color PurpleGhost = new Color(0.55f, 0.00f, 1.00f, 0.7f);
    static readonly Color Magenta     = new Color(1.00f, 0.10f, 0.80f, 1.0f);
    static readonly Color YellowGlow  = new Color(1.00f, 0.92f, 0.10f, 1.0f);

    // ── Referencias ────────────────────────────────────────────────
    Rigidbody2D    rb;
    PlayerDash     dash;
    PlayerController2D controller;
    SpriteRenderer bodySR;
    CameraFollow   camFollow;

    // ── Trail ──────────────────────────────────────────────────────
    TrailRenderer trail;

    // ── Particle Systems ───────────────────────────────────────────
    ParticleSystem jumpPS;
    ParticleSystem landPS;
    ParticleSystem footstepPS;
    ParticleSystem dashBurstPS;

    // ── Estado ────────────────────────────────────────────────────
    bool  wasDashing;
    bool  wasGrounded;
    float lastFootstepTime;
    float lastAfterimageTime;

    const float FootstepInterval  = 0.22f;
    const float AfterimageInterval = 0.045f;

    // ── Auto-instalación ──────────────────────────────────────────
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void AutoInstall()
    {
        SceneManager.sceneLoaded += (scene, _) =>
        {
            if (!GameSceneConfig.IsCombatInputScene(scene.name)) return;
            var pc = FindAnyObjectByType<PlayerController2D>();
            if (pc == null) return;
            if (pc.GetComponent<PlayerVFX>() != null) return;
            pc.gameObject.AddComponent<PlayerVFX>();
            Debug.Log("[PlayerVFX] Auto-instalado.");
        };
    }

    // ── Awake ─────────────────────────────────────────────────────
    void Awake()
    {
        rb         = GetComponent<Rigidbody2D>();
        dash       = GetComponent<PlayerDash>();
        controller = GetComponent<PlayerController2D>();
        bodySR     = GetComponentInChildren<SpriteRenderer>();
        camFollow  = FindAnyObjectByType<CameraFollow>();

        BuildTrail();
        BuildParticleSystems();

        var shooter = GetComponent<HitscanShooter>();
        if (shooter != null) shooter.OnShot += HandleShot;
    }

    void OnDestroy()
    {
        var shooter = GetComponent<HitscanShooter>();
        if (shooter != null) shooter.OnShot -= HandleShot;
    }

    // ── Trail cyan/púrpura durante el dash ────────────────────────
    void BuildTrail()
    {
        trail = gameObject.AddComponent<TrailRenderer>();
        trail.time               = 0.22f;
        trail.startWidth         = 0.6f;
        trail.endWidth           = 0f;
        trail.emitting           = false;
        trail.minVertexDistance  = 0.06f;
        trail.shadowCastingMode  = UnityEngine.Rendering.ShadowCastingMode.Off;
        trail.receiveShadows     = false;

        var mat = new Material(Shader.Find("Sprites/Default"));
        mat.color = Color.white;
        trail.material = mat;

        var g = new Gradient();
        g.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(CyanBright,  0f),
                new GradientColorKey(PurpleGhost, 0.6f),
                new GradientColorKey(Magenta,     1f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(0.9f, 0f),
                new GradientAlphaKey(0.3f, 0.7f),
                new GradientAlphaKey(0f,   1f)
            }
        );
        trail.colorGradient = g;
    }

    // ── Partículas ────────────────────────────────────────────────
    void BuildParticleSystems()
    {
        jumpPS      = MakePS("VFX_Jump",      CyanBright, Color.white,  12, 0.13f, 3.2f, 0.22f, feetOffset: true);
        landPS      = MakePS("VFX_Land",      CyanBright, YellowGlow,   26, 0.10f, 5.0f, 0.15f, feetOffset: true);
        footstepPS  = MakePS("VFX_Footstep",  new Color(0f, 0.85f, 1f, 0.9f), Color.clear, 6, 0.06f, 1.8f, 0.20f, feetOffset: true);
        dashBurstPS = MakePS("VFX_DashBurst", CyanBright, Magenta,      22, 0.11f, 6.0f, 0.10f, feetOffset: false);
    }

    ParticleSystem MakePS(string psName, Color c0, Color c1,
                          int maxPart, float size, float speed, float lifetime,
                          bool feetOffset)
    {
        var go = new GameObject(psName);
        go.transform.SetParent(transform, false);
        go.transform.localPosition = feetOffset ? new Vector3(0f, -0.42f, 0f) : Vector3.zero;

        var ps   = go.AddComponent<ParticleSystem>();
        var rend = ps.GetComponent<ParticleSystemRenderer>();

        var mat = new Material(Shader.Find("Sprites/Default"));
        mat.color = Color.white;
        rend.material          = mat;
        rend.renderMode        = ParticleSystemRenderMode.Billboard;
        rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        var main = ps.main;
        main.loop          = false;
        main.playOnAwake   = false;
        main.maxParticles  = maxPart;
        main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime * 0.55f, lifetime);
        main.startSpeed    = new ParticleSystem.MinMaxCurve(speed * 0.35f, speed);
        main.startSize     = new ParticleSystem.MinMaxCurve(size  * 0.5f, size);
        main.startColor    = new ParticleSystem.MinMaxGradient(c0, c1);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = 0.55f;

        var emission = ps.emission;
        emission.enabled      = true;
        emission.rateOverTime = 0f;

        var shape = ps.shape;
        shape.enabled   = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius    = 0.25f;

        return ps;
    }

    // ── Update ────────────────────────────────────────────────────
    void Update()
    {
        bool isDashing  = dash != null && dash.IsDashing;
        bool isGrounded = controller != null
            ? controller.IsGrounded
            : Physics2D.OverlapCircle((Vector2)transform.position + Vector2.down * 0.5f, 0.22f, LayerMask.GetMask("Ground"));

        float velY = rb.linearVelocity.y;

        // ── Trail durante el dash ───────────────────────────────
        trail.emitting = isDashing;

        // ── Inicio del dash: burst + afterimages ────────────────
        if (isDashing && !wasDashing)
            EmitDashBurst();

        if (isDashing && Time.time > lastAfterimageTime + AfterimageInterval)
        {
            SpawnAfterimage();
            lastAfterimageTime = Time.time;
        }

        // ── Salto ───────────────────────────────────────────────
        if (!isGrounded && wasGrounded && velY > 0.5f)
            EmitJump();

        // ── Aterrizaje ──────────────────────────────────────────
        if (isGrounded && !wasGrounded && velY <= 0f)
            EmitLand();

        // ── Pisadas mientras camina ─────────────────────────────
        if (isGrounded && !isDashing
            && Mathf.Abs(rb.linearVelocity.x) > 1.5f
            && Time.time > lastFootstepTime + FootstepInterval)
        {
            EmitFootstep();
        }

        wasDashing  = isDashing;
        wasGrounded = isGrounded;
    }

    // ── Efectos ───────────────────────────────────────────────────
    void EmitDashBurst() => dashBurstPS.Emit(20);

    void EmitJump()
    {
        jumpPS.Emit(12);
    }

    void EmitLand()
    {
        landPS.Emit(24);
        if (camFollow != null) camFollow.Shake(0.09f, 0.13f);
    }

    void EmitFootstep()
    {
        lastFootstepTime = Time.time;
        footstepPS.Emit(5);
    }

    void HandleShot(Vector2 muzzlePos, Vector2 dir)
    {
        if (bodySR != null) StartCoroutine(BodyFlashRoutine());
        if (camFollow != null) camFollow.Shake(0.055f, 0.08f);
    }

    IEnumerator BodyFlashRoutine()
    {
        Color original = bodySR.color;
        bodySR.color = YellowGlow;
        yield return new WaitForSeconds(0.04f);
        if (bodySR != null) bodySR.color = original;
    }

    // ── Afterimage cian que se desvanece ──────────────────────────
    void SpawnAfterimage()
    {
        var go = new GameObject("Afterimage");
        go.transform.position   = transform.position;
        go.transform.localScale = transform.localScale;

        var ghost = go.AddComponent<SpriteRenderer>();

        if (bodySR != null && bodySR.sprite != null)
        {
            ghost.sprite      = bodySR.sprite;
            ghost.sortingOrder = bodySR.sortingOrder - 1;
            ghost.flipX       = bodySR.flipX;
        }
        else
        {
            ghost.sprite      = GetOrCreateSquareSprite();
            ghost.sortingOrder = -1;
        }

        ghost.color = new Color(0.05f, 1f, 1f, 0.60f);
        StartCoroutine(FadeOutSprite(ghost, 0.24f));
    }

    IEnumerator FadeOutSprite(SpriteRenderer sr, float duration)
    {
        float elapsed = 0f;
        Color startColor = sr.color;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            sr.color = new Color(startColor.r, startColor.g, startColor.b,
                                  Mathf.Lerp(startColor.a, 0f, t));
            yield return null;
        }
        if (sr != null) Destroy(sr.gameObject);
    }

    // ── Sprite cuadrado de emergencia ─────────────────────────────
    static Sprite _squareSprite;
    static Sprite GetOrCreateSquareSprite()
    {
        if (_squareSprite != null) return _squareSprite;
        var tex = new Texture2D(4, 4);
        var px  = new Color[16];
        for (int i = 0; i < 16; i++) px[i] = Color.white;
        tex.SetPixels(px);
        tex.Apply();
        _squareSprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
        return _squareSprite;
    }
}
