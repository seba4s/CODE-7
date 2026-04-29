using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider2D))]
public class Level1DataFragment : MonoBehaviour
{
    public int amount = 4;
    public bool isHiddenFolder;
    public string pickupTitle = "Fragmento de Datos";
    [TextArea(2, 4)] public string pickupBody = "Integridad del sistema recuperada.";

    bool collected;

    void Reset()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (collected) return;
        if (other.GetComponent<PlayerController2D>() == null && other.GetComponentInParent<PlayerController2D>() == null) return;
        if (Level1MissionDirector.Instance == null) return;

        collected = true;
        Level1MissionDirector.Instance.AddData(amount);
        if (isHiddenFolder)
            Level1MissionDirector.Instance.MarkHiddenFolderFound();

        Level1MissionDirector.Instance.ShowLevelMessage(pickupTitle, pickupBody);
        Destroy(gameObject);
    }
}

[RequireComponent(typeof(Collider2D))]
public class Level1RepairTerminal : MonoBehaviour
{
    public string terminalName = "Terminal de Reparacion";
    [TextArea(2, 4)] public string activateBody = "Canal de reparacion restablecido.";

    bool playerInside;
    bool activated;
    SpriteRenderer sr;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    void Reset()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    void Update()
    {
        if (!playerInside || activated) return;
        if (!Input.GetKeyDown(KeyCode.E)) return;
        if (Level1MissionDirector.Instance == null) return;

        activated = true;
        if (sr != null) sr.color = new Color(0.15f, 1f, 0.72f, 1f);

        int current = Level1MissionDirector.Instance.ActivateTerminal();
        Level1MissionDirector.Instance.ShowLevelMessage(
            terminalName,
            $"Terminal {current}/{Level1MissionDirector.RequiredTerminals} en linea. {activateBody}");
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<PlayerController2D>() == null && other.GetComponentInParent<PlayerController2D>() == null) return;
        playerInside = true;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.GetComponent<PlayerController2D>() == null && other.GetComponentInParent<PlayerController2D>() == null) return;
        playerInside = false;
    }
}

[RequireComponent(typeof(Collider2D))]
public class Level1ExitPort : MonoBehaviour
{
    bool playerInside;
    bool completed;

    void Reset()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    void Update()
    {
        if (!playerInside || completed) return;
        if (!Input.GetKeyDown(KeyCode.E)) return;
        if (Level1MissionDirector.Instance == null) return;

        if (!Level1MissionDirector.Instance.ExitUnlocked)
        {
            Level1MissionDirector.Instance.ShowLevelMessage(
                "Puerto de Salida",
                "Bloqueado. Recupera 80 datos y activa las 3 terminales de reparacion.");
            return;
        }

        completed = true;
        PlayerPrefs.SetInt("Level1_Completed", 1);
        PlayerPrefs.Save();

        Level1MissionDirector.Instance.ShowLevelMessage(
            "Primer Formateo",
            "Sector del Disco Duro asegurado. El Registro de Arranque ha sido recuperado.");

        Level1MissionDirector.Instance.CompleteLevelAndOpenTransition();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<PlayerController2D>() == null && other.GetComponentInParent<PlayerController2D>() == null) return;
        playerInside = true;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.GetComponent<PlayerController2D>() == null && other.GetComponentInParent<PlayerController2D>() == null) return;
        playerInside = false;
    }
}

[RequireComponent(typeof(Collider2D))]
public class DamageSectorHazard : MonoBehaviour
{
    public int damagePerTick = 8;
    public float tickInterval = 0.6f;

    float nextTickTime;

    void OnTriggerStay2D(Collider2D other)
    {
        if (Time.time < nextTickTime) return;

        var health = other.GetComponent<PlayerHealth>() ?? other.GetComponentInParent<PlayerHealth>();
        if (health == null) return;

        health.TakeDamage(damagePerTick, transform.position, Vector2.up);
        nextTickTime = Time.time + tickInterval;
    }
}

[RequireComponent(typeof(BoxCollider2D))]
public class RotatingPlatform : MonoBehaviour
{
    public float speed = 45f;

    void Awake()
    {
        var rb = GetComponent<Rigidbody2D>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.simulated = true;
        rb.gravityScale = 0f;
    }

    void Update()
    {
        transform.Rotate(0f, 0f, speed * Time.deltaTime);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        var player = collision.gameObject.GetComponent<PlayerController2D>() ?? collision.gameObject.GetComponentInParent<PlayerController2D>();
        if (player == null) return;
        player.transform.SetParent(transform, true);
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        var player = collision.gameObject.GetComponent<PlayerController2D>() ?? collision.gameObject.GetComponentInParent<PlayerController2D>();
        if (player == null) return;
        if (player.transform.parent == transform) player.transform.SetParent(null, true);
    }
}