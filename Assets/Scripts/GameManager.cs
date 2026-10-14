using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Regras da fase: cronômetro, contagem regressiva, vitória/derrota, pausa e dificuldade.
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public static bool IsPlaying => Instance != null && Instance.state == State.Playing;
    public static float DifficultyMultiplier => Instance != null ? Instance.difficulty : 1f;
    // evita que o botão usado para fechar o menu de pausa também dispare um dash
    public static bool InputReady => Instance == null || Time.unscaledTime - Instance.resumeTime > 0.2f;

    [Header("Tempo")]
    public float startTime = 50f;
    public float lowTimeWarning = 10f;

    [Header("Dificuldade: os gatos ficam mais rápidos com o passar do tempo")]
    public float difficultyPerMinute = 0.35f;
    public float maxDifficulty = 1.6f;

    [Header("Arena (área interna, usada pela NavGrid e pelo anti-softlock)")]
    public Vector2 arenaMin = new Vector2(-9.5f, -5.5f);
    public Vector2 arenaMax = new Vector2(9.5f, 4.5f);
    public float navCellSize = 0.5f;
    public float navClearance = 0.38f;

    [Header("Referências")]
    public PlayerMovement player;
    public Transform playerSpawn;
    public HUD hud;
    public GameObject pausePanel;
    public Button resumeButton;
    public Button restartButton;
    public Button menuButton;

    enum State { Playing, Paused, Ended }

    State state = State.Playing;
    float difficulty = 1f;
    float resumeTime = -10f;
    int lastTickSecond = -1;

    void Awake()
    {
        Instance = this;
        Time.timeScale = 1f;
    }

    void Start()
    {
        int cheeseCount = 0;
        foreach (Collectible c in Collectible.All)
            if (c.kind == Collectible.Kind.Cheese) cheeseCount++;

        GameController.StartRun(cheeseCount, startTime, player != null ? player.maxLives : 3);
        NavGrid.Build(arenaMin, arenaMax, navCellSize, navClearance);

        if (pausePanel != null) pausePanel.SetActive(false);
        if (resumeButton != null) resumeButton.onClick.AddListener(Resume);
        if (restartButton != null) restartButton.onClick.AddListener(Restart);
        if (menuButton != null) menuButton.onClick.AddListener(GoToMenu);

        AudioManager.PlayMusic("musica_jogo");
        if (hud != null) hud.ShowMessage("Coma todos os queijos!", 2.5f);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        Time.timeScale = 1f;
    }

    // Clicou fora do jogo (itch.io) ou trocou de janela: pausa para o tempo não correr sozinho
    void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus) Pause();
    }

    void OnApplicationPause(bool paused)
    {
        if (paused) Pause();
    }

    void Update()
    {
        if (GameInput.MutePressed()) AudioManager.ToggleMute();
        if (state == State.Ended) return;

        if (GameInput.PausePressed())
        {
            if (state == State.Paused) Resume();
            else Pause();
        }
        if (state != State.Playing) return;

        // Cronômetro e contagem regressiva
        float dt = Time.deltaTime;
        GameController.ElapsedTime += dt;
        GameController.TimeLeft -= dt;
        difficulty = Mathf.Min(maxDifficulty, 1f + GameController.ElapsedTime / 60f * difficultyPerMinute);

        // bipe a cada segundo quando o tempo está acabando
        if (GameController.TimeLeft <= lowTimeWarning)
        {
            int sec = Mathf.CeilToInt(GameController.TimeLeft);
            if (sec != lastTickSecond && sec > 0)
            {
                lastTickSecond = sec;
                AudioManager.PlaySfx("tick", 0.6f, 0f);
            }
        }

        if (GameController.TimeLeft <= 0f)
        {
            GameController.TimeLeft = 0f;
            EndGame(false, "O tempo acabou!");
            return;
        }

        // Anti-softlock: se o rato escapar da arena por algum erro de física, volta para o início
        if (player != null && playerSpawn != null)
        {
            Vector2 p = player.transform.position;
            if (p.x < arenaMin.x - 1f || p.x > arenaMax.x + 1f || p.y < arenaMin.y - 1f || p.y > arenaMax.y + 1f)
                player.Teleport(playerSpawn.position);
        }
    }

    public void AddCheese(int points, float timeBonus)
    {
        if (state == State.Ended) return;
        GameController.Score += points;
        GameController.CheeseCollected++;
        GameController.TimeLeft += timeBonus;
        if (hud != null) hud.ShowBonus("+" + timeBonus.ToString("0") + "s");

        if (GameController.CheeseCollected >= GameController.CheeseTotal)
            EndGame(true, "Você comeu todos os queijos!");
    }

    public void AddTime(float seconds, int points)
    {
        if (state == State.Ended) return;
        GameController.TimeLeft += seconds;
        GameController.Score += points;
        if (hud != null)
        {
            hud.ShowBonus("+" + seconds.ToString("0") + "s");
            hud.ShowMessage("Tempo extra!", 1f);
        }
    }

    public void OnPlayerHit(int livesLeft)
    {
        GameController.Lives = livesLeft;
        if (livesLeft <= 0) EndGame(false, "Os gatos pegaram você!");
        else if (hud != null) hud.ShowMessage(livesLeft == 1 ? "Última vida!" : "Ai!", 1f);
    }

    void EndGame(bool won, string reason)
    {
        if (state == State.Ended) return;
        state = State.Ended;
        Time.timeScale = 1f;
        if (pausePanel != null) pausePanel.SetActive(false);

        GameController.Won = won;
        GameController.EndReason = reason;
        if (won) GameController.Score += Mathf.CeilToInt(GameController.TimeLeft) * 5; // bônus pelo tempo que sobrou
        GameController.SaveRecords();

        if (player != null) player.CanControl = false;
        AudioManager.StopMusic();
        AudioManager.PlaySfx(won ? "vitoria" : "derrota", 1f, 0f);
        if (hud != null) hud.ShowMessage(won ? "VITÓRIA!" : reason, 2f);
        StartCoroutine(LoadEndScene());
    }

    IEnumerator LoadEndScene()
    {
        yield return new WaitForSecondsRealtime(2f);
        SceneManager.LoadScene("GameOver");
    }

    public void Pause()
    {
        if (state != State.Playing) return;
        state = State.Paused;
        Time.timeScale = 0f;
        if (pausePanel != null) pausePanel.SetActive(true);
        AudioManager.PlaySfx("clique", 0.8f, 0f);
    }

    public void Resume()
    {
        if (state != State.Paused) return;
        state = State.Playing;
        Time.timeScale = 1f;
        resumeTime = Time.unscaledTime;
        if (pausePanel != null) pausePanel.SetActive(false);
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        AudioManager.PlaySfx("clique", 0.8f, 0f);
    }

    public void Restart()
    {
        Time.timeScale = 1f;
        AudioManager.PlaySfx("clique", 0.8f, 0f);
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void GoToMenu()
    {
        Time.timeScale = 1f;
        AudioManager.PlaySfx("clique", 0.8f, 0f);
        SceneManager.LoadScene("MainMenu");
    }
}
