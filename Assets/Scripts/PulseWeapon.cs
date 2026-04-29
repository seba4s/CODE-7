using System.Collections;
using UnityEngine;

/// <summary>
/// Arma secundaria: mantén CLIC DERECHO para cargar, suelta para disparar.
/// Más carga = más daño y más rango. Cooldown de 8 segundos.
/// </summary>
public class PulseWeapon : MonoBehaviour
{
    [Header("Stats")]
    public float maxChargeTime  = 2f;
    public int   minDamage      = 20;
    public int   maxDamage      = 80;
    public float minRange       = 3f;
    public float maxRange       = 9f;
    public float cooldown       = 8f;

    [Header("Visual")]
    public Color pulseColor = new Color(0.3f, 1f, 0.9f);

    public bool  IsOnCooldown  => Time.time < _nextReadyTime;
    public float ChargeRatio   => _chargeRatio;
    public bool  IsCharging    => _isCharging;
    public float CooldownRatio => Mathf.Clamp01(1f - ((_nextReadyTime - Time.time) / cooldown));

    public event System.Action OnPulseFired;

    float        _nextReadyTime;
    float        _chargeStart;
    float        _chargeRatio;
    bool         _isCharging;

    Camera       _cam;
    AimProvider  _aim;
    LineRenderer _line;

    void Awake()
    {
        _cam  = Camera.main;
        _aim  = GetComponent<AimProvider>();
        _line = CreatePulseLine();
    }

    void Update()
    {
        if (!GameSceneConfig.IsCombatInputScene(GameSceneConfig.CurrentSceneName())) return;

        if (!_isCharging && !IsOnCooldown && GameInput.GetSecondaryFireHeld())
        {
            _isCharging  = true;
            _chargeStart = Time.time;
        }

        if (_isCharging)
        {
            _chargeRatio = Mathf.Clamp01((Time.time - _chargeStart) / maxChargeTime);

            if (GameInput.GetSecondaryFireUp())
            {
                Fire();
                _isCharging  = false;
                _chargeRatio = 0f;
            }
        }
    }

    void Fire()
    {
        _nextReadyTime = Time.time + cooldown;

        int   damage = Mathf.RoundToInt(Mathf.Lerp(minDamage, maxDamage, _chargeRatio));
        float range  = Mathf.Lerp(minRange, maxRange, _chargeRatio);

        Vector2 origin = (Vector2)transform.position;
        Vector2 dir    = GetAimDir(origin);

        int enemyLayer  = LayerMask.NameToLayer("Enemy");
        int groundLayer = LayerMask.NameToLayer("Ground");
        int mask = 0;
        if (enemyLayer  >= 0) mask |= 1 << enemyLayer;
        if (groundLayer >= 0) mask |= 1 << groundLayer;
        if (mask == 0) mask = ~0;

        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(mask);
        filter.useTriggers = false;

        RaycastHit2D[] hits = new RaycastHit2D[10];
        int count = Physics2D.Raycast(origin, dir, filter, hits, range);

        Vector2 endpoint = origin + dir * range;
        for (int i = 0; i < count; i++)
        {
            if (hits[i].collider == null) continue;
            if (hits[i].collider.transform.root == transform.root) continue;
            endpoint = hits[i].point;
            var dmg = hits[i].collider.GetComponent<IDamageable>();
            if (dmg != null) dmg.TakeDamage(damage, hits[i].point, hits[i].normal);
            break;
        }

        if (_line != null)
            StartCoroutine(ShowLine(origin, endpoint));

        OnPulseFired?.Invoke();
    }

    Vector2 GetAimDir(Vector2 origin)
    {
        if (_cam != null)
        {
            Vector3 mouse = GameInput.GetPointerPosition();
            mouse.z = Mathf.Abs(_cam.transform.position.z - transform.position.z);
            Vector2 world = _cam.ScreenToWorldPoint(mouse);
            Vector2 d = world - origin;
            if (d.sqrMagnitude > 0.001f) return d.normalized;
        }
        if (_aim != null && _aim.AimDirection.sqrMagnitude > 0.001f)
            return _aim.AimDirection.normalized;
        return Vector2.right;
    }

    IEnumerator ShowLine(Vector2 from, Vector2 to)
    {
        if (_line == null) yield break;
        _line.enabled = true;
        _line.SetPosition(0, from);
        _line.SetPosition(1, to);
        yield return new WaitForSeconds(0.22f);
        _line.enabled = false;
    }

    LineRenderer CreatePulseLine()
    {
        var go = new GameObject("PulseLine");
        go.transform.SetParent(transform, false);
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace  = true;
        lr.positionCount  = 2;
        lr.startWidth     = 0.18f;
        lr.endWidth       = 0.06f;
        lr.startColor     = pulseColor;
        lr.endColor       = new Color(pulseColor.r, pulseColor.g, pulseColor.b, 0.3f);
        lr.sortingOrder   = 25;
        var shader = Shader.Find("Sprites/Default");
        if (shader != null) lr.material = new Material(shader);
        lr.enabled = false;
        return lr;
    }
}
