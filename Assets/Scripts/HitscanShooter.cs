using UnityEngine;
using UnityEngine.Serialization;

public class HitscanShooter : MonoBehaviour
{
    [Header("Referencias")]
    public Transform muzzle;
    public Camera cam;
    [FormerlySerializedAs("aim")]
    public AimProvider aim;

    [Header("Stats")]
    [Tooltip("Alcance máximo del disparo en unidades")]
    public float range = 12f;
    public int damage = 10;
    public float fireRate = 10f;
    public LayerMask hitMask;
    public bool autoIncludeGroundEnemyMask = true;

    [Header("Debug")]
    public bool debugDrawRay = true;
    public float debugRayTime = 0.15f;

    [Header("Inicio")]
    public bool fireOnPlay = false;
    public float fireOnPlayDelay = 0.02f;

    [Header("Visual (opcional)")]
    [FormerlySerializedAs("line")]
    public LineRenderer shotLine;
    [FormerlySerializedAs("lineTime")]
    public float lineVisibleTime = 0.05f;
    public float lineWidth = 0.06f;
    public Color lineColor = new Color(1f, 0.9f, 0.2f, 1f);

    /// <summary>Se invoca al disparar. Paramámetros: posición del cañón, dirección del disparo.</summary>
    public System.Action<Vector2, Vector2> OnShot;

    float nextFireTime;
    Coroutine lineRoutine;

    void Awake()
    {
        if (cam == null) cam = Camera.main;
        if (aim == null) aim = GetComponent<AimProvider>();
        if (autoIncludeGroundEnemyMask) ApplyDefaultHitMaskLayers();
        if (shotLine == null) shotLine = GetComponentInChildren<LineRenderer>(true);
        if (shotLine == null) shotLine = CreateRuntimeShotLine();
        if (shotLine != null && shotLine.positionCount < 2) shotLine.positionCount = 2;
        ConfigureShotLine(shotLine);
    }

    void Start()
    {
        if (fireOnPlay)
            StartCoroutine(FireOnStartRoutine());
    }

    System.Collections.IEnumerator FireOnStartRoutine()
    {
        if (fireOnPlayDelay > 0f)
            yield return new WaitForSeconds(fireOnPlayDelay);

        TryShoot(ignoreCooldown: true);
    }

    void Update()
    {
        // No disparar si el juego está pausado por NarrativeUI
        if (NarrativeUI.IsGamePaused) return;

        // Disparo con click izquierdo
        if (Input.GetMouseButtonDown(0))
        {
            TryShoot();
        }
    }

    void TryShoot(bool ignoreCooldown = false)
    {
        // Cooldown
        if (!ignoreCooldown && Time.time < nextFireTime) return;
        nextFireTime = Time.time + 1f / Mathf.Max(fireRate, 0.1f);

        // Origen
        Vector2 origin = muzzle != null
            ? (Vector2)muzzle.position
            : (Vector2)transform.position;

        // Dirección hacia el mouse (con fallback robusto)
        Vector2 dir = GetShootDirection(origin);

        Debug.Log($"[Hitscan] origen={origin} dir={dir} range={range} mask={hitMask.value}");
        if (debugDrawRay) Debug.DrawRay(origin, dir * range, Color.cyan, debugRayTime);

                ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(hitMask);
        filter.useTriggers = false;

        RaycastHit2D[] results = new RaycastHit2D[10];
        int count = Physics2D.Raycast(origin, dir, filter, results, range);

        RaycastHit2D hit = default;
        for (int i = 0; i < count; i++)
        {
            if (results[i].collider.transform.root != transform.root)
            {
                hit = results[i];
                break;
            }
        }

        Vector2 endPoint = origin + dir * range;

        if (hit.collider != null)
        {
            endPoint = hit.point;
            Debug.Log($"[Hitscan] HIT: {hit.collider.name} en {hit.point}");

            IDamageable damageable = hit.collider.GetComponent<IDamageable>();
            if (damageable != null)
                damageable.TakeDamage(damage, hit.point, hit.normal);
        }
        else
        {
            RaycastHit2D[] allHits = Physics2D.RaycastAll(origin, dir, range);
            RaycastHit2D blockedByMask = default;

            for (int i = 0; i < allHits.Length; i++)
            {
                if (allHits[i].collider == null) continue;
                if (allHits[i].collider.transform.root == transform.root) continue;

                int otherLayer = allHits[i].collider.gameObject.layer;
                bool isInMask = (hitMask.value & (1 << otherLayer)) != 0;
                if (!isInMask)
                {
                    blockedByMask = allHits[i];
                    break;
                }
            }

            if (blockedByMask.collider != null)
            {
                int layer = blockedByMask.collider.gameObject.layer;
                string layerName = LayerMask.LayerToName(layer);
                Debug.Log($"[Hitscan] NO HIT en mask. Collider detectado fuera de máscara: {blockedByMask.collider.name} (layer {layer}: {layerName}). hitMask={hitMask.value}");
            }
            else
            {
                Debug.Log($"[Hitscan] NO HIT real: no hay colliders en la dirección/rango. hitMask={hitMask.value}");
            }
        }

        // LineRenderer opcional
        if (shotLine != null)
        {
            if (lineRoutine != null) StopCoroutine(lineRoutine);
            lineRoutine = StartCoroutine(ShowLine(origin, endPoint));
        }

        // Notificar a suscriptores (ej: PlayerVFX para shake/flash)
        OnShot?.Invoke(origin, dir);
    }

