using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Ações de entrada do jogo criadas por código com o Input System.
/// Por enquanto funciona só com teclado (WASD e setas).
/// </summary>
public static class GameInput
{
    static InputAction move;

    public static InputAction Move { get { Init(); return move; } }

    // Garante que as ações sejam recriadas mesmo com "Domain Reload" desligado no editor.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetActions()
    {
        move?.Dispose();
        move = null;
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

        move.Enable();
    }
}
