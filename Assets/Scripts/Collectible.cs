using UnityEngine;

/// <summary>
/// Item coletável (prefab) que flutua no lugar até ser pego.
/// A detecção é feita pelo jogador através da tag "Coletavel".
/// </summary>
public class Collectible : MonoBehaviour
{
    public int points = 100;
    public Transform visual;

    public bool Collected { get; set; }

    float phase;

    void Start()
    {
        phase = Random.value * 10f;
    }

    void Update()
    {
        if (visual == null) return;
        float t = Time.time + phase;
        visual.localPosition = new Vector3(0f, Mathf.Sin(t * 3f) * 0.08f, 0f);
        float s = 1f + Mathf.Sin(t * 6f) * 0.05f;
        visual.localScale = new Vector3(s, s, 1f);
    }
}
