using UnityEngine;

public class PlayerVisualFeedback : MonoBehaviour
{
    public PlayerHealth health;
    public SpriteRenderer glassesRenderer;

    [Header("Colors")]
    public Color good = Color.green;
    public Color mid = Color.yellow;
    public Color critical = Color.red;

    [Header("Glitch (MVP)")]
    public SpriteRenderer bodyRenderer;
    public float damageFlashTime = 0.08f;
    public Color flashColor = Color.white;

    Color bodyBaseColor;

    void Awake()
    {
        if (health == null) health = GetComponent<PlayerHealth>();
        if (bodyRenderer != null) bodyBaseColor = bodyRenderer.color;

        health.OnHealthChanged += HandleHealthChanged;
        health.OnDamageTaken += HandleDamage;
    }

    void OnDestroy()
    {
        if (health == null) return;
        health.OnHealthChanged -= HandleHealthChanged;
        health.OnDamageTaken -= HandleDamage;
    }

    void HandleHealthChanged(int current, int max)
    {
        float p = (max <= 0) ? 0f : (float)current / max;

        if (p > 0.66f) glassesRenderer.color = good;
        else if (p > 0.33f) glassesRenderer.color = mid;
        else glassesRenderer.color = critical;
    }

    void HandleDamage(int amount)
    {
        if (bodyRenderer == null) return;
        StopAllCoroutines();
        StartCoroutine(DamageFlash());
    }

    System.Collections.IEnumerator DamageFlash()
    {
        bodyRenderer.color = flashColor;
        yield return new WaitForSeconds(damageFlashTime);
        bodyRenderer.color = bodyBaseColor;
    }
}