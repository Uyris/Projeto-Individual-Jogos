using UnityEngine;

/// <summary>
/// Alienígena com comportamento variável: patrulha seus pontos, persegue o jogador quando
/// o vê (distância + linha de visão) e procura na última posição conhecida ao perdê-lo.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class ChaserEnemy : MonoBehaviour
{
    enum Mode { Patrol, Chase, Search }

    public Vector2[] patrolPoints;
    public float patrolSpeed = 1.8f;
    public float chaseSpeed = 3.6f;
    public float acceleration = 12f;
    public float detectRadius = 6f;
    public float loseRadius = 9f;
    public float searchTime = 3f;

    [Header("Referências")]
    public Collider2D bodyCollider;
    public SpriteRenderer body;
    public GameObject alertIcon;
    public AudioClip alertClip;

    static readonly RaycastHit2D[] Hits = new RaycastHit2D[8];
    static readonly Color ChaseTint = new Color(1f, 0.55f, 0.55f);

    Rigidbody2D rb;
    Transform player;
    Mode mode;
    int patrolIndex;
    Vector2 lastSeen;
    float searchTimer;
    float stuckTimer;
    Vector3 baseScale;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        baseScale = body.transform.localScale;
    }

    void Start()
    {
        PlayerController p = GameManager.Instance.player;
        player = p.transform;
        // O corpo colide com as paredes mas não empurra o jogador; o dano vem do trigger "Inimigo".
        Physics2D.IgnoreCollision(bodyCollider, p.GetComponent<Collider2D>());
        alertIcon.SetActive(false);
    }

    void FixedUpdate()
    {
        if (!GameManager.IsPlaying)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 pos = rb.position;
        Vector2 playerPos = player.position;
        float range = mode == Mode.Chase ? loseRadius : detectRadius;
        bool seesPlayer = (playerPos - pos).magnitude <= range && HasLineOfSight(pos, playerPos);

        if (seesPlayer)
        {
            if (mode != Mode.Chase) AudioManager.PlaySfx(alertClip, 0.7f);
            mode = Mode.Chase;
            lastSeen = playerPos;
        }
        else if (mode == Mode.Chase)
        {
            mode = Mode.Search;
            searchTimer = searchTime;
        }

        Vector2 target = pos;
        float speed = 0f;
        switch (mode)
        {
            case Mode.Chase:
                target = lastSeen;
                speed = chaseSpeed;
                break;

            case Mode.Search:
                // vai até onde viu o jogador pela última vez e espera um pouco
                searchTimer -= Time.fixedDeltaTime;
                if ((lastSeen - pos).sqrMagnitude > 0.09f)
                {
                    target = lastSeen;
                    speed = patrolSpeed * 1.4f;
                }
                if (searchTimer <= 0f) mode = Mode.Patrol;
                break;

            default:
                if (patrolPoints != null && patrolPoints.Length > 0)
                {
                    target = patrolPoints[patrolIndex];
                    speed = patrolSpeed;
                    if ((target - pos).sqrMagnitude < 0.04f) NextPatrolPoint();
                }
                break;
        }

        Vector2 desired = (target - pos).sqrMagnitude > 0.0025f ? (target - pos).normalized * speed : Vector2.zero;
        rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, desired, acceleration * Time.fixedDeltaTime);

        // Se ficar preso em uma parede, desiste do ponto atual.
        bool stuck = speed > 0f && rb.linearVelocity.sqrMagnitude < 0.04f;
        stuckTimer = stuck ? stuckTimer + Time.fixedDeltaTime : 0f;
        if (stuckTimer > 1f)
        {
            stuckTimer = 0f;
            if (mode == Mode.Search) mode = Mode.Patrol;
            else if (mode == Mode.Patrol) NextPatrolPoint();
        }
    }

    void NextPatrolPoint()
    {
        patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
    }

    static bool HasLineOfSight(Vector2 from, Vector2 to)
    {
        ContactFilter2D filter = new ContactFilter2D { useTriggers = false };
        int count = Physics2D.Linecast(from, to, filter, Hits);
        for (int i = 0; i < count; i++)
            if (Hits[i].collider.CompareTag("Parede")) return false;
        return true;
    }

    void Update()
    {
        bool chasing = mode == Mode.Chase && GameManager.IsPlaying;
        alertIcon.SetActive(chasing);

        float wobble = Mathf.Sin(Time.time * (chasing ? 16f : 6f)) * 0.07f;
        body.transform.localScale = new Vector3(baseScale.x * (1f + wobble), baseScale.y * (1f - wobble), 1f);
        body.color = Color.Lerp(body.color, chasing ? ChaseTint : Color.white, Time.deltaTime * 6f);
        if (Mathf.Abs(rb.linearVelocity.x) > 0.1f) body.flipX = rb.linearVelocity.x < 0f;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectRadius);
        if (patrolPoints == null) return;
        Gizmos.color = Color.green;
        foreach (Vector2 p in patrolPoints) Gizmos.DrawWireSphere(p, 0.2f);
    }
}
