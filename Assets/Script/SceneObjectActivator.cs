using System.Collections.Generic;
using UnityEngine;

public class SceneObjectActivator : MonoBehaviour
{
    [System.Serializable]
    public class SceneObjectGroup
    {
        public int sceneId;                    // 对应 BackgroundSceneManager 的 nodeId
        public List<GameObject> objects;       // 该场景下要激活的物体
    }

    [Header("Bindings")]
    public List<SceneObjectGroup> sceneGroups = new List<SceneObjectGroup>();

    [Header("References")]
    public BackgroundSceneManager sceneManager;

    private int _lastSceneId = -1;

    void Start()
    {
        if (sceneManager == null)
        {
            Debug.LogError("[SceneObjectActivator] Missing BackgroundSceneManager reference.");
            return;
        }

        // 初始同步一次
        ApplyScene(sceneManager.currentNodeId);
    }

    void Update()
    {
        if (sceneManager == null) return;

        int current = sceneManager.currentNodeId;
        if (current != _lastSceneId)
        {
            ApplyScene(current);
        }
    }

    void ApplyScene(int sceneId)
    {
        _lastSceneId = sceneId;

        foreach (var group in sceneGroups)
        {
            bool active = group.sceneId == sceneId;

            foreach (var go in group.objects)
            {
                if (go != null)
                    go.SetActive(active);
            }
        }
    }
}
