using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Genera enemigos cuadrados en runtime al dar Play.
/// SE AUTO-INSTALA: no necesitas adjuntarlo manualmente en la escena.
/// Al presionar Play crea un GameObject "EnemySpawner" automáticamente.
/// También puedes agregarlo manualmente a cualquier objeto para configurarlo
/// desde el Inspector.
/// </summary>
public class EnemyRuntimeSpawner : MonoBehaviour
{
    [System.Serializable]
    public struct SpawnEntry
    {
        [Tooltip("Posición en el mundo donde aparecerá el enemigo")]
        public Vector2 position;
        [Tooltip("Rango de patrulla a cada lado desde su posición inicial")]
        public float patrolRange;
        [Tooltip("Variante de archivo infectado")]
        public EnemySquareAI.EnemyVariant variant;
    }

    [Header("Configuración de spawn")]
    [Tooltip("Si está activo, genera enemigos en Start. Si no, espera condición narrativa.")]
    public bool spawnOnStart = false;
    [Tooltip("Si spawnOnStart está desactivado, genera enemigos al cruzar esta X.")]
    public float spawnWhenPlayerXAtLeast = 18f;

    [Tooltip("Lista de posiciones donde se crean enemigos al iniciar")]
    public SpawnEntry[] spawnPoints = new SpawnEntry[]
    {
        new SpawnEntry { position = new Vector2( 8f, 1f),  patrolRange = 3f, variant = EnemySquareAI.EnemyVariant.BasicMelee },
        new SpawnEntry { position = new Vector2(22f, 2.1f), patrolRange = 2f, variant = EnemySquareAI.EnemyVariant.BasicMelee },
        new SpawnEntry { position = new Vector2(33f, 2.1f), patrolRange = 3f, variant = EnemySquareAI.EnemyVariant.BasicMelee },
        new SpawnEntry { position = new Vector2(48f, 3.0f), patrolRange = 3f, variant = EnemySquareAI.EnemyVariant.Runner },
        new SpawnEntry { position = new Vector2(64f, 2.3f), patrolRange = 3f, variant = EnemySquareAI.EnemyVariant.Runner },
        new SpawnEntry { position = new Vector2(82f, 3.1f), patrolRange = 4f, variant = EnemySquareAI.EnemyVariant.Tank },
        new SpawnEntry { position = new Vector2(100f, 3.4f), patrolRange = 4f, variant = EnemySquareAI.EnemyVariant.Glitch },
        new SpawnEntry { position = new Vector2(-6f, 1f),  patrolRange = 2f, variant = EnemySquareAI.EnemyVariant.BasicMelee },
    };

    [Header("Stats compartidos (aplica a todos los que se generen aquí)")]
    public int   enemyHp         = 30;
    public int   contactDamage   = 10;
    public float patrolSpeed     = 2f;
    public float chaseSpeed      = 4f;
    public float detectRange     = 6f;
    public float attackRange     = 0.6f;

    [Header("Visual")]
    public float enemySize       = 0.7f;    // tamaño del cuadrado en unidades

    // Layer "Enemy" se asigna automáticamente (si existe en el proyecto)
    int enemyLayer;
    bool hasSpawned;
    Transform player;

    // ── Auto-instalación: se crea solo al iniciar la escena ──
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void AutoInstall()
    {
        SceneManager.sceneLoaded += (scene, _) =>
        {
            if (!GameSceneConfig.IsCombatInputScene(scene.name)) return;
            if (FindAnyObjectByType<EnemyRuntimeSpawner>() != null) return;
            var go = new GameObject("EnemySpawner");
            go.AddComponent<EnemyRuntimeSpawner>();
            Debug.Log("[EnemyRuntimeSpawner] Auto-instalado en la escena.");
        };
    }

    void Start()
    {
        enemyLayer = LayerMask.NameToLayer("Enemy");
        if (enemyLayer < 0) enemyLayer = 0;   // fallback a Default

        ConfigureForCurrentScene();

        var playerObj = GameObject.FindWithTag("Player");
        if (playerObj == null)
        {
            var pc = FindAnyObjectByType<PlayerController2D>();
            if (pc != null) playerObj = pc.gameObject;
        }
        if (playerObj != null) player = playerObj.transform;

        if (spawnOnStart)
            SpawnAll();
    }

