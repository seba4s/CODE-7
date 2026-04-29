using UnityEngine;

public class AimProvider : MonoBehaviour
{
    public Camera cam;
    public Vector2 AimDirection { get; private set; } = Vector2.right;

    void Awake()
    {
        if (cam == null) cam = Camera.main;
    }

    void Update()
    {
        Vector3 mouse = GameInput.GetPointerPosition();
        Vector3 world = cam.ScreenToWorldPoint(mouse);
        Vector2 dir = (Vector2)(world - transform.position);

        if (dir.sqrMagnitude > 0.0001f)
            AimDirection = dir.normalized;
    }
}