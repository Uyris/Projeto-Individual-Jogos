using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>Cápsula de fuga: fica trancada até todos os cristais serem coletados.</summary>
public class ExitPod : MonoBehaviour
{
    public SpriteRenderer body;
    public Sprite lockedSprite;
    public Sprite openSprite;
    public Light2D glow;
    public ParticleSystem openFx;
    public Color lockedColor = new Color(1f, 0.25f, 0.3f);
    public Color openColor = new Color(0.3f, 1f, 0.55f);

    public bool IsOpen { get; private set; }

    void Start()
    {
        body.sprite = lockedSprite;
        if (glow) glow.color = lockedColor;
    }

    void Update()
    {
        if (glow) glow.intensity = IsOpen ? 1.3f + Mathf.Sin(Time.time * 5f) * 0.4f : 0.6f;
        if (IsOpen) body.transform.Rotate(0f, 0f, -60f * Time.deltaTime);
    }

    public void Unlock()
    {
        if (IsOpen) return;
        IsOpen = true;
        body.sprite = openSprite;
        if (glow) glow.color = openColor;
        if (openFx) openFx.Play();
        CameraController.Shake(0.2f, 0.15f);
    }
}
