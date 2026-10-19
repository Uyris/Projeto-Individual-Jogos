using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Sons de navegação e clique dos botões.</summary>
[RequireComponent(typeof(Button))]
public class UIButtonSound : MonoBehaviour, ISelectHandler, IPointerEnterHandler
{
    float enabledAt;

    void Start()
    {
        GetComponent<Button>().onClick.AddListener(() => AudioManager.PlayUi(true));
    }

    void OnEnable()
    {
        enabledAt = Time.unscaledTime;
    }

    public void OnSelect(BaseEventData eventData)
    {
        // evita tocar o som quando o painel abre e seleciona o primeiro botão sozinho
        if (Time.unscaledTime - enabledAt > 0.2f) AudioManager.PlayUi(false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        AudioManager.PlayUi(false);
    }
}
