using UnityEngine;

/// <summary>Animação simples de flutuar e girar (decoração, minas, drones, menu).</summary>
public class Floater : MonoBehaviour
{
    public float bobAmplitude;
    public float bobSpeed = 1.5f;
    public float rotateSpeed;
    public float phase;

    Vector3 startPosition;

    void Start()
    {
        startPosition = transform.localPosition;
    }

    void Update()
    {
        if (bobAmplitude != 0f)
            transform.localPosition = startPosition + Vector3.up * Mathf.Sin(Time.unscaledTime * bobSpeed + phase) * bobAmplitude;
        if (rotateSpeed != 0f)
            transform.Rotate(0f, 0f, rotateSpeed * Time.deltaTime);
    }
}
