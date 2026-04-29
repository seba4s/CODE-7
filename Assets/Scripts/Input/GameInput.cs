using UnityEngine;
using UnityEngine.InputSystem;

public static class GameInput
{
    const float StickDeadZone = 0.25f;

    public static float GetMoveXRaw()
    {
        float x = 0f;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) x -= 1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) x += 1f;
        }

        if (Mathf.Abs(x) > 0.01f) return Mathf.Clamp(x, -1f, 1f);

        if (Gamepad.current != null)
        {
            float stickX = Gamepad.current.leftStick.x.ReadValue();
            if (Mathf.Abs(stickX) >= StickDeadZone)
                return Mathf.Sign(stickX);
        }

        return 0f;
    }

    public static Vector2 GetLookDelta()
    {
        Vector2 delta = Vector2.zero;

        if (Mouse.current != null)
            delta = Mouse.current.delta.ReadValue();

        if (Gamepad.current != null)
            delta += Gamepad.current.rightStick.ReadValue() * 8f;

        return delta;
    }

    public static Vector2 GetPointerPosition()
    {
        if (Mouse.current != null)
            return Mouse.current.position.ReadValue();

        if (Touchscreen.current != null)
            return Touchscreen.current.primaryTouch.position.ReadValue();

        return new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
    }

    public static bool GetPrimaryFireDown()
    {
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) return true;
        if (Gamepad.current != null && Gamepad.current.rightTrigger.wasPressedThisFrame) return true;
        return false;
    }

    public static bool GetJumpDown()
    {
        if (Keyboard.current != null)
        {
            if (Keyboard.current.spaceKey.wasPressedThisFrame) return true;
            if (Keyboard.current.wKey.wasPressedThisFrame)     return true;
            if (Keyboard.current.upArrowKey.wasPressedThisFrame) return true;
        }
        if (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame) return true;
        return false;
    }

    public static bool GetSecondaryFireHeld()
    {
        if (Mouse.current != null && Mouse.current.rightButton.isPressed) return true;
        if (Gamepad.current != null && Gamepad.current.leftTrigger.ReadValue() > 0.5f) return true;
        return false;
    }

    public static bool GetSecondaryFireUp()
    {
        if (Mouse.current != null && Mouse.current.rightButton.wasReleasedThisFrame) return true;
        if (Gamepad.current != null && Gamepad.current.leftTrigger.wasReleasedThisFrame) return true;
        return false;
    }

    public static bool GetInteractDown()
    {
        if (Keyboard.current != null)
        {
            if (Keyboard.current.eKey.wasPressedThisFrame) return true;
            if (Keyboard.current.enterKey.wasPressedThisFrame) return true;
            if (Keyboard.current.numpadEnterKey.wasPressedThisFrame) return true;
            if (Keyboard.current.spaceKey.wasPressedThisFrame) return true;
        }

        if (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame) return true;
        return false;
    }

    public static bool GetCancelDown()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) return true;
        if (Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame) return true;
        return false;
    }

    public static bool GetKeyDown(KeyCode key)
    {
        if (Keyboard.current == null) return false;

        switch (key)
        {
            case KeyCode.LeftShift: return Keyboard.current.leftShiftKey.wasPressedThisFrame;
            case KeyCode.RightShift: return Keyboard.current.rightShiftKey.wasPressedThisFrame;
            case KeyCode.E: return Keyboard.current.eKey.wasPressedThisFrame;
            case KeyCode.Return: return Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.numpadEnterKey.wasPressedThisFrame;
            case KeyCode.Space: return Keyboard.current.spaceKey.wasPressedThisFrame;
            case KeyCode.Escape: return Keyboard.current.escapeKey.wasPressedThisFrame;
            default: return false;
        }
    }

    public static bool GetMouseButtonDown(int button)
    {
        if (Mouse.current == null) return false;

        switch (button)
        {
            case 0: return Mouse.current.leftButton.wasPressedThisFrame;
            case 1: return Mouse.current.rightButton.wasPressedThisFrame;
            case 2: return Mouse.current.middleButton.wasPressedThisFrame;
            default: return false;
        }
    }
}