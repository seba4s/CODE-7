using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerDash : MonoBehaviour
{
    public PlayerHealth health;
    public PlayerStamina stamina;
    public bool IsDashing => isDashing;

    [Header("Dash")]
    public float dashSpeed = 22f;
    public float dashDuration = 0.15f;
    public float dashCooldown = 0.8f;
    public float staminaCost = 25f;

    [Header("Input")]
    [FormerlySerializedAs("aim")]
    public KeyCode dashKey = KeyCode.LeftShift;
    public bool dashOnADKeys = false;
    public bool dashOnShiftWithDirection = true;

    Rigidbody2D rb;
    float nextDashTime;
    bool isDashing;
    float dashEndTime;
    Vector2 dashDir = Vector2.right;

    float originalGravityScale;
    Vector2 lastNonZeroDir = Vector2.right;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (health == null) health = GetComponent<PlayerHealth>();
        if (stamina == null) stamina = GetComponent<PlayerStamina>();
        if (stamina == null) stamina = gameObject.AddComponent<PlayerStamina>();
        originalGravityScale = rb.gravityScale;
    }

    void Update()
    {
        float x = GameInput.GetMoveXRaw();
        if (x > 0.01f) lastNonZeroDir = Vector2.right;
        else if (x < -0.01f) lastNonZeroDir = Vector2.left;

        if (!isDashing)
        {
            if (dashOnShiftWithDirection && GameInput.GetKeyDown(dashKey))
            {
                float heldX = GameInput.GetMoveXRaw();
                if (heldX > 0.01f) TryStartDash(Vector2.right);
                else if (heldX < -0.01f) TryStartDash(Vector2.left);
                else TryStartDash(lastNonZeroDir);
            }
        }

        if (isDashing && Time.time >= dashEndTime)
            EndDash();
    }

    void FixedUpdate()
    {
        if (!isDashing) return;

        // Mantener velocidad constante durante todo el dash.
        rb.linearVelocity = new Vector2(dashDir.x * dashSpeed, rb.linearVelocity.y);
    }

    void TryStartDash(Vector2 requestedDir)
    {
        if (Time.time < nextDashTime) return;

        if (stamina != null && !stamina.TryConsume(staminaCost))
        {
            // Sin estamina: no se inicia el dash.
            return;
        }

        nextDashTime = Time.time + dashCooldown;

        Vector2 dir = requestedDir.sqrMagnitude > 0.01f ? requestedDir.normalized : lastNonZeroDir;
        if (dir.sqrMagnitude <= 0.01f) dir = Vector2.right;
        dir = new Vector2(Mathf.Sign(dir.x), 0f);
        dashDir = dir;

        isDashing = true;
        dashEndTime = Time.time + dashDuration;

        // i-frames ON
        if (health != null) health.SetInvulnerable(true);

        // Dash: impulso/velocidad fija durante un rato
        rb.gravityScale = 0f;
        rb.linearVelocity = new Vector2(dir.x * dashSpeed, rb.linearVelocity.y);
    }

    void EndDash()
    {
        isDashing = false;
        if (health != null) health.SetInvulnerable(false);

        rb.gravityScale = originalGravityScale;
        // opcional: no “cortar” la velocidad al salir del dash
        // rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y);
    }
}