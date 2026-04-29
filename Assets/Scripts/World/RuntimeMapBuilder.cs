using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Construye un tramo jugable hacia la derecha en tiempo de ejecución:
/// suelo largo, bases y plataformas para saltar.
/// </summary>
public class RuntimeMapBuilder : MonoBehaviour
{
    [Header("Activación")]
    public bool buildOnStart = true;

    [Header("Capas y visual")]
    public string groundLayerName = "Ground";
    public Color groundColor = new Color(0.15f, 0.18f, 0.23f);
    public Color platformColor = new Color(0.23f, 0.28f, 0.35f);

    [Header("Límites de generación")]
    public float startX = -12f;
    public float endX = 120f;
    public float groundY = -1.8f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void AutoInstall()
    {
        SceneManager.sceneLoaded += (scene, _) =>
        {
            if (!GameSceneConfig.IsCombatInputScene(scene.name)) return;
            if (FindAnyObjectByType<RuntimeMapBuilder>() != null) return;
            var go = new GameObject("RuntimeMapBuilder");
            go.AddComponent<RuntimeMapBuilder>();
            Debug.Log("[RuntimeMapBuilder] Auto-instalado.");
        };
    }

    void Start()
    {
        if (!buildOnStart) return;
        Build();
    }

    public void Build()
    {
        var existing = GameObject.Find("RuntimeMap");
        if (existing != null) return;

        var root = new GameObject("RuntimeMap");
        int groundLayer = LayerMask.NameToLayer(groundLayerName);
        if (groundLayer < 0) groundLayer = 0;

        if (GameSceneConfig.IsGameplayScene(GameSceneConfig.CurrentSceneName()))
        {
            BuildHardDriveSector(root.transform, groundLayer);
            Debug.Log("[RuntimeMapBuilder] Nivel 1 creado: Disco Duro.");
            return;
        }

        BuildTutorialSlice(root.transform, groundLayer);
        Debug.Log("[RuntimeMapBuilder] Mapa de tutorial creado.");
    }

    void BuildTutorialSlice(Transform root, int groundLayer)
    {
        // Suelo principal continuo
        CreateBlock(root, "MainGround", new Vector2((startX + endX) * 0.5f, groundY - 0.6f), new Vector2(endX - startX, 2.4f), groundLayer, groundColor, 0);

        // Bases (pequeñas plataformas grandes)
        CreateBlock(root, "Base_A", new Vector2(10f, 0.4f), new Vector2(8f, 0.8f), groundLayer, platformColor, 1);
        CreateBlock(root, "Base_B", new Vector2(28f, 1.2f), new Vector2(7f, 0.8f), groundLayer, platformColor, 1);
        CreateBlock(root, "Base_C", new Vector2(46f, 2.1f), new Vector2(9f, 0.8f), groundLayer, platformColor, 1);
        CreateBlock(root, "Base_D", new Vector2(70f, 1.5f), new Vector2(10f, 0.8f), groundLayer, platformColor, 1);
        CreateBlock(root, "Base_E", new Vector2(96f, 2.6f), new Vector2(12f, 0.8f), groundLayer, platformColor, 1);

        // Ruta de salto progresiva a la derecha
        CreateBlock(root, "Jump_01", new Vector2(16f, 2.6f), new Vector2(3.2f, 0.55f), groundLayer, platformColor, 2);
        CreateBlock(root, "Jump_02", new Vector2(21f, 3.8f), new Vector2(3.2f, 0.55f), groundLayer, platformColor, 2);
        CreateBlock(root, "Jump_03", new Vector2(26f, 5.0f), new Vector2(3.2f, 0.55f), groundLayer, platformColor, 2);
        CreateBlock(root, "Jump_04", new Vector2(35f, 3.4f), new Vector2(3.5f, 0.55f), groundLayer, platformColor, 2);
        CreateBlock(root, "Jump_05", new Vector2(40f, 4.7f), new Vector2(3.5f, 0.55f), groundLayer, platformColor, 2);
        CreateBlock(root, "Jump_06", new Vector2(54f, 5.6f), new Vector2(4f, 0.55f), groundLayer, platformColor, 2);
        CreateBlock(root, "Jump_07", new Vector2(61f, 4.1f), new Vector2(4f, 0.55f), groundLayer, platformColor, 2);
        CreateBlock(root, "Jump_08", new Vector2(77f, 3.6f), new Vector2(4.2f, 0.55f), groundLayer, platformColor, 2);
        CreateBlock(root, "Jump_09", new Vector2(85f, 5.0f), new Vector2(4.2f, 0.55f), groundLayer, platformColor, 2);
        CreateBlock(root, "Jump_10", new Vector2(107f, 4.2f), new Vector2(4.5f, 0.55f), groundLayer, platformColor, 2);

        // Estructuras verticales (sensación de base/ruina)
        CreateBlock(root, "Tower_A", new Vector2(32f, 0.8f), new Vector2(1.3f, 4f), groundLayer, groundColor, 0);
        CreateBlock(root, "Tower_B", new Vector2(58f, 0.9f), new Vector2(1.3f, 5f), groundLayer, groundColor, 0);
        CreateBlock(root, "Tower_C", new Vector2(90f, 1.0f), new Vector2(1.3f, 6f), groundLayer, groundColor, 0);
    }

