using UnityEngine;

/// <summary>
/// Astronauta controlado pelo jogador: movimento com Rigidbody2D (corrigindo a diagonal)
/// e coleta de itens por trigger.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movimento")]
    public float speed = 6f;
    public float acceleration = 50f;

    [Header("Referências")]
    public AudioSource audioSource;
    public AudioClip collectClip;

    Rigidbody2D rb;
    Vector2 movement;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        // Tutorial (Movimentação Diagonal): o vetor nunca passa de tamanho 1, então andar na
        // diagonal não fica mais rápido.
        movement = Vector2.ClampMagnitude(GameInput.Move.ReadValue<Vector2>(), 1f);
    }

    void FixedUpdate()
    {
        // Tutorial (Movimentando o Jogador): a física move o Rigidbody2D no FixedUpdate.
        Vector2 target = movement * speed;
        rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, target, acceleration * Time.fixedDeltaTime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Tutorial (Coletáveis): identifica o item pela tag e destrói o objeto.
        if (other.CompareTag("Coletavel"))
        {
            Collectible item = other.GetComponent<Collectible>();
            if (item == null || item.Collected) return;

            item.Collected = true;
            audioSource.PlayOneShot(collectClip);
            Destroy(other.gameObject);
        }
    }
}
