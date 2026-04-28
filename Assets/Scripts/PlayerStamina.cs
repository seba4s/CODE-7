using System;
using UnityEngine;

public class PlayerStamina : MonoBehaviour
{
    [Header("Stamina")]
    public float maxStamina = 100f;
    public float currentStamina;
    public float regenPerSecond = 22f;
    public float regenDelayAfterUse = 0.45f;

    public event Action<float, float> OnStaminaChanged; // current, max

    float nextRegenTime;

    void Awake()
    {
        currentStamina = Mathf.Clamp(currentStamina <= 0f ? maxStamina : currentStamina, 0f, maxStamina);
        OnStaminaChanged?.Invoke(currentStamina, maxStamina);
    }

    void Update()
    {
        if (Time.time < nextRegenTime) return;
        if (currentStamina >= maxStamina) return;

        currentStamina = Mathf.Min(maxStamina, currentStamina + regenPerSecond * Time.deltaTime);
        OnStaminaChanged?.Invoke(currentStamina, maxStamina);
    }

    public bool TryConsume(float amount)
    {
        if (amount <= 0f) return true;
        if (currentStamina < amount) return false;

        currentStamina -= amount;
        nextRegenTime = Time.time + regenDelayAfterUse;
        OnStaminaChanged?.Invoke(currentStamina, maxStamina);
        return true;
    }

    public void RestoreFull()
    {
        currentStamina = maxStamina;
        OnStaminaChanged?.Invoke(currentStamina, maxStamina);
    }
}
