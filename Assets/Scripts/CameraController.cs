using UnityEngine;

/// <summary>
/// Ajusta o zoom da câmera para a arena inteira caber em qualquer proporção de tela.
/// </summary>
[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
    [Tooltip("Área do mundo (em unidades) que precisa aparecer na tela")]
    public Vector2 viewSize = new Vector2(34.4f, 20.2f);

    Camera cam;

    void Awake()
    {
        cam = GetComponent<Camera>();
    }

    void LateUpdate()
    {
        cam.orthographicSize = Mathf.Max(viewSize.y * 0.5f, viewSize.x * 0.5f / cam.aspect);
    }
}
