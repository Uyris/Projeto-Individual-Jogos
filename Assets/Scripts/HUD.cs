using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Interface durante a partida: pontuação, cronômetro, cristais e o painel de vitória.
/// </summary>
public class HUD : MonoBehaviour
{
    [Header("Barra superior")]
    public Text scoreText;
    public Text timeText;
    public Text crystalText;

    [Header("Painéis")]
    public GameObject victoryPanel;
    public Text victoryStatsText;

    [Header("Botões")]
    public Button victoryRestartButton;
    public Button victoryMenuButton;

    void Start()
    {
        GameManager gm = GameManager.Instance;
        victoryRestartButton.onClick.AddListener(gm.Restart);
        victoryMenuButton.onClick.AddListener(gm.GoToMenu);

        victoryPanel.SetActive(false);
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;

        scoreText.text = gm.Score.ToString("00000");

        timeText.text = GameManager.FormatTime(gm.ElapsedTime);
        crystalText.text = $"{gm.CrystalsCollected}/{gm.TotalCrystals}";
    }

    public void ShowVictory(float time, int score)
    {
        victoryStatsText.text =
            $"TEMPO FINAL: {GameManager.FormatTime(time)}\n" +
            $"PONTUAÇÃO FINAL: {score:00000}";
        victoryPanel.SetActive(true);
    }
}
