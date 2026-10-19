using System.Collections;
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
    public Transform visual;
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
        if (!GameManager.IsPlaying)
        {
            movement = Vector2.zero;
            return;
        }

        // Tutorial (Movimentação Diagonal): o vetor nunca passa de tamanho 1, então andar na
        // diagonal não fica mais rápido.
        movement = Vector2.ClampMagnitude(GameInput.Move.ReadValue<Vector2>(), 1f);
    }

    void FixedUpdate()
    {
        if (!GameManager.IsPlaying)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        // Tutorial (Movimentando o Jogador): a física move o Rigidbody2D no FixedUpdate.
        Vector2 target = movement * speed;
        rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, target, acceleration * Time.fixedDeltaTime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!GameManager.IsPlaying) return;

        // Tutorial (Coletáveis): identifica o item pela tag e destrói o objeto.
        if (other.CompareTag("Coletavel"))
        {
            Collectible item = other.GetComponent<Collectible>();
            if (item == null || item.Collected) return;

            item.Collected = true;
            audioSource.PlayOneShot(collectClip);
            GameManager.Instance.Collect(item);
            Destroy(other.gameObject);
        }
    }

    /// <summary>Chamado pelo GameManager na derrota.</summary>
    public void Die()
    {
        StopPhysics();
        StartCoroutine(ShrinkRoutine(transform.position));
    }

    void StopPhysics()
    {
        rb.linearVelocity = Vector2.zero;
        rb.simulated = false;
    }

    IEnumerator ShrinkRoutine(Vector3 target)
    {
        Vector3 start = transform.position;
        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.8f)
        {
            transform.position = Vector3.Lerp(start, target, t);
            visual.localScale = Vector3.one * (1f - t);
            visual.Rotate(0f, 0f, 720f * Time.unscaledDeltaTime);
            yield return null;
        }
        visual.localScale = Vector3.zero;
    }
}