    Vector2 GetShootDirection(Vector2 origin)
    {
        if (cam != null)
        {
            Vector3 mouse = Input.mousePosition;
            float targetZ = muzzle != null ? muzzle.position.z : transform.position.z;
            mouse.z = Mathf.Abs(cam.transform.position.z - targetZ);
            Vector3 world = cam.ScreenToWorldPoint(mouse);
            Vector2 camDir = (Vector2)world - origin;
            if (camDir.sqrMagnitude > 0.0001f)
                return camDir.normalized;
        }

        // Fallback solo si no hay cam o el mouse coincide con el origen.
        if (aim != null && aim.AimDirection.sqrMagnitude > 0.0001f)
            return aim.AimDirection.normalized;

        float xSign = transform.lossyScale.x >= 0f ? 1f : -1f;
        return new Vector2(xSign, 0f);
    }

    void ApplyDefaultHitMaskLayers()
    {
        int resolvedMask = hitMask.value;

        int groundLayer = LayerMask.NameToLayer("Ground");
        if (groundLayer >= 0) resolvedMask |= (1 << groundLayer);

        int enemyLayer = LayerMask.NameToLayer("Enemy");
        if (enemyLayer >= 0) resolvedMask |= (1 << enemyLayer);

        hitMask = resolvedMask;
    }

    System.Collections.IEnumerator ShowLine(Vector2 from, Vector2 to)
    {
        if (shotLine == null) yield break;
        shotLine.enabled = true;
        shotLine.SetPosition(0, from);
        shotLine.SetPosition(1, to);
        yield return new WaitForSeconds(lineVisibleTime);
        shotLine.enabled = false;
        lineRoutine = null;
    }

    LineRenderer CreateRuntimeShotLine()
    {
        var go = new GameObject("ShotLine");
        go.transform.SetParent(transform, false);
        var lr = go.AddComponent<LineRenderer>();
        return lr;
    }

    void ConfigureShotLine(LineRenderer lr)
    {
        if (lr == null) return;

        lr.useWorldSpace = true;
        lr.textureMode = LineTextureMode.Stretch;
        lr.alignment = LineAlignment.View;
        lr.positionCount = Mathf.Max(2, lr.positionCount);
        lr.startWidth = lineWidth;
        lr.endWidth = lineWidth;
        lr.startColor = lineColor;
        lr.endColor = lineColor;
        lr.sortingOrder = Mathf.Max(lr.sortingOrder, 20);

        if (lr.material == null)
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null) lr.material = new Material(shader);
        }

        lr.enabled = false;
    }
}