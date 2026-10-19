using System.Collections;
using UnityEngine;

/// <summary>
/// Astronauta controlado pelo jogador: movimento com Rigidbody2D (corrigindo a diagonal),
/// dash que gasta stamina, vida, coleta de itens por trigger e dano ao tocar inimigos.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movimento")]
    public float speed = 6f;
    public float acceleration = 50f;

    [Header("Dash (gasta stamina)")]
    public float dashSpeed = 16f;
    public float dashDuration = 0.18f;
    public float dashCost = 35f;
    public float maxStamina = 100f;
    public float staminaRegen = 25f;

    [Header("Vida")]
    public int maxHealth = 3;
    public float invulnerableTime = 1.5f;
    public float knockback = 10f;

    [Header("Referências")]
    public Transform visual;
    public SpriteRenderer body;
    public AudioSource audioSource;
    public AudioClip collectClip;

    public int Health { get; private set; }
    public float Stamina { get; private set; }
    public bool IsDashing => dashTimer > 0f;

    Rigidbody2D rb;
    Vector2 movement;
    Vector2 facing = Vector2.right;
    float dashTimer;
    float invulnerableTimer;
    float knockbackTimer;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        Health = maxHealth;
        Stamina = maxStamina;
    }

    void Update()
    {
        if (!GameManager.IsPlaying)
        {
            movement = Vector2.zero;
            return;
        }

        // Tutorial (Movimentação Diagonal): o vetor nunca passa de tamanho 1, então andar na
        // diagonal não fica mais rápido. ClampMagnitude mantém a sensibilidade do analógico.
        movement = Vector2.ClampMagnitude(GameInput.Move.ReadValue<Vector2>(), 1f);
        if (movement.sqrMagnitude > 0.01f) facing = movement.normalized;

        if (GameInput.Dash.WasPressedThisFrame()) TryDash();
        if (!IsDashing) Stamina = Mathf.Min(maxStamina, Stamina + staminaRegen * Time.deltaTime);

        if (invulnerableTimer > 0f)
        {
            invulnerableTimer -= Time.deltaTime;
            body.enabled = invulnerableTimer <= 0f || Mathf.Repeat(invulnerableTimer, 0.16f) > 0.08f;
        }
    }

    void FixedUpdate()
    {
        if (!GameManager.IsPlaying)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        if (IsDashing)
        {
            dashTimer -= Time.fixedDeltaTime;
            rb.linearVelocity = facing * dashSpeed;
            return;
        }

        if (knockbackTimer > 0f)
        {
            knockbackTimer -= Time.fixedDeltaTime;
            return;
        }

        // Tutorial (Movimentando o Jogador): a física move o Rigidbody2D no FixedUpdate.
        Vector2 target = movement * speed;
        rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, target, acceleration * Time.fixedDeltaTime);
    }

    void TryDash()
    {
        if (IsDashing || Stamina < dashCost) return;

        Stamina -= dashCost;
        dashTimer = dashDuration;
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
        else if (other.CompareTag("Saida"))
        {
            GameManager.Instance.ReachExit();
        }
        else if (other.CompareTag("Inimigo"))
        {
            TakeDamage(other);
        }
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (GameManager.IsPlaying && other.CompareTag("Inimigo")) TakeDamage(other);
    }

    void TakeDamage(Collider2D source)
    {
        // Durante o dash o astronauta fica invulnerável por um instante.
        if (invulnerableTimer > 0f || IsDashing || Health <= 0) return;

        Health--;
        invulnerableTimer = invulnerableTime;

        Vector2 away = rb.position - source.ClosestPoint(rb.position);
        if (away.sqrMagnitude < 0.0001f) away = rb.position - (Vector2)source.bounds.center;
        if (away.sqrMagnitude < 0.0001f) away = -facing;
        rb.linearVelocity = away.normalized * knockback;
        knockbackTimer = 0.2f;

        GameManager.Instance.PlayerHurt(Health);
    }

    /// <summary>Chamado pelo GameManager na derrota.</summary>
    public void Die()
    {
        StopPhysics();
        StartCoroutine(ShrinkRoutine(transform.position));
    }

    /// <summary>Chamado pelo GameManager na vitória: o astronauta entra na cápsula.</summary>
    public void Escape(Vector3 podPosition)
    {
        StopPhysics();
        StartCoroutine(ShrinkRoutine(podPosition));
    }

    void StopPhysics()
    {
        rb.linearVelocity = Vector2.zero;
        rb.simulated = false;
        body.enabled = true;
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
