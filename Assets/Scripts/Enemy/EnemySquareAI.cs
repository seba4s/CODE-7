using UnityEngine;

/// <summary>
/// Enemigo cuadrado con IA de 3 estados: Patrulla / Perseguir / Atacar
/// Se puede crear completamente por código (ver EnemyRuntimeSpawner).
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public class EnemySquareAI : MonoBehaviour, IDamageable
{
    public enum EnemyVariant
    {
        BasicMelee,
        Runner,
        Tank,
        Glitch
    }

    // ── Estadísticas ──────────────────────────────────────────
    [Header("Stats")]
    public int   maxHp         = 30;
    public int   contactDamage = 10;
    public float damageInterval = 1f;   // segundos entre daños por contacto

    // ── Movimiento ────────────────────────────────────────────
    [Header("Movement")]
    public float patrolSpeed  = 2f;
    public float chaseSpeed   = 4f;
    public float patrolRange  = 4f;     // distancia desde spawnPoint a cada lado

    // ── Detección ─────────────────────────────────────────────
    [Header("Detection")]
    public float detectRange  = 6f;
    public float loseRange    = 10f;
    public float attackRange  = 0.6f;   // distancia para daño de contacto

    [Header("Variant")]
    public EnemyVariant variant = EnemyVariant.BasicMelee;

    // ── Estado interno ────────────────────────────────────────
    enum State { Patrol, Chase, Attack }
    State     state        = State.Patrol;
    int       currentHp;
    Rigidbody2D rb;
    Transform playerTransform;
    Vector2   spawnPoint;
    float     patrolTarget;   // X hacia la que patrulla ahora
    float     nextDamageTime;
    float     nextGlitchBurstTime;
    SpriteRenderer sr;

    // ── Colores por estado ────────────────────────────────────
    static readonly Color ColorPatrol = new Color(0.9f, 0.2f, 0.2f);   // rojo
    static readonly Color ColorChase  = new Color(1f,   0.6f, 0f);     // naranja
    static readonly Color ColorAttack = new Color(1f,   1f,   0f);     // amarillo

    // ─────────────────────────────────────────────────────────
    void Awake()
    {
        rb         = GetComponent<Rigidbody2D>();
        sr         = GetComponent<SpriteRenderer>();
        currentHp  = maxHp;
        spawnPoint = transform.position;
        patrolTarget = spawnPoint.x + patrolRange;

        ApplyVariantStats();

        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.gravityScale = 3f;

        // Buscar jugador por tag; si falla, buscar por componente
        var playerObj = GameObject.FindWithTag("Player");
        if (playerObj == null)
        {
            var pc = FindAnyObjectByType<PlayerController2D>();
            if (pc != null) playerObj = pc.gameObject;
        }
        if (playerObj != null) playerTransform = playerObj.transform;
    }

    void Update()
    {
        if (playerTransform == null) return;

        float distToPlayer = Vector2.Distance(transform.position, playerTransform.position);

        switch (state)
        {
            case State.Patrol:
                if (distToPlayer <= detectRange)
                    ChangeState(State.Chase);
                else
                    DoPatrol();
                break;

            case State.Chase:
                if (distToPlayer <= attackRange)
                    ChangeState(State.Attack);
                else if (distToPlayer > loseRange)
                    ChangeState(State.Patrol);
                else
                    DoChase();
                break;

            case State.Attack:
                if (distToPlayer > attackRange)
                    ChangeState(State.Chase);
                else
                    DoAttack();
                break;
        }
    }

    // ── Patrulla ──────────────────────────────────────────────
    void DoPatrol()
    {
        float dir = patrolTarget > transform.position.x ? 1f : -1f;
        rb.linearVelocity = new Vector2(dir * patrolSpeed, rb.linearVelocity.y);

        if (Mathf.Abs(transform.position.x - patrolTarget) < 0.15f)
        {
            // Invertir destino
            patrolTarget = patrolTarget == spawnPoint.x + patrolRange
                ? spawnPoint.x - patrolRange
                : spawnPoint.x + patrolRange;
        }
    }

    // ── Perseguir ────────────────────────────────────────────
    void DoChase()
    {
        float dir = playerTransform.position.x > transform.position.x ? 1f : -1f;

        float burst = 1f;
        if (variant == EnemyVariant.Glitch && Time.time >= nextGlitchBurstTime)
        {
            burst = 1.8f;
            nextGlitchBurstTime = Time.time + 0.55f;
        }

        rb.linearVelocity = new Vector2(dir * chaseSpeed * burst, rb.linearVelocity.y);
    }

    // ── Atacar (daño de contacto continuo) ────────────────────
    void DoAttack()
    {
        // Se queda quieto horizontalmente pero sigue afectado por gravedad
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        TryDealContactDamage();
    }

    void TryDealContactDamage()
    {
        if (Time.time < nextDamageTime) return;

        // Buscar PlayerHealth por distancia cercana
        var ph = playerTransform.GetComponent<PlayerHealth>();
        if (ph != null)
        {
            ph.TakeDamage(contactDamage, transform.position, Vector2.zero);
            nextDamageTime = Time.time + damageInterval;
        }
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        if (Time.time < nextDamageTime) return;
        if (collision == null) return;

        var ph = collision.gameObject.GetComponent<PlayerHealth>();
        if (ph == null) return;

        Vector2 hitPoint = collision.contactCount > 0 ? collision.GetContact(0).point : (Vector2)transform.position;
        ph.TakeDamage(contactDamage, hitPoint, Vector2.zero);
        nextDamageTime = Time.time + damageInterval;
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (Time.time < nextDamageTime) return;
        if (other == null) return;

        var ph = other.GetComponent<PlayerHealth>();
        if (ph == null) return;

        ph.TakeDamage(contactDamage, transform.position, Vector2.zero);
        nextDamageTime = Time.time + damageInterval;
    }

    // ── Cambio de estado ────────────────────────────────────
    void ChangeState(State next)
    {
        state = next;
        if (sr != null && variant == EnemyVariant.BasicMelee)
            sr.color = next == State.Patrol ? ColorPatrol
                     : next == State.Chase  ? ColorChase
                     : ColorAttack;

        // Al volver a patrullar, retomar la ronda desde la posición actual
        if (next == State.Patrol)
        {
            spawnPoint = transform.position;
            patrolTarget = transform.position.x + patrolRange;
        }
    }

    void ApplyVariantStats()
    {
        if (sr == null) return;

        switch (variant)
        {
            case EnemyVariant.Runner:
                maxHp = Mathf.RoundToInt(maxHp * 0.75f);
                patrolSpeed *= 1.35f;
                chaseSpeed *= 1.55f;
                contactDamage = Mathf.RoundToInt(contactDamage * 0.85f);
                detectRange += 1.2f;
                sr.color = new Color(1f, 0.45f, 0.2f, 1f);
                transform.localScale *= 0.88f;
                break;

            case EnemyVariant.Tank:
                maxHp = Mathf.RoundToInt(maxHp * 1.9f);
                patrolSpeed *= 0.72f;
                chaseSpeed *= 0.68f;
                contactDamage = Mathf.RoundToInt(contactDamage * 1.65f);
                detectRange -= 0.4f;
                sr.color = new Color(0.7f, 0.16f, 0.16f, 1f);
                transform.localScale *= 1.28f;
                break;

            case EnemyVariant.Glitch:
                maxHp = Mathf.RoundToInt(maxHp * 1.1f);
                patrolSpeed *= 1.1f;
                chaseSpeed *= 1.18f;
                contactDamage = Mathf.RoundToInt(contactDamage * 1.2f);
                detectRange += 0.8f;
                damageInterval *= 0.8f;
                sr.color = new Color(0.95f, 0.12f, 0.62f, 1f);
                break;

            default:
                sr.color = ColorPatrol;
                break;
        }

        currentHp = maxHp;
    }

    // ── IDamageable ──────────────────────────────────────────
    public void TakeDamage(int amount, Vector2 hitPoint, Vector2 hitNormal)
    {
        currentHp -= amount;
        Debug.Log($"[Enemy] {name} recibió {amount} daño → HP={currentHp}");

        // Flash blanco
        if (sr != null) StartCoroutine(FlashWhite());

        if (currentHp <= 0) Die();
    }

    System.Collections.IEnumerator FlashWhite()
    {
        Color prev = sr.color;
        sr.color = Color.white;
        yield return new WaitForSeconds(0.08f);
        sr.color = prev;
    }

    void Die()
    {
        Debug.Log($"[Enemy] {name} eliminado");
        Destroy(gameObject);
    }

    // ── Gizmos en editor ────────────────────────────────────
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
