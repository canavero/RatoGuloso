using UnityEngine;

// Gato laranja: anda de um ponto a outro (patrulha) em caminhos pré-definidos.
// Usa Rigidbody2D Kinematic + Collider2D trigger: atravessa o rato causando dano (componente Hazard).
[RequireComponent(typeof(Rigidbody2D))]
public class EnemyPatrol : MonoBehaviour
{
    [Tooltip("Pontos da patrulha em coordenadas do mundo.")]
    public Vector2[] points;
    public float speed = 2.2f;
    public float waitTime = 0.5f;
    [Tooltip("true = vai e volta (A-B-A-B). false = volta ao primeiro ponto (A-B-C-A).")]
    public bool pingPong = true;
    public Transform visual;

    Rigidbody2D rb;
    int index;
    int direction = 1;
    float wait;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        if (visual == null) visual = transform;
    }

    void Start()
    {
        if (points == null || points.Length == 0) points = new[] { (Vector2)transform.position };
        transform.position = points[0];
        rb.position = points[0];
        index = points.Length > 1 ? 1 : 0;
    }

    void FixedUpdate()
    {
        if (!GameManager.IsPlaying || points.Length < 2) return;
        if (wait > 0f)
        {
            wait -= Time.fixedDeltaTime;
            return;
        }

        Vector2 target = points[index];
        float step = speed * GameManager.DifficultyMultiplier * Time.fixedDeltaTime;
        Vector2 next = Vector2.MoveTowards(rb.position, target, step);
        Face(next - rb.position);
        rb.MovePosition(next);

        if ((next - target).sqrMagnitude < 0.0001f)
        {
            wait = waitTime;
            Advance();
        }
    }

    void Advance()
    {
        if (pingPong)
        {
            if (index + direction >= points.Length || index + direction < 0) direction = -direction;
            index += direction;
        }
        else
        {
            index = (index + 1) % points.Length;
        }
    }

    void Face(Vector2 delta)
    {
        if (delta.sqrMagnitude < 0.000001f) return;
        float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
        visual.rotation = Quaternion.Slerp(visual.rotation, Quaternion.Euler(0f, 0f, angle), 10f * Time.fixedDeltaTime);
    }

    // Mostra o caminho da patrulha na janela Scene
    void OnDrawGizmosSelected()
    {
        if (points == null || points.Length < 2) return;
        Gizmos.color = new Color(1f, 0.6f, 0.1f);
        for (int i = 0; i < points.Length - 1; i++) Gizmos.DrawLine(points[i], points[i + 1]);
        if (!pingPong) Gizmos.DrawLine(points[points.Length - 1], points[0]);
    }
}
