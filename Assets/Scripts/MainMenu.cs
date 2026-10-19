using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Menu inicial: jogar, como jogar, créditos e sair.</summary>
public class MainMenu : MonoBehaviour
{
    public string gameScene = "Game";

    [Header("Painéis")]
    public GameObject mainPanel;
    public GameObject howToPanel;
    public GameObject creditsPanel;

    [Header("Botões")]
    public Button playButton;
    public Button howToButton;
    public Button creditsButton;
    public Button quitButton;
    public Button howToBackButton;
    public Button creditsBackButton;

    public Text bestText;

    void Start()
    {
        Time.timeScale = 1f;

        playButton.onClick.AddListener(Play);
        howToButton.onClick.AddListener(() => Show(howToPanel));
        creditsButton.onClick.AddListener(() => Show(creditsPanel));
        quitButton.onClick.AddListener(Application.Quit);
        howToBackButton.onClick.AddListener(() => Show(mainPanel));
        creditsBackButton.onClick.AddListener(() => Show(mainPanel));

        // No navegador não existe "sair do jogo".
        if (Application.platform == RuntimePlatform.WebGLPlayer) quitButton.gameObject.SetActive(false);

        float bestTime = PlayerPrefs.GetFloat(GameManager.BestTimeKey, 0f);
        int bestScore = PlayerPrefs.GetInt(GameManager.BestScoreKey, 0);
        bestText.text = bestTime > 0f
            ? $"MELHOR TEMPO {GameManager.FormatTime(bestTime)}   RECORDE {bestScore:00000}"
            : "COLETE OS CRISTAIS E FUJA DA ESTAÇÃO!";

        Show(mainPanel);
    }

    void Update()
    {
        if (mainPanel.activeSelf) return;

        bool back = (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) ||
                    (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame);
        if (back)
        {
            AudioManager.PlayUi(true);
            Show(mainPanel);
        }
    }

    void Show(GameObject panel)
    {
        mainPanel.SetActive(panel == mainPanel);
        howToPanel.SetActive(panel == howToPanel);
        creditsPanel.SetActive(panel == creditsPanel);
    }

    void Play()
    {
        StartCoroutine(LoadGame());
    }

    IEnumerator LoadGame()
    {
        playButton.interactable = false;
        yield return new WaitForSecondsRealtime(0.15f);
        SceneManager.LoadScene(gameScene);
    }
}
