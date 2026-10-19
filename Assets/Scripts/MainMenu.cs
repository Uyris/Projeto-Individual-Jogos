using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Menu inicial: jogar e sair.</summary>
public class MainMenu : MonoBehaviour
{
    public string gameScene = "Game";

    [Header("Botões")]
    public Button playButton;
    public Button quitButton;

    void Start()
    {
        Time.timeScale = 1f;

        playButton.onClick.AddListener(Play);
        quitButton.onClick.AddListener(Application.Quit);

        // No navegador não existe "sair do jogo".
        if (Application.platform == RuntimePlatform.WebGLPlayer) quitButton.gameObject.SetActive(false);
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
