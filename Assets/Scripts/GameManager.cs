using System.Collections;
using System.Globalization;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// GameManager estático (tutorial "End Game, Static GameManager"): controla o cronômetro,
/// o oxigênio (tempo limite), a pontuação, a pausa e o fim de jogo (vitória ou derrota).
/// </summary>
public class GameManager : MonoBehaviour
{
    public enum State { Playing, Paused, Won, Lost }

    public static GameManager Instance { get; private set; }
    public static bool IsPlaying => Instance != null && Instance.CurrentState == State.Playing;

    public const string BestTimeKey = "BestTime";
    public const string BestScoreKey = "BestScore";
    public const string MenuScene = "MainMenu";

    [Header("Oxigênio (mecânica de tempo)")]
    public float startOxygen = 60f;
    public float maxOxygen = 99f;
    public float lowOxygen = 15f;

    [Header("Bônus no final")]
    public int oxygenBonusPerSecond = 10;
    public int lifeBonus = 250;

    [Header("Referências")]
    public PlayerController player;
    public HUD hud;
    public ExitPod exitPod;

    public State CurrentState { get; private set; }
    public float ElapsedTime { get; private set; }
    public float Oxygen { get; private set; }
    public int Score { get; private set; }
    public int CrystalsCollected { get; private set; }
    public int TotalCrystals { get; private set; }

    float exitHintCooldown;

    void Awake()
    {
        Instance = this;
        Time.timeScale = 1f;
        Oxygen = startOxygen;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Start()
    {
        foreach (Collectible c in FindObjectsByType<Collectible>(FindObjectsSortMode.None))
            if (c.kind == Collectible.Kind.Crystal) TotalCrystals++;

        CurrentState = State.Playing;
    }

    void Update()
    {
        if (GameInput.Pause.WasPressedThisFrame())
        {
            if (CurrentState == State.Playing) Pause();
            else if (CurrentState == State.Paused) Resume();
        }

        exitHintCooldown -= Time.deltaTime;
        if (CurrentState != State.Playing) return;

        // Cronômetro da partida e oxigênio que acaba com o tempo.
        ElapsedTime += Time.deltaTime;
        Oxygen -= Time.deltaTime;

        if (Oxygen <= 0f)
        {
            Oxygen = 0f;
            Lose("SEU OXIGÊNIO ACABOU!");
        }
    }

    public void Collect(Collectible item)
    {
        Score += item.points;
        Oxygen = Mathf.Min(maxOxygen, Oxygen + item.oxygen);

        if (item.kind == Collectible.Kind.Oxygen)
        {
            hud.ShowMessage($"+{item.oxygen:0}s DE OXIGÊNIO", 1.5f);
            return;
        }

        CrystalsCollected++;
        if (CrystalsCollected >= TotalCrystals)
        {
            exitPod.Unlock();
            hud.ShowMessage("CÁPSULA DE FUGA LIBERADA!\nCORRA ATÉ ELA!", 3f);
        }
    }

    public void ReachExit()
    {
        if (CurrentState != State.Playing) return;

        if (exitPod.IsOpen)
        {
            Win();
        }
        else if (exitHintCooldown <= 0f)
        {
            int missing = TotalCrystals - CrystalsCollected;
            hud.ShowMessage($"CÁPSULA TRANCADA!\nFALTAM {missing} CRISTAIS", 2f);
            exitHintCooldown = 2.5f;
        }
    }

    public void PlayerHurt(int health)
    {
        if (health <= 0) Lose("VOCÊ FOI ATINGIDO DEMAIS!");
    }

    void Win()
    {
        CurrentState = State.Won;

        int oxygenBonus = Mathf.CeilToInt(Oxygen) * oxygenBonusPerSecond;
        int healthBonus = player.Health * lifeBonus;
        Score += oxygenBonus + healthBonus;

        float bestTime = PlayerPrefs.GetFloat(BestTimeKey, 0f);
        bool newRecord = bestTime <= 0f || ElapsedTime < bestTime;
        if (newRecord) bestTime = ElapsedTime;
        PlayerPrefs.SetFloat(BestTimeKey, bestTime);
        if (Score > PlayerPrefs.GetInt(BestScoreKey, 0)) PlayerPrefs.SetInt(BestScoreKey, Score);
        PlayerPrefs.Save();

        player.Escape(exitPod.transform.position);

        float time = ElapsedTime;
        int score = Score;
        StartCoroutine(EndRoutine(() => hud.ShowVictory(time, score, oxygenBonus, healthBonus, newRecord, bestTime)));
    }

    void Lose(string reason)
    {
        if (CurrentState == State.Lost || CurrentState == State.Won) return;
        CurrentState = State.Lost;

        player.Die();
        CameraController.Shake(0.4f, 0.4f);

        StartCoroutine(EndRoutine(() => hud.ShowGameOver(reason, ElapsedTime, Score, CrystalsCollected, TotalCrystals)));
    }

    IEnumerator EndRoutine(System.Action showPanel)
    {
        yield return new WaitForSecondsRealtime(1.1f);
        Time.timeScale = 0f;
        showPanel();
    }

    public void Pause()
    {
        if (CurrentState != State.Playing) return;
        CurrentState = State.Paused;
        Time.timeScale = 0f;
        hud.ShowPause(true);
    }

    public void Resume()
    {
        if (CurrentState != State.Paused) return;
        CurrentState = State.Playing;
        Time.timeScale = 1f;
        hud.ShowPause(false);
    }

    public void Restart()
    {
        LoadScene(SceneManager.GetActiveScene().name);
    }

    public void GoToMenu()
    {
        LoadScene(MenuScene);
    }

    static void LoadScene(string scene)
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(scene);
    }

    public static string FormatTime(float seconds)
    {
        int minutes = (int)(seconds / 60f);
        float rest = seconds - minutes * 60f;
        return minutes.ToString("00") + ":" + rest.ToString("00.00", CultureInfo.InvariantCulture);
    }
}
