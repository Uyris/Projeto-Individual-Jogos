using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// Mantém um botão selecionado no painel para que os menus possam ser navegados
/// com controle ou teclado (mesmo depois de usar o mouse).
/// </summary>
public class MenuFocus : MonoBehaviour
{
    public GameObject firstSelected;

    void OnEnable()
    {
        StartCoroutine(SelectNextFrame());
    }

    IEnumerator SelectNextFrame()
    {
        yield return null;
        Select();
    }

    void Update()
    {
        EventSystem es = EventSystem.current;
        if (es == null) return;

        GameObject current = es.currentSelectedGameObject;
        bool valid = current != null && current.activeInHierarchy && current.transform.IsChildOf(transform);
        if (!valid && NavigationPressed()) Select();
    }

    static bool NavigationPressed()
    {
        Gamepad gp = Gamepad.current;
        if (gp != null && (gp.leftStick.ReadValue().sqrMagnitude > 0.25f ||
                           gp.dpad.ReadValue().sqrMagnitude > 0.25f ||
                           gp.buttonSouth.wasPressedThisFrame))
            return true;

        Keyboard kb = Keyboard.current;
        return kb != null && (kb.upArrowKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame ||
                              kb.wKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame ||
                              kb.tabKey.wasPressedThisFrame);
    }

    public void Select()
    {
        EventSystem es = EventSystem.current;
        if (es == null || firstSelected == null) return;
        es.SetSelectedGameObject(null);
        es.SetSelectedGameObject(firstSelected);
    }
}