    void BuildHardDriveSector(Transform root, int groundLayer)
    {
        Color hddGround = new Color(0.22f, 0.26f, 0.31f);
        Color hddPlatform = new Color(0.33f, 0.42f, 0.5f);
        Color dataRail = new Color(0.18f, 0.9f, 0.75f);
        Color steelDark = new Color(0.12f, 0.16f, 0.2f);
        Color hazardColor = new Color(1f, 0.2f, 0.2f, 0.55f);

        CreateParallaxBackdrop(root);

        CreateBlock(root, "StorageFloor", new Vector2(64f, -2.6f), new Vector2(168f, 2.8f), groundLayer, hddGround, 0);

        // Distrito de entrada / arranque
        CreateBlock(root, "BootPlatform", new Vector2(-8f, -0.2f), new Vector2(12f, 0.9f), groundLayer, hddPlatform, 1);
        CreateBlock(root, "BootBridge", new Vector2(5f, 0.4f), new Vector2(10f, 0.65f), groundLayer, dataRail, 2);
        CreateBlock(root, "StorageShelf_A", new Vector2(18f, 1.2f), new Vector2(11f, 0.85f), groundLayer, hddPlatform, 1);
        CreateBlock(root, "StorageShelf_B", new Vector2(31f, 2.6f), new Vector2(9f, 0.85f), groundLayer, hddPlatform, 1);
        CreateBlock(root, "StorageShelf_C", new Vector2(42f, 4.3f), new Vector2(8f, 0.7f), groundLayer, dataRail, 2);

        // Platos magneticos / ruta media
        CreateBlock(root, "Platter_A", new Vector2(56f, -0.1f), new Vector2(14f, 0.9f), groundLayer, hddPlatform, 1);
        CreateBlock(root, "Platter_B", new Vector2(72f, 1.9f), new Vector2(12f, 0.8f), groundLayer, dataRail, 2);
        CreateBlock(root, "Platter_C", new Vector2(86f, 3.7f), new Vector2(10f, 0.7f), groundLayer, hddPlatform, 1);
        CreateBlock(root, "HeadRail", new Vector2(98f, 1.1f), new Vector2(8f, 0.65f), groundLayer, dataRail, 2);

        var rotatingA = CreateBlock(root, "RotatingPlatter_A", new Vector2(63f, 3.7f), new Vector2(8f, 0.7f), groundLayer, dataRail, 3);
        rotatingA.AddComponent<RotatingPlatform>().speed = 38f;
        var rotatingB = CreateBlock(root, "RotatingPlatter_B", new Vector2(79f, 5.6f), new Vector2(7.5f, 0.7f), groundLayer, hddPlatform, 3);
        rotatingB.AddComponent<RotatingPlatform>().speed = -46f;

        // Tramo final hacia el archivo del nivel
        CreateBlock(root, "ArchiveBase_A", new Vector2(111f, 2.8f), new Vector2(10f, 0.8f), groundLayer, hddPlatform, 1);
        CreateBlock(root, "ArchiveBase_B", new Vector2(124f, 4.6f), new Vector2(9f, 0.75f), groundLayer, dataRail, 2);
        CreateBlock(root, "ArchiveBase_C", new Vector2(138f, 6.0f), new Vector2(12f, 0.9f), groundLayer, hddPlatform, 1);

        // Torres / racks de almacenamiento
        CreateBlock(root, "Rack_A", new Vector2(12f, 0.2f), new Vector2(1.6f, 4.6f), groundLayer, steelDark, 0);
        CreateBlock(root, "Rack_B", new Vector2(36f, 0.5f), new Vector2(1.8f, 6.6f), groundLayer, steelDark, 0);
        CreateBlock(root, "Rack_C", new Vector2(64f, 0.9f), new Vector2(1.8f, 7.4f), groundLayer, steelDark, 0);
        CreateBlock(root, "Rack_D", new Vector2(93f, 0.2f), new Vector2(1.8f, 5.8f), groundLayer, steelDark, 0);
        CreateBlock(root, "Rack_E", new Vector2(131f, 1.2f), new Vector2(1.8f, 8.4f), groundLayer, steelDark, 0);

        // Barreras laterales para dar sensación industrial
        CreateBlock(root, "Bulkhead_A", new Vector2(24f, -1.1f), new Vector2(4f, 2.0f), groundLayer, steelDark, 0);
        CreateBlock(root, "Bulkhead_B", new Vector2(79f, -1.1f), new Vector2(4f, 2.0f), groundLayer, steelDark, 0);
        CreateBlock(root, "Bulkhead_C", new Vector2(118f, -1.1f), new Vector2(4f, 2.0f), groundLayer, steelDark, 0);

        CreateFolderGate(root, new Vector2(50f, -0.5f), "/Documentos", new Color(0.38f, 0.78f, 0.95f));
        CreateFolderGate(root, new Vector2(103f, -0.5f), "/Fotos", new Color(0.35f, 0.92f, 0.8f));
        CreateFolderGate(root, new Vector2(148f, 6.7f), "/Proyectos", new Color(0.65f, 0.9f, 1f));

        // Sectores dañados
        CreateHazard(root, "BadSector_A", new Vector2(48f, -1.95f), new Vector2(9f, 0.55f), hazardColor, 9, 0.55f);
        CreateHazard(root, "BadSector_B", new Vector2(91f, -1.95f), new Vector2(10f, 0.55f), hazardColor, 9, 0.55f);

        // Terminales de reparación
        CreateTerminal(root, "Terminal_01", new Vector2(20f, 2.3f), "Terminal de Reparacion /Documentos", "Compuerta /Fotos parcialmente desbloqueada.");
        CreateTerminal(root, "Terminal_02", new Vector2(76f, 3.0f), "Terminal de Reparacion /Fotos", "Ruta hacia /Proyectos restaurada.");
        CreateTerminal(root, "Terminal_03", new Vector2(132f, 7.4f), "Terminal de Reparacion /Proyectos", "Puerto de salida listo para sincronizacion.");

        // Fragmentos de datos (20 x 4 = 80)
        CreateDataFragment(root, new Vector2(-4f, 1.2f),  false, "Fragmento de Integridad", "Datos del arranque estabilizados.");
        CreateDataFragment(root, new Vector2(2f, 1.7f),   false, "Fragmento de Integridad", "Paquete de diagnostico recuperado.");
        CreateDataFragment(root, new Vector2(10f, 2.0f),  false, "Fragmento de Integridad", "Indices del sistema restaurados.");
        CreateDataFragment(root, new Vector2(18f, 2.9f),  false, "Fragmento de Integridad", "Entrada de carpeta recuperada.");
        CreateDataFragment(root, new Vector2(27f, 4.1f),  false, "Fragmento de Integridad", "Datos de acceso preservados.");
        CreateDataFragment(root, new Vector2(33f, 4.9f),  false, "Fragmento de Integridad", "Checksum parcial encontrado.");
        CreateDataFragment(root, new Vector2(43f, 5.6f),  false, "Fragmento de Integridad", "Bloque estable rescatado.");
        CreateDataFragment(root, new Vector2(55f, 1.6f),  false, "Fragmento de Integridad", "Sector magnetico sincronizado.");
        CreateDataFragment(root, new Vector2(61f, 5.0f),  false, "Fragmento de Integridad", "Ruta de plato reparada.");
        CreateDataFragment(root, new Vector2(69f, 3.2f),  false, "Fragmento de Integridad", "Bits de memoria fria recuperados.");
        CreateDataFragment(root, new Vector2(74f, 6.8f),  false, "Fragmento de Integridad", "Rastro de fotos digitales detectado.");
        CreateDataFragment(root, new Vector2(81f, 6.9f),  false, "Fragmento de Integridad", "Estructura de carpetas recompuesta.");
        CreateDataFragment(root, new Vector2(88f, 5.0f),  false, "Fragmento de Integridad", "Mapa de sectores saneado.");
        CreateDataFragment(root, new Vector2(97f, 2.2f),  false, "Fragmento de Integridad", "Canal de cabezal restablecido.");
        CreateDataFragment(root, new Vector2(106f, 3.8f), false, "Fragmento de Integridad", "Cluster seguro restaurado.");
        CreateDataFragment(root, new Vector2(114f, 4.3f), false, "Fragmento de Integridad", "Bloque critico preservado.");
        CreateDataFragment(root, new Vector2(122f, 5.8f), false, "Fragmento de Integridad", "Respaldo parcial encontrado.");
        CreateDataFragment(root, new Vector2(130f, 8.2f), false, "Fragmento de Integridad", "Indice de proyecto recompuesto.");
        CreateDataFragment(root, new Vector2(138f, 8.9f), false, "Fragmento de Integridad", "Cabecera del registro recuperada.");
        CreateDataFragment(root, new Vector2(146f, 7.2f), true,  "FOTO_ANTIGUA.png", "Carpeta oculta encontrada. LUMA detecta un eco emocional inusual en la primera foto de Sebastian.");

        // Puerto de salida del nivel
        CreateExitPort(root, new Vector2(151f, 8.2f));

        // Señales de carpetas
        CreateWorldLabel(root, "/Documentos", new Vector2(16f, 4.8f), new Color(0.6f, 0.95f, 1f));
        CreateWorldLabel(root, "/Fotos", new Vector2(70f, 8.4f), new Color(0.55f, 1f, 0.75f));
        CreateWorldLabel(root, "/Proyectos", new Vector2(128f, 10.4f), new Color(0.7f, 0.95f, 1f));
    }

