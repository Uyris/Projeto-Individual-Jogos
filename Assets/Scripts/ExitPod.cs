using UnityEngine;

/// <summary>Cápsula de fuga: fica trancada até todos os cristais serem coletados.</summary>
public class ExitPod : MonoBehaviour
{
    public SpriteRenderer body;
    public Color lockedColor = new Color(1f, 0.25f, 0.3f);
    public Color openColor = new Color(0.3f, 1f, 0.55f);

    public bool IsOpen { get; private set; }

    void Start()
    {
        body.color = lockedColor;
    }

    void Update()
    {
        if (IsOpen) body.transform.Rotate(0f, 0f, -60f * Time.deltaTime);
    }

    public void Unlock()
    {
        if (IsOpen) return;
        IsOpen = true;
        body.color = openColor;
    }
}
