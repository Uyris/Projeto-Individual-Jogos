using UnityEngine;

/// <summary>
/// Astronauta controlado pelo jogador: movimento com Rigidbody2D.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movimento")]
    public float speed = 6f;
    public float acceleration = 50f;

    Rigidbody2D rb;
    Vector2 movement;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        movement = GameInput.Move.ReadValue<Vector2>();
    }

    void FixedUpdate()
    {
        // Tutorial (Movimentando o Jogador): a física move o Rigidbody2D no FixedUpdate.
        Vector2 target = movement * speed;
        rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, target, acceleration * Time.fixedDeltaTime);
    }
}
