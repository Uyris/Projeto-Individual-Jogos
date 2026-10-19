using UnityEngine;

/// <summary>Cápsula de fuga: fica trancada até todos os cristais serem coletados.</summary>
public class ExitPod : MonoBehaviour
{
    public SpriteRenderer body;
    public Sprite lockedSprite;
    public Sprite openSprite;
    public Color lockedColor = new Color(1f, 0.25f, 0.3f);
    public Color openColor = new Color(0.3f, 1f, 0.55f);

    public bool IsOpen { get; private set; }

    void Start()
    {
        body.sprite = lockedSprite;
    }

    void Update()
    {
        if (IsOpen) body.transform.Rotate(0f, 0f, -60f * Time.deltaTime);
    }

    public void Unlock()
    {
        if (IsOpen) return;
        IsOpen = true;
        body.sprite = openSprite;
    }
}
