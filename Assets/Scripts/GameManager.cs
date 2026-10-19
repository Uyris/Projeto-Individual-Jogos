using UnityEngine;

/// <summary>
/// GameManager estático (tutorial "End Game, Static GameManager"): conta os cristais
/// coletados e termina o jogo quando todos forem pegos.
/// </summary>
public class GameManager : MonoBehaviour
{
    public enum State { Playing, Won }

    public static GameManager Instance { get; private set; }
    public static bool IsPlaying => Instance != null && Instance.CurrentState == State.Playing;

    [Header("Referências")]
    public GameObject victoryText;

    public State CurrentState { get; private set; }
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
        victoryText.SetActive(false);
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
        victoryText.SetActive(true);
    }
}
