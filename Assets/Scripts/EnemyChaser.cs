using System.Collections.Generic;
using UnityEngine;

// Gato preto: patrulha a cozinha e, quando vê o rato por perto, PERSEGUE usando a NavGrid
// (contorna os balcões). Se o rato fugir para longe, ele desiste e volta a patrulhar.
[RequireComponent(typeof(Rigidbody2D))]
public class EnemyChaser : MonoBehaviour
{
    public float patrolSpeed = 2f;
    public float chaseSpeed = 3.4f;
    public float detectRadius = 5f;
    public float loseRadius = 8f;
    [Tooltip("Segundos parado no início da fase (vantagem para o jogador).")]
    public float startDelay = 4f;
    public Vector2[] patrolPoints;

    [Header("Referências")]
    [Tooltip("Collider sólido que bate nas paredes (ele ignora o rato; o dano vem do collider trigger).")]
    public Collider2D solidCollider;
    public Transform visual;
    public SpriteRenderer alertIcon;

    Rigidbody2D rb;
    Transform player;
    List<Vector2> path = new List<Vector2>();
    int pathIndex;
    int patrolIndex;
    float repathTimer;
    float alertTimer;
    float stunTimer;
    bool chasing;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        if (visual == null) visual = transform;
        if (alertIcon != null) alertIcon.enabled = false;
    }

    void Start()
    {
        PlayerMovement pm = Compat.FindFirst<PlayerMovement>();
        if (pm != null)
        {
            player = pm.transform;
            Collider2D playerCollider = pm.GetComponent<Collider2D>();
            if (playerCollider != null && solidCollider != null)
                Physics2D.IgnoreCollision(solidCollider, playerCollider, true);
        }
        if (patrolPoints == null || patrolPoints.Length == 0) patrolPoints = new[] { (Vector2)transform.position };
    }

    void Update()
    {
        if (alertIcon == null) return;
        alertTimer -= Time.deltaTime;
        alertIcon.enabled = alertTimer > 0f;
    }

    void FixedUpdate()
    {
        if (!GameManager.IsPlaying || startDelay > 0f)
        {
            if (GameManager.IsPlaying) startDelay -= Time.fixedDeltaTime;
            rb.SetVelocity(Vector2.zero);
            return;
        }

        if (stunTimer > 0f)
        {
            stunTimer -= Time.fixedDeltaTime;
            rb.SetVelocity(Vector2.zero);
            return;
        }

        UpdateState();

        Vector2 goal;
        if (chasing)
        {
            goal = player.position;
        }
        else
        {
            goal = patrolPoints[patrolIndex];
            if (Vector2.Distance(rb.position, goal) < 0.35f)
            {
                patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
                goal = patrolPoints[patrolIndex];
                repathTimer = 0f;
            }
        }

        repathTimer -= Time.fixedDeltaTime;
        if (repathTimer <= 0f)
        {
            repathTimer = chasing ? 0.25f : 0.75f;
            path = NavGrid.FindPath(rb.position, goal);
            pathIndex = path.Count > 1 ? 1 : 0; // o primeiro ponto é a célula onde ele já está
        }

        Vector2 target = NextWaypoint(goal);
        float speed = (chasing ? chaseSpeed : patrolSpeed) * GameManager.DifficultyMultiplier;
        Vector2 dir = target - rb.position;
        Vector2 desired = dir.sqrMagnitude > 0.0004f ? dir.normalized * speed : Vector2.zero;
        rb.SetVelocity(Vector2.MoveTowards(rb.GetVelocity(), desired, 30f * Time.fixedDeltaTime));
        Face(desired);
    }

    public void Stun(float seconds)
    {
        stunTimer = Mathf.Max(stunTimer, seconds);
    }

    void UpdateState()
    {
        if (player == null)
        {
            chasing = false;
            return;
        }
        float dist = Vector2.Distance(rb.position, player.position);
        if (!chasing && dist < detectRadius)
        {
            chasing = true;
            repathTimer = 0f;
            alertTimer = 1.2f;
            AudioManager.PlaySfx("miau", 0.9f);
        }
        else if (chasing && dist > loseRadius)
        {
            chasing = false;
            repathTimer = 0f;
        }
    }

    Vector2 NextWaypoint(Vector2 goal)
    {
        if (path == null || path.Count == 0) return goal;
        while (pathIndex < path.Count - 1 && Vector2.Distance(rb.position, path[pathIndex]) < 0.2f) pathIndex++;
        // última célula do caminho: vai direto ao objetivo
        return pathIndex >= path.Count - 1 ? goal : path[pathIndex];
    }

    void Face(Vector2 velocity)
    {
        if (velocity.sqrMagnitude < 0.01f) return;
        float angle = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg;
        visual.rotation = Quaternion.Slerp(visual.rotation, Quaternion.Euler(0f, 0f, angle), 10f * Time.fixedDeltaTime);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectRadius);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, loseRadius);
        if (path == null) return;
        Gizmos.color = Color.cyan;
        for (int i = 0; i < path.Count - 1; i++) Gizmos.DrawLine(path[i], path[i + 1]);
    }
}
