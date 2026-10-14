using UnityEngine;
using UnityEngine.UI;

// Interface durante o jogo: tempo restante, cronômetro, pontos, queijos, vidas e stamina.
public class HUD : MonoBehaviour
{
    public PlayerMovement player;

    [Header("Textos")]
    public Text timeLeftText;    // contagem regressiva (grande)
    public Text stopwatchText;   // cronômetro (tempo total)
    public Text scoreText;
    public Text cheeseText;
    public Text bonusText;       // "+3s" que aparece embaixo do relógio
    public Text messageText;     // mensagens no meio da tela

    [Header("Vidas e stamina")]
    public Image[] hearts;
    public RectTransform staminaFill;
    public Image staminaFillImage;

    [Header("Cores")]
    public Color timeNormalColor = Color.white;
    public Color timeLowColor = new Color(1f, 0.35f, 0.3f);
    public Color heartFullColor = Color.white;
    public Color heartEmptyColor = new Color(0.2f, 0.2f, 0.2f, 0.45f);
    public Color staminaReadyColor = new Color(1f, 0.82f, 0.2f);
    public Color staminaLowColor = new Color(0.55f, 0.55f, 0.6f);

    float messageTimer;
    float bonusTimer;

    void Update()
    {
        float left = GameController.TimeLeft;
        bool low = left <= 10f;
        timeLeftText.text = GameController.FormatCountdown(left);
        timeLeftText.color = low ? timeLowColor : timeNormalColor;
        float pulse = low ? 1f + 0.12f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 6f)) : 1f;
        timeLeftText.rectTransform.localScale = new Vector3(pulse, pulse, 1f);

        stopwatchText.text = "Cronômetro " + GameController.FormatTime(GameController.ElapsedTime);
        scoreText.text = "Pontos " + GameController.Score;
        cheeseText.text = GameController.CheeseCollected + "/" + GameController.CheeseTotal;

        int lives = player != null ? player.Lives : GameController.Lives;
        for (int i = 0; i < hearts.Length; i++)
            hearts[i].color = i < lives ? heartFullColor : heartEmptyColor;

        if (player != null && staminaFill != null)
        {
            float f = Mathf.Clamp01(player.Stamina / player.maxStamina);
            staminaFill.anchorMax = new Vector2(f, 1f);
            if (staminaFillImage != null)
                staminaFillImage.color = player.Stamina >= player.dashCost ? staminaReadyColor : staminaLowColor;
        }

        Fade(messageText, ref messageTimer);
        Fade(bonusText, ref bonusTimer);
    }

    static void Fade(Text text, ref float timer)
    {
        if (text == null) return;
        timer -= Time.unscaledDeltaTime;
        Color c = text.color;
        c.a = Mathf.Clamp01(timer / 0.4f);
        text.color = c;
    }

    public void ShowMessage(string message, float duration)
    {
        if (messageText == null) return;
        messageText.text = message;
        messageTimer = duration;
    }

    public void ShowBonus(string text)
    {
        if (bonusText == null) return;
        bonusText.text = text;
        bonusTimer = 1f;
    }
}
