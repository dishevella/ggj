using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuSceneLoader : MonoBehaviour
{
    [Header("Scene To Load")]
    public string gameSceneName = "Game"; // 你的游戏场景名字（必须和Scene文件名一致）

    // Start按钮绑定这个
    public void OnClickStart()
    {
        SceneManager.LoadScene(gameSceneName);
    }

    // Quit按钮绑定这个
    public void OnClickQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
