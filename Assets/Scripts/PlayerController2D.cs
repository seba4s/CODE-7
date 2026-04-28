using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController2D : MonoBehaviour
{
    [Header("Move")]
    public float moveSpeed = 7f;

    [Header("Wall")]
    public float wallCheckDistance = 0.25f;
    public float wallFallSpeed = 2.5f;
    public bool applyZeroFrictionMaterial = true;

    [Header("Jump")]
    public float jumpHeight = 4f;
    public Transform groundCheck;
    public float groundCheckRadius = 0.15f;
    public LayerMask groundMask;

    Rigidbody2D rb;
    PlayerDash dash;
    Collider2D col;

    bool isGrounded;
    bool isTouchingWall;
    bool wallOnRight;
    bool hasDoubleJump; // luego lo conectamos a progreso
    bool usedDoubleJump;

    /// <summary>Expone el estado de suelo para que PlayerVFX pueda detectar saltos/aterrizajes.</summary>
    public bool IsGrounded => isGrounded;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        dash = GetComponent<PlayerDash>();
        col = GetComponent<Collider2D>();

        if (applyZeroFrictionMaterial && col != null)
        {
            var mat = new PhysicsMaterial2D("PlayerZeroFriction")
            {
                friction = 0f,
                bounciness = 0f
            };
            col.sharedMaterial = mat;
        }
    }

    void Update()
    {
        // No procesar input si el juego está pausado por NarrativeUI
        if (NarrativeUI.IsGamePaused)
        {
            // Detener el personaje completamente mientras el mensaje está visible
            if (rb != null) rb.linearVelocity = Vector2.zero;
            return;
        }

        // Ground check
        Vector2 checkPos = groundCheck != null ? (Vector2)groundCheck.position : (Vector2)transform.position;
        isGrounded = Physics2D.OverlapCircle(checkPos, groundCheckRadius, groundMask);
        if (isGrounded) usedDoubleJump = false;

        UpdateWallContact();

        // Horizontal move (no sobrescribir velocidad durante dash)
        if (dash == null || !dash.IsDashing)
        {
            float x = Input.GetAxisRaw("Horizontal");

            // Anti-wall-stick: si empujas hacia la pared en el aire, no te quedas pegado.
            if (!isGrounded && isTouchingWall && ((wallOnRight && x > 0f) || (!wallOnRight && x < 0f)))
            {
                rb.linearVelocity = new Vector2(0f, Mathf.Min(rb.linearVelocity.y, -wallFallSpeed));
            }
            else
            {
            rb.linearVelocity = new Vector2(x * moveSpeed, rb.linearVelocity.y);
            }
        }

        // Jump
        if (Input.GetButtonDown("Jump"))
        {
            if (isGrounded)
            {
                Jump();
            }
            else if (hasDoubleJump && !usedDoubleJump)
            {
                usedDoubleJump = true;
                Jump();
            }
        }
    }

    void Jump()
    {
        // v = sqrt(2 * g * h)
        float g = Mathf.Abs(Physics2D.gravity.y) * rb.gravityScale;
        float jumpVel = Mathf.Sqrt(2f * g * jumpHeight);

        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpVel);
    }

    // Llamarás a esto cuando desbloquees doble salto por historia
    public void SetDoubleJumpUnlocked(bool unlocked) => hasDoubleJump = unlocked;

    void UpdateWallContact()
    {
        Vector2 origin = col != null ? col.bounds.center : (Vector2)transform.position;
        float castDistance = wallCheckDistance;

        RaycastHit2D hitRight = Physics2D.Raycast(origin, Vector2.right, castDistance, groundMask);
        RaycastHit2D hitLeft = Physics2D.Raycast(origin, Vector2.left, castDistance, groundMask);

        if (hitRight.collider != null)
        {
            isTouchingWall = true;
            wallOnRight = true;
            return;
        }

        if (hitLeft.collider != null)
        {
            isTouchingWall = true;
            wallOnRight = false;
            return;
        }

        isTouchingWall = false;
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}