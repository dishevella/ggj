using UnityEngine;

public class SceneArrowUI : MonoBehaviour
{
    public BackgroundSceneManager sceneManager;

    [Header("Arrow Objects")]
    public GameObject leftArrow;
    public GameObject rightArrow;

    void Start()
    {
        if (sceneManager == null)
        {
            Debug.LogError("[SceneArrowUI] Missing sceneManager reference.");
            return;
        }

        // 订阅事件（推荐）
        sceneManager.OnReachabilityChanged += Apply;

        // 初始同步一次
        Apply(sceneManager.CanGoLeft(), sceneManager.CanGoRight());
    }

    void OnDestroy()
    {
        if (sceneManager != null)
            sceneManager.OnReachabilityChanged -= Apply;
    }

    void Apply(bool canLeft, bool canRight)
    {
        if (leftArrow != null) leftArrow.SetActive(canLeft);
        if (rightArrow != null) rightArrow.SetActive(canRight);
    }
}
