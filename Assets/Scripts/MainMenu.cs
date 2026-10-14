using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Menu inicial: Jogar, Como Jogar e Sair (o botão Sair some na versão WebGL, onde não faz sentido).
public class MainMenu : MonoBehaviour
{
    public GameObject mainPanel;
    public GameObject howToPanel;
    public Button playButton;
    public Button howToButton;
    public Button quitButton;
    public Button backButton;
    public Text recordText;

    void Start()
    {
        Time.timeScale = 1f;
        AudioManager.PlayMusic("musica_menu");

        playButton.onClick.AddListener(Play);
        howToButton.onClick.AddListener(ShowHowTo);
        quitButton.onClick.AddListener(Quit);
        backButton.onClick.AddListener(ShowMain);
#if UNITY_WEBGL && !UNITY_EDITOR
        quitButton.gameObject.SetActive(false);
#endif

        if (recordText != null)
        {
            string best = GameController.BestTime > 0f ? GameController.FormatTime(GameController.BestTime) : "--:--.-";
            recordText.text = "Recorde: " + GameController.BestScore + " pts   |   Melhor tempo: " + best;
        }
        ShowMain();
    }

    void Update()
    {
        if (GameInput.MutePressed()) AudioManager.ToggleMute();
        if (howToPanel.activeSelf && GameInput.PausePressed()) ShowMain();
    }

    void Play()
    {
        AudioManager.PlaySfx("clique", 0.8f, 0f);
        SceneManager.LoadScene("Game");
    }

    void ShowHowTo()
    {
        AudioManager.PlaySfx("clique", 0.8f, 0f);
        mainPanel.SetActive(false);
        howToPanel.SetActive(true);
    }

    void ShowMain()
    {
        howToPanel.SetActive(false);
        mainPanel.SetActive(true);
    }

    void Quit()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
