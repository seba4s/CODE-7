using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Inicia una historia base por progreso horizontal.
/// Al cruzar ciertos X, dispara beats narrativos.
/// Si hay NarrativeUI, la usa; si no, lo deja en consola.
/// </summary>
public class RuntimeStoryDirector : MonoBehaviour
{
    [System.Serializable]
    public class StoryBeat
    {
        public float triggerX;
        public string id;
        public string title;
        [TextArea(2, 6)] public string body;

        [HideInInspector] public bool fired;
    }

    [Header("Beats narrativos")]
    public StoryBeat[] beats =
    {
        new StoryBeat
        {
            triggerX = 6f,
            id = "beat_001",
            title = "Disco Duro // Diagnostico Inicial",
            body = "LUMA: Diagnostico completado. El Disco Duro esta bajo ataque. Sebastian podria perderlo todo. Cruza el pasillo de arranque y estabiliza el sector industrial."
        },
        new StoryBeat
        {
            triggerX = 42f,
            id = "beat_002",
            title = "ERASER-Omega // Proyeccion Intrusa",
            body = "ERASER-Omega: Crees que un poco de limpieza detendra el Borrado Final? Lo que se olvida, me pertenece. Cada plato que gira me entrega otro recuerdo abandonado."
        },
        new StoryBeat
        {
            triggerX = 88f,
            id = "beat_003",
            title = "LUMA // Carpeta Oculta",
            body = "LUMA detecta una firma inusual detras de las carpetas corrompidas. Hay un archivo oculto que no figura en el indice principal. Podria ser una foto clave del usuario."
        },
        new StoryBeat
        {
            triggerX = 110f,
            id = "beat_004",
            title = "Registro de Arranque // Eco Emocional",
            body = "Al insertar los datos en el Puerto de Salida se reconstruye la primera foto de Sebastian. Codigo-7 detecta un eco emocional y su visor cambia brevemente a verde antes de fijar el siguiente objetivo."
        }
    };

    Transform player;
    string currentObjective = "Objetivo: Recolecta 80 datos y activa 3 terminales de reparacion para abrir el Puerto de Salida.";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void AutoInstall()
    {
        SceneManager.sceneLoaded += (scene, _) =>
        {
            if (!GameSceneConfig.IsGameplayScene(scene.name)) return;
            if (FindAnyObjectByType<RuntimeStoryDirector>() != null) return;
            var go = new GameObject("RuntimeStoryDirector");
            go.AddComponent<RuntimeStoryDirector>();
        };
    }

    void Start()
    {
        var playerObj = GameObject.FindWithTag("Player");
        if (playerObj == null)
        {
            var pc = FindAnyObjectByType<PlayerController2D>();
            if (pc != null) playerObj = pc.gameObject;
        }

        if (playerObj != null)
        {
            player = playerObj.transform;
            Debug.Log("[Historia] " + currentObjective);
        }
        else
        {
            Debug.LogWarning("[RuntimeStoryDirector] No se encontró Player.");
        }
    }

    void Update()
    {
        if (player == null || beats == null) return;

        float x = player.position.x;
        for (int i = 0; i < beats.Length; i++)
        {
            var beat = beats[i];
            if (beat.fired) continue;
            if (x < beat.triggerX) continue;

            beat.fired = true;
            FireBeat(beat);
            UpdateObjective(i);
        }
    }

    void FireBeat(StoryBeat beat)
    {
        var entry = ScriptableObject.CreateInstance<NarrativeEntrySO>();
        entry.id = beat.id;
        entry.type = NarrativeEntryType.Chat;
        entry.title = beat.title;
        entry.body = beat.body;

        if (NarrativeUI.Instance != null)
        {
            NarrativeUI.Instance.Show(entry);
        }
        else
        {
            Debug.Log($"[Historia] {beat.title}\n{beat.body}");
        }
    }

    void UpdateObjective(int beatIndex)
    {
        switch (beatIndex)
        {
            case 0:
                currentObjective = "Objetivo: Supera la Zona A y aprende el flujo del sector mientras limpias los primeros archivos infectados.";
                break;
            case 1:
                currentObjective = "Objetivo: Activa el Terminal de Reparacion 1 y cruza los platos giratorios sin caer en sectores dañados.";
                break;
            case 2:
                currentObjective = "Objetivo: Recorre el Corredor de Carpetas Corrompidas y encuentra la carpeta oculta FOTO_ANTIGUA.png.";
                break;
            default:
                currentObjective = "Objetivo cumplido: Disco Duro estabilizado. Proximo destino: Memoria RAM.";
                break;
        }

        Debug.Log("[Historia] " + currentObjective);
    }

    void OnGUI()
    {
        if (NarrativeUI.Instance != null && NarrativeUI.Instance.IsVisible) return;

        var style = new GUIStyle(GUI.skin.box)
        {
            alignment = TextAnchor.MiddleLeft,
            fontSize = 24,
            normal = { textColor = Color.white }
        };

        style.padding = new RectOffset(18, 18, 8, 8);

        // Reservar el bloque inferior izquierdo para el HUD principal.
        float hudSafeLeft = 580f;
        float minRightMargin = 16f;

        float width = Mathf.Clamp(Screen.width - hudSafeLeft - minRightMargin, 320f, 1200f);
        float height = 56f;
        float x = Mathf.Max(hudSafeLeft, Screen.width - width - minRightMargin);
        float y = Screen.height - height - 18f;

        GUI.Box(new Rect(x, y, width, height), currentObjective, style);
    }
}
