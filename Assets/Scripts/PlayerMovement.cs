using UnityEngine;

// Controla o rato (jogador): movimento com Rigidbody2D, dash com stamina, vidas,
// invencibilidade depois de levar dano e coleta de itens.
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movimento")]
    public float speed = 6f;
    public float acceleration = 60f;

    [Header("Dash (gasta stamina)")]
    public float dashSpeed = 15f;
    public float dashDuration = 0.18f;
    public float maxStamina = 100f;
    public float dashCost = 40f;
    public float staminaRegen = 22f; // por segundo

    [Header("Vida")]
    public int maxLives = 3;
    public float invincibleTime = 1.5f;
    public float knockbackSpeed = 9f;
    public float knockbackDuration = 0.18f;

    [Header("Referências")]
    public Transform visual;              // filho com o sprite (gira para a direção do movimento)
    public SpriteRenderer spriteRenderer;

    public int Lives { get; private set; }
    public float Stamina { get; private set; }
    public bool IsDashing => dashTimer > 0f;
    public bool CanControl { get; set; } = true;

    Rigidbody2D rb;
    Vector2 input;
    Vector2 lastDir = Vector2.right;
    Vector2 knockbackDir;
    float dashTimer;
    float invincibleTimer;
    float knockbackTimer;
    Vector3 baseScale;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.sleepMode = RigidbodySleepMode2D.NeverSleep; // para o OnTriggerStay2D funcionar parado
        Lives = maxLives;
        Stamina = maxStamina;
        if (visual == null) visual = transform;
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        baseScale = visual.localScale;
    }

    void Update()
    {
        if (!CanControl || Time.timeScale == 0f)
        {
            input = Vector2.zero;
            if (spriteRenderer != null) spriteRenderer.enabled = true;
            return;
        }

        input = GameInput.Move();
        if (input.sqrMagnitude > 0.01f) lastDir = input.normalized;

        float dt = Time.deltaTime;
        dashTimer -= dt;
        invincibleTimer -= dt;
        knockbackTimer -= dt;
        if (!IsDashing) Stamina = Mathf.Min(maxStamina, Stamina + staminaRegen * dt);

        if (GameInput.DashPressed() && GameManager.InputReady && !IsDashing && Stamina >= dashCost)
            StartDash();

        // Visual: gira para onde está indo, pisca quando invencível e "estica" no dash
        float angle = Mathf.Atan2(lastDir.y, lastDir.x) * Mathf.Rad2Deg;
        visual.rotation = Quaternion.Slerp(visual.rotation, Quaternion.Euler(0f, 0f, angle), 15f * dt);
        if (spriteRenderer != null)
            spriteRenderer.enabled = invincibleTimer <= 0f || Mathf.Repeat(Time.time * 10f, 1f) > 0.4f;
        visual.localScale = IsDashing
            ? new Vector3(baseScale.x * 1.25f, baseScale.y * 0.8f, baseScale.z)
            : baseScale;
    }

    void FixedUpdate()
    {
        if (!CanControl)
        {
            rb.SetVelocity(Vector2.zero);
            return;
        }

        Vector2 velocity;
        if (knockbackTimer > 0f) velocity = knockbackDir * knockbackSpeed;
        else if (IsDashing) velocity = lastDir * dashSpeed;
        else velocity = Vector2.MoveTowards(rb.GetVelocity(), input * speed, acceleration * Time.fixedDeltaTime);
        rb.SetVelocity(velocity);
    }

    void StartDash()
    {
        Stamina -= dashCost;
        dashTimer = dashDuration;
        AudioManager.PlaySfx("dash", 0.7f);
    }

    // Retorna true se o dano foi aplicado (false se estava invencível).
    public bool TakeDamage(Vector2 sourcePosition)
    {
        if (invincibleTimer > 0f || Lives <= 0 || !CanControl) return false;

        Lives--;
        invincibleTimer = invincibleTime;
        knockbackTimer = knockbackDuration;
        dashTimer = 0f;
        Vector2 away = (Vector2)transform.position - sourcePosition;
        knockbackDir = away.sqrMagnitude > 0.0001f ? away.normalized : -lastDir;

        AudioManager.PlaySfx("dano");
        CameraController.Shake(0.25f, 0.25f);
        if (GameManager.Instance != null) GameManager.Instance.OnPlayerHit(Lives);
        return true;
    }

    // Usado pelo GameManager se o rato sair da arena por algum erro de física (anti-softlock).
    public void Teleport(Vector2 position)
    {
        rb.position = position;
        transform.position = position;
        rb.SetVelocity(Vector2.zero);
    }

    // ----------------------------------------------------------------- colisões
    void OnTriggerEnter2D(Collider2D other)
    {
        Collectible item = other.GetComponent<Collectible>();
        if (item != null)
        {
            item.Collect();
            return;
        }
        CheckHazard(other);
    }

    void OnTriggerStay2D(Collider2D other) => CheckHazard(other);
    void OnCollisionEnter2D(Collision2D collision) => CheckHazard(collision.collider);
    void OnCollisionStay2D(Collision2D collision) => CheckHazard(collision.collider);

    void CheckHazard(Collider2D other)
    {
        Hazard hazard = other.GetComponentInParent<Hazard>();
        if (hazard == null || !hazard.IsActive) return;
        if (!TakeDamage(other.transform.position)) return;
        hazard.OnHitPlayer();
        // o gato preto fica "satisfeito" por um instante depois de pegar o rato (dá tempo de fugir)
        EnemyChaser chaser = hazard.GetComponent<EnemyChaser>();
        if (chaser != null) chaser.Stun(1.2f);
    }
}
