using UnityEngine;
using UnityEngine.UI;

/// <summary>Faz o fundo de estrelas do menu se mover lentamente.</summary>
[RequireComponent(typeof(RawImage))]
public class UVScroller : MonoBehaviour
{
    public Vector2 speed = new Vector2(0.008f, 0.002f);

    RawImage image;

    void Awake()
    {
        image = GetComponent<RawImage>();
    }

    void Update()
    {
        Rect r = image.uvRect;
        r.position += speed * Time.unscaledDeltaTime;
        image.uvRect = r;
    }
}
