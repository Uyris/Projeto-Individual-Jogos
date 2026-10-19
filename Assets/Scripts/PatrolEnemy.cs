using UnityEngine;

/// <summary>
/// Drone que percorre uma rota pré-determinada (vai e volta ou em circuito).
/// Causa dano pelo trigger com a tag "Inimigo".
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PatrolEnemy : MonoBehaviour
{
    public Vector2[] waypoints;
    public float speed = 3f;
    public float waitTime = 0.25f;
    [Tooltip("Ligado: vai e volta. Desligado: circuito fechado.")]
    public bool pingPong = true;

    Rigidbody2D rb;
    int index;
    int direction = 1;
    float waitTimer;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Start()
    {
        if (waypoints == null || waypoints.Length == 0) return;
        rb.position = waypoints[0];
        index = Mathf.Min(1, waypoints.Length - 1);
    }

    void FixedUpdate()
    {
        if (!GameManager.IsPlaying || waypoints == null || waypoints.Length < 2) return;

        if (waitTimer > 0f)
        {
            waitTimer -= Time.fixedDeltaTime;
            return;
        }

        Vector2 target = waypoints[index];
        Vector2 next = Vector2.MoveTowards(rb.position, target, speed * Time.fixedDeltaTime);
        rb.MovePosition(next);

        if ((next - target).sqrMagnitude < 0.0001f)
        {
            waitTimer = waitTime;
            Advance();
        }
    }

    void Advance()
    {
        if (!pingPong)
        {
            index = (index + 1) % waypoints.Length;
            return;
        }

        if (index + direction >= waypoints.Length || index + direction < 0) direction = -direction;
        index += direction;
    }

    void OnDrawGizmos()
    {
        if (waypoints == null || waypoints.Length < 2) return;
        Gizmos.color = new Color(1f, 0.4f, 0.3f, 0.8f);
        for (int i = 0; i < waypoints.Length - 1; i++) Gizmos.DrawLine(waypoints[i], waypoints[i + 1]);
        if (!pingPong) Gizmos.DrawLine(waypoints[waypoints.Length - 1], waypoints[0]);
    }
}
