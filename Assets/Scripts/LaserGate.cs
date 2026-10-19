using UnityEngine;

/// <summary>
/// Barreira de laser que liga e desliga em um ciclo de tempo fixo.
/// Pisca antes de ligar para o jogador acertar o momento de passar.
/// </summary>
public class LaserGate : MonoBehaviour
{
    public float onDuration = 1.6f;
    public float offDuration = 1.8f;
    public float warningTime = 0.5f;
    public float timeOffset;

    [Header("Referências")]
    public SpriteRenderer beam;
    public Collider2D beamCollider;
    public Color beamColor = new Color(1f, 0.2f, 0.28f);

    float clock;

    void Start()
    {
        clock = timeOffset;
    }

    void Update()
    {
        if (GameManager.IsPlaying) clock += Time.deltaTime;

        float cycle = onDuration + offDuration;
        float t = Mathf.Repeat(clock, cycle);
        bool on = t < onDuration;
        bool warning = !on && t > cycle - warningTime;

        beamCollider.enabled = on;

        float alpha;
        if (on) alpha = 0.85f + Mathf.Sin(Time.time * 40f) * 0.15f;
        else if (warning) alpha = Mathf.Repeat(t, 0.12f) < 0.06f ? 0.5f : 0.12f;
        else alpha = 0.06f;

        Color c = beamColor;
        c.a = alpha;
        beam.color = c;
    }
}
