using UnityEngine;

// Marca um objeto como perigoso para o rato (gatos e ratoeiras).
// O PlayerMovement procura este componente quando encosta em algo.
public class Hazard : MonoBehaviour
{
    [Tooltip("Ratoeira: tempo (s) que fica fechada/desarmada depois de pegar o rato. 0 = sempre ativo (gatos).")]
    public float rearmTime = 0f;
    public SpriteRenderer spriteRenderer;
    public Sprite armedSprite;
    public Sprite snappedSprite;

    float cooldown;

    public bool IsActive => cooldown <= 0f;

    public void OnHitPlayer()
    {
        if (rearmTime <= 0f) return;
        cooldown = rearmTime;
        AudioManager.PlaySfx("ratoeira", 1f, 0f);
        if (spriteRenderer != null && snappedSprite != null) spriteRenderer.sprite = snappedSprite;
    }

    void Update()
    {
        if (cooldown <= 0f) return;
        cooldown -= Time.deltaTime;
        if (cooldown <= 0f && spriteRenderer != null && armedSprite != null)
            spriteRenderer.sprite = armedSprite;
    }
}
