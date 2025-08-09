using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class WelcomeScreen : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject welcomePanel; // Your full-screen panel with text & button
    public Button continueButton;

    [Header("Scene Settings")]
    public string arSceneName = "ARScene"; // Name of the scene to load
    public Animator animator;

    private const string PrefKey_WelcomeShown = "WelcomeScreenShown";

    void Start()
    {
        // Check if the welcome screen has been shown before
        if (PlayerPrefs.GetInt(PrefKey_WelcomeShown, 0) == 1)
        {
            LoadARScene();
        }
        else
        {
            welcomePanel.SetActive(true);
            continueButton.onClick.AddListener(OnContinue);
        }
    }

    public void OnContinue()
    {
        animator.SetBool("Continue", true);

        StartCoroutine(ContinueAfterDelay(1));
    }

    private System.Collections.IEnumerator ContinueAfterDelay(int delay)
    {
        PlayerPrefs.SetInt(PrefKey_WelcomeShown, 1);
        PlayerPrefs.Save();

        yield return new WaitForSeconds(delay);

        LoadARScene();
    }

    void LoadARScene()
    {
        SceneManager.LoadScene(arSceneName);
    }
}
