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
    public enum State { Countdown, Playing, Paused, Won, Lost }

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

    [Header("Sons")]
    public AudioClip countdownClip;
    public AudioClip goClip;
    public AudioClip unlockClip;
    public AudioClip alarmClip;
    public AudioClip victoryClip;
    public AudioClip gameOverClip;

    public State CurrentState { get; private set; }
    public float ElapsedTime { get; private set; }
    public float Oxygen { get; private set; }
    public int Score { get; private set; }
    public int CrystalsCollected { get; private set; }
    public int TotalCrystals { get; private set; }

    float alarmTimer;
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

        StartCoroutine(CountdownRoutine());
    }

    IEnumerator CountdownRoutine()
    {
        CurrentState = State.Countdown;
        for (int i = 3; i > 0; i--)
        {
            hud.ShowCountdown(i.ToString());
            AudioManager.PlaySfx(countdownClip);
            yield return new WaitForSeconds(0.7f);
        }

        hud.ShowCountdown("VAI!");
        AudioManager.PlaySfx(goClip);
        CurrentState = State.Playing;
        hud.ShowMessage($"COLETE OS {TotalCrystals} CRISTAIS!", 2.5f);

        yield return new WaitForSeconds(0.7f);
        hud.ShowCountdown(null);
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

        if (Oxygen <= lowOxygen)
        {
            alarmTimer -= Time.deltaTime;
            if (alarmTimer <= 0f)
            {
                AudioManager.PlaySfx(alarmClip, 0.6f);
                alarmTimer = 1f;
            }
        }

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
            AudioManager.PlaySfx(unlockClip);
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
        AudioManager.StopMusic();
        AudioManager.PlaySfx(victoryClip);

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
        AudioManager.StopMusic();
        AudioManager.PlaySfx(gameOverClip);

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
        AudioManager.DuckMusic(true);
        hud.ShowPause(true);
    }

    public void Resume()
    {
        if (CurrentState != State.Paused) return;
        CurrentState = State.Playing;
        Time.timeScale = 1f;
        AudioManager.DuckMusic(false);
        hud.ShowPause(false);
    }

    public void Restart()
    {
        StartCoroutine(LoadSceneRoutine(SceneManager.GetActiveScene().name));
    }

    public void GoToMenu()
    {
        StartCoroutine(LoadSceneRoutine(MenuScene));
    }

    static IEnumerator LoadSceneRoutine(string scene)
    {
        // pequena espera para o som do clique tocar antes de trocar de cena
        yield return new WaitForSecondsRealtime(0.12f);
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