    static GameObject CreateBlock(Transform parent, string name, Vector2 position, Vector2 size, int layer, Color color, int sortingOrder)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.position = position;
        go.transform.localScale = size;
        go.layer = layer;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = CreateSquareSprite();
        sr.color = color;
        sr.sortingOrder = sortingOrder;

        var col = go.AddComponent<BoxCollider2D>();
        col.size = Vector2.one;

        return go;
    }

    static void CreateDataFragment(Transform parent, Vector2 position, bool hiddenFolder, string title, string body)
    {
        var go = new GameObject(hiddenFolder ? "HiddenFolder" : "DataFragment");
        go.transform.SetParent(parent);
        go.transform.position = position;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = CreateSquareSprite();
        sr.color = hiddenFolder ? new Color(1f, 0.92f, 0.25f, 1f) : new Color(0.2f, 1f, 0.85f, 1f);
        sr.sortingOrder = 5;
        go.transform.localScale = hiddenFolder ? new Vector3(0.7f, 0.7f, 1f) : new Vector3(0.45f, 0.45f, 1f);

        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.5f;

        var fragment = go.AddComponent<Level1DataFragment>();
        fragment.amount = 4;
        fragment.isHiddenFolder = hiddenFolder;
        fragment.pickupTitle = title;
        fragment.pickupBody = body;
    }

    static void CreateTerminal(Transform parent, string name, Vector2 position, string title, string body)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.position = position;
        go.transform.localScale = new Vector3(1.2f, 2.2f, 1f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = CreateSquareSprite();
        sr.color = new Color(0.18f, 0.34f, 0.45f, 1f);
        sr.sortingOrder = 4;

        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(1.2f, 1.2f);

        var terminal = go.AddComponent<Level1RepairTerminal>();
        terminal.terminalName = title;
        terminal.activateBody = body;

        CreateWorldLabel(go.transform, "[E]", new Vector2(0f, 1.8f), new Color(0.3f, 1f, 0.85f));
    }

    static void CreateExitPort(Transform parent, Vector2 position)
    {
        var go = new GameObject("ExitPort");
        go.transform.SetParent(parent);
        go.transform.position = position;
        go.transform.localScale = new Vector3(2.4f, 3.2f, 1f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = CreateSquareSprite();
        sr.color = new Color(0.14f, 0.45f, 0.62f, 1f);
        sr.sortingOrder = 4;

        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(1f, 1f);

        go.AddComponent<Level1ExitPort>();
        CreateWorldLabel(go.transform, "PUERTO DE SALIDA\n[E]", new Vector2(0f, 2.6f), new Color(0.8f, 1f, 1f));
    }

    static void CreateHazard(Transform parent, string name, Vector2 position, Vector2 size, Color color, int damagePerTick, float tickInterval)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.position = position;
        go.transform.localScale = size;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = CreateSquareSprite();
        sr.color = color;
        sr.sortingOrder = 2;

        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;

        var hazard = go.AddComponent<DamageSectorHazard>();
        hazard.damagePerTick = damagePerTick;
        hazard.tickInterval = tickInterval;
    }

    static void CreateWorldLabel(Transform parent, string text, Vector2 localPosition, Color color)
    {
        var go = new GameObject("Label");
        go.transform.SetParent(parent);
        go.transform.localPosition = new Vector3(localPosition.x, localPosition.y, 0f);

        var tm = go.AddComponent<TextMesh>();
        tm.text = text;
        tm.fontSize = 44;
        tm.characterSize = 0.12f;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = color;
    }

    static void CreateParallaxBackdrop(Transform parent)
    {
        var far = CreateParallaxLayer(parent, "BG_Far", new Vector2(80f, 18f), new Vector2(260f, 50f), new Color(0.06f, 0.08f, 0.13f), -30, 0.1f);
        CreateParallaxLayer(parent, "BG_Mid", new Vector2(85f, 12f), new Vector2(220f, 35f), new Color(0.1f, 0.14f, 0.2f), -20, 0.2f);
        CreateParallaxLayer(parent, "BG_Data", new Vector2(90f, 7f), new Vector2(200f, 16f), new Color(0.12f, 0.32f, 0.38f, 0.55f), -10, 0.35f);

        CreateWorldLabel(far.transform, "PLATOS MAGNETICOS // DISTRITO INDUSTRIAL", new Vector2(0f, 6f), new Color(0.48f, 0.8f, 1f, 0.85f));
    }

    static GameObject CreateParallaxLayer(Transform parent, string name, Vector2 position, Vector2 size, Color color, int sortingOrder, float factor)
    {
        var go = CreateBlock(parent, name, position, size, 0, color, sortingOrder);
        var col = go.GetComponent<BoxCollider2D>();
        if (col != null) Object.Destroy(col);

        var parallax = go.AddComponent<SimpleParallaxLayer>();
        parallax.factor = factor;
        return go;
    }

    static void CreateFolderGate(Transform parent, Vector2 position, string folderName, Color tint)
    {
        var frame = CreateBlock(parent, "GateFrame_" + folderName, position, new Vector2(1.6f, 8.8f), 0, new Color(0.08f, 0.12f, 0.17f), 3);
        var col = frame.GetComponent<BoxCollider2D>();
        if (col != null) Object.Destroy(col);

        var panelA = CreateBlock(parent, "GatePanelA_" + folderName, position + new Vector2(-1.3f, 0f), new Vector2(1.2f, 5.4f), 0, tint, 3);
        var panelB = CreateBlock(parent, "GatePanelB_" + folderName, position + new Vector2(1.3f, 0f), new Vector2(1.2f, 5.4f), 0, tint, 3);
        var colA = panelA.GetComponent<BoxCollider2D>();
        if (colA != null) Object.Destroy(colA);
        var colB = panelB.GetComponent<BoxCollider2D>();
        if (colB != null) Object.Destroy(colB);

        CreateWorldLabel(frame.transform, folderName, new Vector2(0f, 4.8f), Color.white);
    }

    static Sprite CreateSquareSprite()
    {
        const int size = 32;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;

        var pixels = new Color32[size * size];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color32(255, 255, 255, 255);
        tex.SetPixels32(pixels);
        tex.Apply();

        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }
}

public class SimpleParallaxLayer : MonoBehaviour
{
    public float factor = 0.2f;

    Transform cam;
    Vector3 initialPos;
    float camStartX;

    void Start()
    {
        cam = Camera.main != null ? Camera.main.transform : null;
        initialPos = transform.position;
        camStartX = cam != null ? cam.position.x : 0f;
    }

    void LateUpdate()
    {
        if (cam == null)
        {
            if (Camera.main == null) return;
            cam = Camera.main.transform;
            camStartX = cam.position.x;
        }

        float deltaX = cam.position.x - camStartX;
        transform.position = new Vector3(initialPos.x + deltaX * factor, initialPos.y, initialPos.z);
    }
}
