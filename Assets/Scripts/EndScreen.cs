using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Tela de fim de jogo: mostra se ganhou ou perdeu, o tempo final, a pontuação e os recordes.
public class EndScreen : MonoBehaviour
{
    public Text titleText;
    public Text reasonText;
    public Text statsText;
    public Text recordText;
    public Image heroImage;
    public Sprite winSprite;
    public Sprite loseSprite;
    public Button playAgainButton;
    public Button menuButton;

    void Start()
    {
        Time.timeScale = 1f;
        AudioManager.PlayMusic("musica_menu");

        bool won = GameController.Won;
        titleText.text = won ? "VITÓRIA!" : "FIM DE JOGO";
        titleText.color = won ? new Color(1f, 0.82f, 0.2f) : new Color(1f, 0.45f, 0.4f);
        reasonText.text = GameController.EndReason;
        statsText.text =
            "Queijos: " + GameController.CheeseCollected + "/" + GameController.CheeseTotal + "\n" +
            "Tempo final: " + GameController.FormatTime(GameController.ElapsedTime) + "\n" +
            "Pontuação: " + GameController.Score;

        string best = GameController.BestTime > 0f ? GameController.FormatTime(GameController.BestTime) : "--:--.-";
        recordText.text = (GameController.NewRecord ? "NOVO RECORDE!\n" : "") +
                          "Recorde: " + GameController.BestScore + " pts  |  Melhor tempo: " + best;

        if (heroImage != null) heroImage.sprite = won ? winSprite : loseSprite;

        playAgainButton.onClick.AddListener(PlayAgain);
        menuButton.onClick.AddListener(ToMenu);
        StartCoroutine(UnlockButtons());
    }

    // trava os botões por um instante: quem estava apertando A/Enter no fim da partida não pula a tela sem querer
    IEnumerator UnlockButtons()
    {
        playAgainButton.interactable = false;
        menuButton.interactable = false;
        yield return new WaitForSecondsRealtime(0.8f);
        playAgainButton.interactable = true;
        menuButton.interactable = true;
    }

    void Update()
    {
        if (GameInput.MutePressed()) AudioManager.ToggleMute();
    }

    void PlayAgain()
    {
        AudioManager.PlaySfx("clique", 0.8f, 0f);
        SceneManager.LoadScene("Game");
    }

    void ToMenu()
    {
        AudioManager.PlaySfx("clique", 0.8f, 0f);
        SceneManager.LoadScene("MainMenu");
    }
}
