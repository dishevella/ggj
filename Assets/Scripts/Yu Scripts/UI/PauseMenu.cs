using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    [Header("UI")]
    public GameObject pausePanel;

    [Header("Keys")]
    public KeyCode toggleKey = KeyCode.Escape;

    [Header("Scene Names")]
    public string mainMenuSceneName = "MainMenu";

    public bool IsPaused { get; private set; }

    void Start()
    {
        // 防止从菜单回来时 timescale 没恢复
        Time.timeScale = 1f;

        if (pausePanel) pausePanel.SetActive(false);
        IsPaused = false;
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            Debug.Log("[PauseMenu] ESC detected!");
            if (IsPaused) Resume();
            else Pause();
        }
    }

    public void Pause()
    {
        Debug.Log("[PauseMenu] Pause() called, pausePanel=" + (pausePanel ? pausePanel.name : "NULL"));

        IsPaused = true;
        if (pausePanel) pausePanel.SetActive(true);

        Time.timeScale = 0f;

        // 可选：解锁鼠标（FPS常用）
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Resume()
    {
        IsPaused = false;
        if (pausePanel) pausePanel.SetActive(false);

        Time.timeScale = 1f;

        // 可选：锁回鼠标
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void GoToMainMenu()
    {
        // 重要：切场景前先恢复时间
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void QuitGame()
    {
        Time.timeScale = 1f;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
