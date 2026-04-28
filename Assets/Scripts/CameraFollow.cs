using UnityEngine;

/// <summary>
/// Cámara que sigue al jugador con suavizado.
/// SE AUTO-INSTALA: no necesitas agregarla manualmente.
/// Se adjunta automáticamente a la Main Camera al dar Play.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Header("Objetivo")]
    public Transform target;

    [Header("Suavizado")]
    [Tooltip("Qué tan rápido sigue la cámara al jugador. Más alto = más rápido.")]
    public float smoothSpeed = 6f;

    [Header("Offset")]
    [Tooltip("Desplazamiento de la cámara respecto al jugador.")]
    public Vector3 offset = new Vector3(0f, 1.5f, 0f);

    [Header("Límites de cámara (opcional)")]
    public bool  useBounds  = false;
    public float minX = -100f, maxX = 100f;
    public float minY = -100f, maxY = 100f;

    // ── Auto-instalación ────────────────────────────────────
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoInstall()
    {
        var cam = Camera.main;
        if (cam == null) return;

        // Si ya tiene CameraFollow, no hacer nada
        if (cam.GetComponent<CameraFollow>() != null) return;

        var cf = cam.gameObject.AddComponent<CameraFollow>();

        // Buscar jugador
        var playerObj = GameObject.FindWithTag("Player");
        if (playerObj == null)
        {
            var pc = FindAnyObjectByType<PlayerController2D>();
            if (pc != null) playerObj = pc.gameObject;
        }

        if (playerObj != null)
        {
            cf.target = playerObj.transform;
            Debug.Log("[CameraFollow] Auto-instalado siguiendo a: " + playerObj.name);
        }
        else
        {
            Debug.LogWarning("[CameraFollow] No se encontró el jugador para seguir.");
        }
    }

    // ── Shake ────────────────────────────────────────────────────
    float shakeAmplitude;
    float shakeEndTime;

    /// <summary>Inicia una sacudida de cámara. Llamar desde PlayerVFX.</summary>
    public void Shake(float amplitude, float duration)
    {
        shakeAmplitude = amplitude;
        shakeEndTime   = Time.time + duration;
    }

    // ────────────────────────────────────────────────────────
    void LateUpdate()
    {
        if (target == null) return;

        // Si el juego está pausado por NarrativeUI, no mover la cámara
        if (NarrativeUI.IsGamePaused) return;

        Vector3 desired = target.position + offset;
        desired.z = transform.position.z;   // mantener profundidad de cámara

        if (useBounds)
        {
            desired.x = Mathf.Clamp(desired.x, minX, maxX);
            desired.y = Mathf.Clamp(desired.y, minY, maxY);
        }

        // Shake offset
        if (Time.time < shakeEndTime)
        {
            desired += (Vector3)Random.insideUnitCircle * shakeAmplitude;
        }

        transform.position = Vector3.Lerp(transform.position, desired, smoothSpeed * Time.deltaTime);
    }
}
