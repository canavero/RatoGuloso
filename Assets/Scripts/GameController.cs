using UnityEngine;

// Guarda os dados da partida entre as cenas (classe estática: não precisa estar em nenhum GameObject).
// A cena do jogo escreve aqui e a cena de fim de jogo lê daqui.
public static class GameController
{
    public static int Score;
    public static int CheeseCollected;
    public static int CheeseTotal;
    public static int Lives;
    public static float ElapsedTime;   // cronômetro: tempo total da partida
    public static float TimeLeft;      // contagem regressiva (mecânica de tempo)
    public static bool Won;
    public static string EndReason = "";
    public static bool NewRecord;

    const string BestScoreKey = "RatoGuloso_BestScore";
    const string BestTimeKey = "RatoGuloso_BestTime";

    public static int BestScore => PlayerPrefs.GetInt(BestScoreKey, 0);
    public static float BestTime => PlayerPrefs.GetFloat(BestTimeKey, 0f); // 0 = ainda não venceu

    public static void StartRun(int cheeseTotal, float startTime, int lives)
    {
        Score = 0;
        CheeseCollected = 0;
        CheeseTotal = cheeseTotal;
        Lives = lives;
        ElapsedTime = 0f;
        TimeLeft = startTime;
        Won = false;
        EndReason = "";
        NewRecord = false;
    }

    public static void SaveRecords()
    {
        NewRecord = false;
        if (Score > BestScore)
        {
            PlayerPrefs.SetInt(BestScoreKey, Score);
            NewRecord = true;
        }
        if (Won && (BestTime <= 0f || ElapsedTime < BestTime))
        {
            PlayerPrefs.SetFloat(BestTimeKey, ElapsedTime);
            NewRecord = true;
        }
        PlayerPrefs.Save();
    }

    // 83.4 -> "01:23.4"
    public static string FormatTime(float seconds)
    {
        int tenths = Mathf.RoundToInt(Mathf.Max(0f, seconds) * 10f);
        int min = tenths / 600;
        int sec = tenths % 600 / 10;
        return min.ToString("00") + ":" + sec.ToString("00") + "." + (tenths % 10);
    }

    // 59.2 -> "1:00" (arredonda para cima, como um relógio de contagem regressiva)
    public static string FormatCountdown(float seconds)
    {
        int total = Mathf.CeilToInt(Mathf.Max(0f, seconds));
        return (total / 60) + ":" + (total % 60).ToString("00");
    }
}
