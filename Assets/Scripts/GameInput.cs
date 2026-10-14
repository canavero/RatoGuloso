using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// Leitura dos controles em um lugar só: teclado (WASD / setas) e controle de Xbox (ou qualquer gamepad).
// Funciona com o Input System novo e também com o Input Manager antigo, então o projeto
// compila qualquer que seja a opção "Active Input Handling" do Player Settings.
public static class GameInput
{
    const float StickDeadZone = 0.2f;

    // Direção de movimento (comprimento máximo 1, então a diagonal não fica mais rápida).
    public static Vector2 Move()
    {
        Vector2 v = Vector2.zero;
#if ENABLE_INPUT_SYSTEM
        Keyboard kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) v.x -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) v.x += 1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed) v.y -= 1f;
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed) v.y += 1f;
        }
        Gamepad pad = Gamepad.current;
        if (pad != null)
        {
            Vector2 stick = pad.leftStick.ReadValue();
            if (stick.magnitude > StickDeadZone) v += stick;
            v += pad.dpad.ReadValue();
        }
#else
        v.x = Input.GetAxisRaw("Horizontal");
        v.y = Input.GetAxisRaw("Vertical");
        if (v.magnitude < StickDeadZone) v = Vector2.zero;
#endif
        return Vector2.ClampMagnitude(v, 1f);
    }

    // Dash: Espaço / Shift no teclado, A / RB / RT no controle.
    public static bool DashPressed()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard kb = Keyboard.current;
        Gamepad pad = Gamepad.current;
        return (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.leftShiftKey.wasPressedThisFrame || kb.rightShiftKey.wasPressedThisFrame))
            || (pad != null && (pad.buttonSouth.wasPressedThisFrame || pad.rightShoulder.wasPressedThisFrame || pad.rightTrigger.wasPressedThisFrame));
#else
        return Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift)
            || Input.GetKeyDown(KeyCode.JoystickButton0) || Input.GetKeyDown(KeyCode.JoystickButton5);
#endif
    }

    // Pausar / voltar: Esc ou P no teclado, Start no controle.
    public static bool PausePressed()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard kb = Keyboard.current;
        Gamepad pad = Gamepad.current;
        return (kb != null && (kb.escapeKey.wasPressedThisFrame || kb.pKey.wasPressedThisFrame))
            || (pad != null && pad.startButton.wasPressedThisFrame);
#else
        return Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P) || Input.GetKeyDown(KeyCode.JoystickButton7);
#endif
    }

    // Liga/desliga o som: M no teclado, Back/View no controle.
    public static bool MutePressed()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard kb = Keyboard.current;
        Gamepad pad = Gamepad.current;
        return (kb != null && kb.mKey.wasPressedThisFrame) || (pad != null && pad.selectButton.wasPressedThisFrame);
#else
        return Input.GetKeyDown(KeyCode.M) || Input.GetKeyDown(KeyCode.JoystickButton6);
#endif
    }
}
