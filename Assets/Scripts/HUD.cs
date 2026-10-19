using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Interface durante a partida: pontuação, cronômetro, cristais, vida, oxigênio, stamina,
/// mensagens e os painéis de pausa, derrota e vitória.
/// </summary>
public class HUD : MonoBehaviour
{
    [Header("Barra superior")]
    public Text scoreText;
    public Text timeText;
    public Text crystalText;
    public Image[] hearts;
    public Sprite heartFull;
    public Sprite heartEmpty;
    public RectTransform oxygenFill;
    public Image oxygenFillImage;
    public Text oxygenText;
    public RectTransform staminaFill;
    public Image staminaFillImage;

    [Header("Mensagens")]
    public Text messageText;
    public Text countdownText;

    [Header("Painéis")]
    public GameObject pausePanel;
    public GameObject gameOverPanel;
    public Text gameOverReasonText;
    public Text gameOverStatsText;
    public GameObject victoryPanel;
    public Text victoryStatsText;
    public Text victoryRecordText;

    [Header("Botões")]
    public Button resumeButton;
    public Button pauseRestartButton;
    public Button pauseMenuButton;
    public Button gameOverRestartButton;
    public Button gameOverMenuButton;
    public Button victoryRestartButton;
    public Button victoryMenuButton;

    static readonly Color OxygenColor = new Color(0.3f, 0.85f, 1f);
    static readonly Color OxygenLowColor = new Color(1f, 0.3f, 0.3f);
    static readonly Color StaminaColor = new Color(1f, 0.82f, 0.25f);
    static readonly Color StaminaLowColor = new Color(0.55f, 0.45f, 0.25f);

    float messageTimer;
    float scorePunch;
    float countdownPunch;
    int lastScore;

    void Start()
    {
        GameManager gm = GameManager.Instance;
        resumeButton.onClick.AddListener(gm.Resume);
        pauseRestartButton.onClick.AddListener(gm.Restart);
        pauseMenuButton.onClick.AddListener(gm.GoToMenu);
        gameOverRestartButton.onClick.AddListener(gm.Restart);
        gameOverMenuButton.onClick.AddListener(gm.GoToMenu);
        victoryRestartButton.onClick.AddListener(gm.Restart);
        victoryMenuButton.onClick.AddListener(gm.GoToMenu);

        pausePanel.SetActive(false);
        gameOverPanel.SetActive(false);
        victoryPanel.SetActive(false);
        messageText.text = "";
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;
        PlayerController player = gm.player;
        float dt = Time.unscaledDeltaTime;

        scoreText.text = gm.Score.ToString("00000");
        if (gm.Score != lastScore)
        {
            lastScore = gm.Score;
            scorePunch = 1f;
        }
        scorePunch = Mathf.MoveTowards(scorePunch, 0f, dt * 4f);
        scoreText.transform.localScale = Vector3.one * (1f + scorePunch * 0.25f);

        timeText.text = GameManager.FormatTime(gm.ElapsedTime);
        crystalText.text = $"{gm.CrystalsCollected}/{gm.TotalCrystals}";

        for (int i = 0; i < hearts.Length; i++)
            hearts[i].sprite = i < player.Health ? heartFull : heartEmpty;

        bool low = gm.Oxygen <= gm.lowOxygen;
        SetFill(oxygenFill, gm.Oxygen / gm.maxOxygen);
        oxygenFillImage.color = !low ? OxygenColor
            : (Mathf.Repeat(Time.unscaledTime, 0.5f) < 0.25f ? OxygenLowColor : OxygenLowColor * 0.6f);
        oxygenText.text = $"{Mathf.CeilToInt(gm.Oxygen)}s";
        oxygenText.color = low ? OxygenLowColor : Color.white;

        SetFill(staminaFill, player.Stamina / player.maxStamina);
        staminaFillImage.color = player.Stamina >= player.dashCost ? StaminaColor : StaminaLowColor;

        if (messageTimer > 0f)
        {
            messageTimer -= dt;
            Color c = messageText.color;
            c.a = Mathf.Clamp01(messageTimer * 2f);
            messageText.color = c;
        }

        if (countdownText.gameObject.activeSelf)
        {
            countdownPunch = Mathf.MoveTowards(countdownPunch, 0f, dt * 3f);
            countdownText.transform.localScale = Vector3.one * (1f + countdownPunch * 0.6f);
        }
    }

    static void SetFill(RectTransform fill, float amount)
    {
        fill.anchorMax = new Vector2(Mathf.Clamp01(amount), 1f);
    }

    public void ShowMessage(string message, float duration)
    {
        messageText.text = message;
        messageTimer = duration;
        Color c = messageText.color;
        c.a = 1f;
        messageText.color = c;
    }

    public void ShowCountdown(string text)
    {
        countdownText.gameObject.SetActive(!string.IsNullOrEmpty(text));
        countdownText.text = text ?? "";
        countdownPunch = 1f;
    }

    public void ShowPause(bool show)
    {
        pausePanel.SetActive(show);
    }

    public void ShowGameOver(string reason, float time, int score, int crystals, int totalCrystals)
    {
        messageText.text = "";
        gameOverReasonText.text = reason;
        gameOverStatsText.text =
            $"TEMPO FINAL: {GameManager.FormatTime(time)}\n" +
            $"CRISTAIS: {crystals}/{totalCrystals}\n" +
            $"PONTOS: {score:00000}";
        gameOverPanel.SetActive(true);
    }

    public void ShowVictory(float time, int score, int oxygenBonus, int healthBonus, bool newRecord, float bestTime)
    {
        messageText.text = "";
        victoryStatsText.text =
            $"TEMPO FINAL: {GameManager.FormatTime(time)}\n" +
            $"BÔNUS DE OXIGÊNIO: +{oxygenBonus}\n" +
            $"BÔNUS DE VIDA: +{healthBonus}\n" +
            $"PONTUAÇÃO FINAL: {score:00000}";
        victoryRecordText.text = newRecord
            ? "NOVO RECORDE DE TEMPO!"
            : $"MELHOR TEMPO: {GameManager.FormatTime(bestTime)}";
        victoryPanel.SetActive(true);
    }
}
