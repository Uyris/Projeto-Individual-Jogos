using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Interface durante a partida: pontuação, cronômetro, cristais, oxigênio
/// e os painéis de derrota e vitória.
/// </summary>
public class HUD : MonoBehaviour
{
    [Header("Barra superior")]
    public Text scoreText;
    public Text timeText;
    public Text crystalText;
    public RectTransform oxygenFill;
    public Image oxygenFillImage;
    public Text oxygenText;

    [Header("Painéis")]
    public GameObject gameOverPanel;
    public Text gameOverReasonText;
    public Text gameOverStatsText;
    public GameObject victoryPanel;
    public Text victoryStatsText;
    public Text victoryRecordText;

    [Header("Botões")]
    public Button gameOverRestartButton;
    public Button gameOverMenuButton;
    public Button victoryRestartButton;
    public Button victoryMenuButton;

    static readonly Color OxygenColor = new Color(0.3f, 0.85f, 1f);
    static readonly Color OxygenLowColor = new Color(1f, 0.3f, 0.3f);

    void Start()
    {
        GameManager gm = GameManager.Instance;
        gameOverRestartButton.onClick.AddListener(gm.Restart);
        gameOverMenuButton.onClick.AddListener(gm.GoToMenu);
        victoryRestartButton.onClick.AddListener(gm.Restart);
        victoryMenuButton.onClick.AddListener(gm.GoToMenu);

        gameOverPanel.SetActive(false);
        victoryPanel.SetActive(false);
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;

        scoreText.text = gm.Score.ToString("00000");

        timeText.text = GameManager.FormatTime(gm.ElapsedTime);
        crystalText.text = $"{gm.CrystalsCollected}/{gm.TotalCrystals}";

        bool low = gm.Oxygen <= gm.lowOxygen;
        SetFill(oxygenFill, gm.Oxygen / gm.maxOxygen);
        oxygenFillImage.color = !low ? OxygenColor
            : (Mathf.Repeat(Time.unscaledTime, 0.5f) < 0.25f ? OxygenLowColor : OxygenLowColor * 0.6f);
        oxygenText.text = $"{Mathf.CeilToInt(gm.Oxygen)}s";
        oxygenText.color = low ? OxygenLowColor : Color.white;
    }

    static void SetFill(RectTransform fill, float amount)
    {
        fill.anchorMax = new Vector2(Mathf.Clamp01(amount), 1f);
    }

    public void ShowGameOver(string reason, float time, int score, int crystals, int totalCrystals)
    {
        gameOverReasonText.text = reason;
        gameOverStatsText.text =
            $"TEMPO FINAL: {GameManager.FormatTime(time)}\n" +
            $"CRISTAIS: {crystals}/{totalCrystals}\n" +
            $"PONTOS: {score:00000}";
        gameOverPanel.SetActive(true);
    }

    public void ShowVictory(float time, int score, int oxygenBonus, bool newRecord, float bestTime)
    {
        victoryStatsText.text =
            $"TEMPO FINAL: {GameManager.FormatTime(time)}\n" +
            $"BÔNUS DE OXIGÊNIO: +{oxygenBonus}\n" +
            $"PONTUAÇÃO FINAL: {score:00000}";
        victoryRecordText.text = newRecord
            ? "NOVO RECORDE DE TEMPO!"
            : $"MELHOR TEMPO: {GameManager.FormatTime(bestTime)}";
        victoryPanel.SetActive(true);
    }
}
