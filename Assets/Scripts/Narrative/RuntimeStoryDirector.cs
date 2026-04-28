using UnityEngine;

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
            title = "Código-7 // Señal Intervenida",
            body = "Interceptaste una transmisión: 'La Base Delta guarda el archivo C7'. Avanza al este y busca terminales activas."
        },
        new StoryBeat
        {
            triggerX = 30f,
            id = "beat_002",
            title = "Código-7 // Primer Hallazgo",
            body = "Entre ruinas encuentras rastros de pruebas humanas. El archivo menciona un proyecto llamado 'Espejo'."
        },
        new StoryBeat
        {
            triggerX = 60f,
            id = "beat_003",
            title = "Código-7 // Advertencia",
            body = "'Si lees esto, ya te detectaron'. La seguridad de la corporación aumenta cuanto más te acercas al núcleo."
        },
        new StoryBeat
        {
            triggerX = 95f,
            id = "beat_004",
            title = "Código-7 // Objetivo Actualizado",
            body = "Llegaste al borde del sector. Próximo objetivo: infiltrar la torre central y extraer la verdad del Proyecto Espejo."
        }
    };

    Transform player;
    string currentObjective = "Objetivo: Avanza hacia el este y recupera información.";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoInstall()
    {
        if (FindAnyObjectByType<RuntimeStoryDirector>() != null) return;
        var go = new GameObject("RuntimeStoryDirector");
        go.AddComponent<RuntimeStoryDirector>();
    }

    void Start()
    {
        var playerObj = GameObject.FindWithTag("Player");
        if (playerObj == null)
        {
            var pc = FindFirstObjectByType<PlayerController2D>();
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
        if (beatIndex < beats.Length - 1)
        {
            currentObjective = $"Objetivo: Sigue avanzando al este. Próximo punto en X={beats[beatIndex + 1].triggerX:0}.";
        }
        else
        {
            currentObjective = "Objetivo cumplido: Sector explorado. Próxima fase: torre central.";
        }

        Debug.Log("[Historia] " + currentObjective);
    }

    void OnGUI()
    {
        if (NarrativeUI.Instance != null && NarrativeUI.Instance.IsVisible) return;

        var style = new GUIStyle(GUI.skin.box)
        {
            alignment = TextAnchor.MiddleLeft,
            fontSize = 14,
            normal = { textColor = Color.white }
        };

        GUI.Box(new Rect(12, 12, 520, 36), currentObjective, style);
    }
}
