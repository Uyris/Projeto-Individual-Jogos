using System.Globalization;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// GameManager estático (tutorial "End Game, Static GameManager"): controla o cronômetro,
/// a pontuação e o fim de jogo.
/// </summary>
public class GameManager : MonoBehaviour
{
    public enum State { Playing, Won }

    public static GameManager Instance { get; private set; }
    public static bool IsPlaying => Instance != null && Instance.CurrentState == State.Playing;

    public const string MenuScene = "MainMenu";

    [Header("Referências")]
    public HUD hud;

    public State CurrentState { get; private set; }
    public float ElapsedTime { get; private set; }
    public int Score { get; private set; }
    public int CrystalsCollected { get; private set; }
    public int TotalCrystals { get; private set; }

    void Awake()
    {
        Instance = this;
        Time.timeScale = 1f;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Start()
    {
        foreach (Collectible c in FindObjectsByType<Collectible>(FindObjectsSortMode.None))
            TotalCrystals++;

        CurrentState = State.Playing;
    }

    void Update()
    {
        if (CurrentState != State.Playing) return;

        // Cronômetro da partida.
        ElapsedTime += Time.deltaTime;
    }

    public void Collect(Collectible item)
    {
        Score += item.points;

        CrystalsCollected++;
        if (CrystalsCollected >= TotalCrystals)
        {
            Win();
        }
    }

    void Win()
    {
        CurrentState = State.Won;
        Time.timeScale = 0f;
        hud.ShowVictory(ElapsedTime, Score);
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
