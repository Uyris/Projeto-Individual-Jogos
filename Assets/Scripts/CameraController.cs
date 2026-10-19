using UnityEngine;

/// <summary>
/// Ajusta o zoom da câmera para a arena inteira caber em qualquer proporção de tela
/// e faz o efeito de tremida quando o jogador leva dano.
/// </summary>
[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
    [Tooltip("Área do mundo (em unidades) que precisa aparecer na tela")]
    public Vector2 viewSize = new Vector2(34.4f, 20.2f);

    static CameraController instance;

    Camera cam;
    Vector3 basePosition;
    float shakeTime;
    float shakeDuration;
    float shakeStrength;

    void Awake()
    {
        instance = this;
        cam = GetComponent<Camera>();
        basePosition = transform.position;
    }

    void LateUpdate()
    {
        cam.orthographicSize = Mathf.Max(viewSize.y * 0.5f, viewSize.x * 0.5f / cam.aspect);

        Vector3 offset = Vector3.zero;
        if (shakeTime > 0f)
        {
            shakeTime -= Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(shakeTime / shakeDuration);
            offset = (Vector3)(Random.insideUnitCircle * shakeStrength * k);
        }
        transform.position = basePosition + offset;
    }

    public static void Shake(float duration, float strength)
    {
        if (instance == null) return;
        instance.shakeTime = instance.shakeDuration = duration;
        instance.shakeStrength = strength;
    }
}
