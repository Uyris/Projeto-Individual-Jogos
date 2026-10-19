using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Ações de entrada do jogo criadas por código com o Input System.
/// Funciona com teclado e com controle (Xbox / gamepad genérico).
/// </summary>
public static class GameInput
{
    static InputAction move;
    static InputAction dash;
    static InputAction pause;

    public static InputAction Move { get { Init(); return move; } }
    public static InputAction Dash { get { Init(); return dash; } }
    public static InputAction Pause { get { Init(); return pause; } }

    // Garante que as ações sejam recriadas mesmo com "Domain Reload" desligado no editor.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetActions()
    {
        move?.Dispose();
        dash?.Dispose();
        pause?.Dispose();
        move = dash = pause = null;
    }

    static void Init()
    {
        if (move != null) return;

        move = new InputAction("Move", InputActionType.Value, expectedControlType: "Vector2");
        move.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
        move.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow")
            .With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
        move.AddBinding("<Gamepad>/leftStick");
        move.AddBinding("<Gamepad>/dpad");
        move.AddBinding("<Joystick>/stick");

        dash = new InputAction("Dash", InputActionType.Button);
        dash.AddBinding("<Keyboard>/space");
        dash.AddBinding("<Keyboard>/leftShift");
        dash.AddBinding("<Gamepad>/buttonSouth");
        dash.AddBinding("<Gamepad>/rightTrigger");
        dash.AddBinding("<Joystick>/trigger");

        pause = new InputAction("Pause", InputActionType.Button);
        pause.AddBinding("<Keyboard>/escape");
        pause.AddBinding("<Keyboard>/p");
        pause.AddBinding("<Gamepad>/start");

        move.Enable();
        dash.Enable();
        pause.Enable();
    }
}
