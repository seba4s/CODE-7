using UnityEngine;

public class EnemyDummy : MonoBehaviour, IDamageable
{
    public int hp = 30;

    public void TakeDamage(int amount, Vector2 hitPoint, Vector2 hitNormal)
    {
        hp -= amount;
        Debug.Log($"{name} hp={hp}");
        if (hp <= 0) Destroy(gameObject);
    }
}