    void ConfigureForCurrentScene()
    {
        if (!GameSceneConfig.IsGameplayScene(GameSceneConfig.CurrentSceneName())) return;

        spawnOnStart = false;
        spawnWhenPlayerXAtLeast = 10f;

        spawnPoints = new SpawnEntry[]
        {
            new SpawnEntry { position = new Vector2(14f, 1.4f),  patrolRange = 3.5f, variant = EnemySquareAI.EnemyVariant.BasicMelee },
            new SpawnEntry { position = new Vector2(27f, 2.2f),  patrolRange = 2.5f, variant = EnemySquareAI.EnemyVariant.BasicMelee },
            new SpawnEntry { position = new Vector2(42f, 4.9f),  patrolRange = 2.0f, variant = EnemySquareAI.EnemyVariant.Runner },
            new SpawnEntry { position = new Vector2(56f, 0.8f),  patrolRange = 4.0f, variant = EnemySquareAI.EnemyVariant.Runner },
            new SpawnEntry { position = new Vector2(73f, 2.8f),  patrolRange = 3.0f, variant = EnemySquareAI.EnemyVariant.Tank },
            new SpawnEntry { position = new Vector2(86f, 4.6f),  patrolRange = 2.5f, variant = EnemySquareAI.EnemyVariant.Glitch },
            new SpawnEntry { position = new Vector2(111f, 3.7f), patrolRange = 2.5f, variant = EnemySquareAI.EnemyVariant.Tank },
            new SpawnEntry { position = new Vector2(125f, 6.0f), patrolRange = 3.0f, variant = EnemySquareAI.EnemyVariant.Glitch },
            new SpawnEntry { position = new Vector2(138f, 6.9f), patrolRange = 3.0f, variant = EnemySquareAI.EnemyVariant.Glitch },
            new SpawnEntry { position = new Vector2(145f, 8.4f), patrolRange = 2.2f, variant = EnemySquareAI.EnemyVariant.Runner },
        };

        enemyHp = 34;
        patrolSpeed = 2.1f;
        chaseSpeed = 4.3f;
        detectRange = 6.5f;
        attackRange = 0.65f;
    }

    void Update()
    {
        if (hasSpawned || spawnOnStart || player == null) return;
        if (player.position.x < spawnWhenPlayerXAtLeast) return;

        SpawnAll();
        Debug.Log($"[EnemyRuntimeSpawner] Enemigos activados al cruzar X={spawnWhenPlayerXAtLeast:0.##}.");
    }

    void SpawnAll()
    {
        if (hasSpawned) return;
        hasSpawned = true;

        foreach (var entry in spawnPoints)
            SpawnEnemy(entry);
    }

    void SpawnEnemy(SpawnEntry entry)
    {
        // ── 1. GameObject base ────────────────────────────────
        var go = new GameObject("EnemySquare");
        go.layer = enemyLayer;
        go.transform.position = entry.position;

        // ── 2. Sprite (cuadrado sólido) ───────────────────────
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = CreateSquareSprite();
        sr.color  = new Color(0.9f, 0.2f, 0.2f);   // rojo por defecto (patrulla)
        sr.sortingOrder = 1;

        // Escalar al tamaño deseado
        go.transform.localScale = Vector3.one * enemySize;

        // ── 3. Collider ───────────────────────────────────────
        var col = go.AddComponent<BoxCollider2D>();
        col.size = Vector2.one;     // 1x1 en espacio local → enemySize en mundo

        // ── 4. Rigidbody2D ────────────────────────────────────
        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale  = 3f;
        rb.constraints   = RigidbodyConstraints2D.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        // ── 5. IA ─────────────────────────────────────────────
        var ai = go.AddComponent<EnemySquareAI>();
        ai.maxHp        = enemyHp;
        ai.contactDamage = contactDamage;
        ai.patrolSpeed  = patrolSpeed;
        ai.chaseSpeed   = chaseSpeed;
        ai.patrolRange  = entry.patrolRange;
        ai.detectRange  = detectRange;
        ai.attackRange  = attackRange;
        ai.variant = entry.variant;
    }

    /// <summary>
    /// Crea un sprite de cuadrado blanco de 64x64 px en tiempo de ejecución.
    /// No requiere assets externos.
    /// </summary>
    static Sprite CreateSquareSprite()
    {
        const int size = 64;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;

        // Relleno completo blanco
        var pixels = new Color32[size * size];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = new Color32(255, 255, 255, 255);
        tex.SetPixels32(pixels);
        tex.Apply();

        return Sprite.Create(
            tex,
            new Rect(0, 0, size, size),
            new Vector2(0.5f, 0.5f),   // pivot centrado
            size                        // PPU = 64 → escala 1:1
        );
    }

    // ── Gizmos: ver posiciones de spawn en el editor ────────
    void OnDrawGizmos()
    {
        if (spawnPoints == null) return;
        Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.7f);
        foreach (var entry in spawnPoints)
        {
            Gizmos.DrawWireCube(entry.position, Vector3.one * 0.7f);
            Gizmos.DrawLine(
                new Vector3(entry.position.x - entry.patrolRange, entry.position.y),
                new Vector3(entry.position.x + entry.patrolRange, entry.position.y)
            );
        }
    }
}
