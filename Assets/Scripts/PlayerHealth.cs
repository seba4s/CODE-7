using System;
using System.Collections;
using UnityEngine;

public class PlayerHealth : MonoBehaviour, IDamageable
{
    public int maxHP = 100;
    public int currentHP;

    [Header("Respawn")]
    public float respawnDelay = 1.2f;
    public float invulnerabilityAfterRespawn = 1.0f;
    public Vector3 manualRespawnPoint;
    public bool useManualRespawnPoint;

    public bool IsInvulnerable { get; private set; }

    public event Action<int, int> OnHealthChanged; // current, max
    public event Action<int> OnDamageTaken; // amount

    Rigidbody2D rb;
    PlayerStamina stamina;
    MonoBehaviour playerController;
    MonoBehaviour playerDash;
    bool isDead;
    Vector3 startRespawnPoint;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        stamina = GetComponent<PlayerStamina>();
        playerController = GetComponent<PlayerController2D>();
        playerDash = GetComponent<PlayerDash>();

        startRespawnPoint = transform.position;
        currentHP = maxHP;
        OnHealthChanged?.Invoke(currentHP, maxHP);
    }

    public void SetInvulnerable(bool value) => IsInvulnerable = value;

    public void TakeDamage(int amount, Vector2 hitPoint, Vector2 hitNormal)
    {
        if (amount <= 0) return;
        if (IsInvulnerable) return;
        if (isDead) return;

        currentHP = Mathf.Max(0, currentHP - amount);
        OnDamageTaken?.Invoke(amount);
        OnHealthChanged?.Invoke(currentHP, maxHP);

        if (currentHP == 0)
        {
            StartCoroutine(RespawnRoutine());
        }
    }

    public void SetRespawnPoint(Vector3 point)
    {
        manualRespawnPoint = point;
        useManualRespawnPoint = true;
    }

    IEnumerator RespawnRoutine()
    {
        if (isDead) yield break;
        isDead = true;
        IsInvulnerable = true;

        if (playerController != null) playerController.enabled = false;
        if (playerDash != null) playerDash.enabled = false;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.simulated = false;
        }

        Debug.Log("Player dead");
        yield return new WaitForSeconds(respawnDelay);

        Vector3 target = useManualRespawnPoint ? manualRespawnPoint : startRespawnPoint;
        transform.position = target;

        currentHP = maxHP;
        OnHealthChanged?.Invoke(currentHP, maxHP);

        if (stamina != null)
            stamina.RestoreFull();

        if (rb != null)
        {
            rb.simulated = true;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        if (playerController != null) playerController.enabled = true;
        if (playerDash != null) playerDash.enabled = true;

        if (invulnerabilityAfterRespawn > 0f)
        {
            yield return new WaitForSeconds(invulnerabilityAfterRespawn);
        }

        IsInvulnerable = false;
        isDead = false;
    }
}