using System;
using System.Collections.Generic;
using UnityEngine;

public enum LinkDirection
{
    Left = -1,
    Right = 1
}

[CreateAssetMenu(menuName = "Exploration/Scene Graph (SO)", fileName = "SceneGraphSO")]
public class SceneGraphSO : ScriptableObject
{
    [Serializable]
    public class NeighborLink
    {
        public LinkDirection direction;     // 左/右
        public int neighborNodeId;          // 邻居节点编号
        public int enterSpawnIndex = 0;     // 进入邻居后，玩家初始位置编号（spawn index）
        public bool locked = false;         // 这条边是否锁定（锁定则该方向空气墙）
    }

    [Serializable]
    public class SceneNode
    {
        public int nodeId;

        [Header("Background")]
        public Sprite backgroundSprite;

        [Header("Spawn Points (local offsets or world positions)")]
        public List<Vector2> spawnPoints = new List<Vector2>(); 
        // 建议你用“世界坐标”存（更直观）；也可以存相对坐标，自己约定即可

        [Header("Neighbors")]
        public List<NeighborLink> neighbors = new List<NeighborLink>();

        [Header("Node Lock")]
        public bool nodeLocked = false; // 如果锁定：两侧都空气墙（不允许离开）
    }

    public List<SceneNode> nodes = new List<SceneNode>();

    public SceneNode GetNode(int nodeId)
    {
        for (int i = 0; i < nodes.Count; i++)
        {
            if (nodes[i] != null && nodes[i].nodeId == nodeId)
                return nodes[i];
        }
        return null;
    }

    public bool TryGetLink(int fromNodeId, LinkDirection dir, out NeighborLink link)
    {
        link = null;
        var node = GetNode(fromNodeId);
        if (node == null) return false;

        for (int i = 0; i < node.neighbors.Count; i++)
        {
            var l = node.neighbors[i];
            if (l != null && l.direction == dir)
            {
                link = l;
                return true;
            }
        }
        return false;
    }

    public Vector2 GetSpawnPointOrDefault(SceneNode node, int spawnIndex, Vector2 fallback)
    {
        if (node == null) return fallback;
        if (node.spawnPoints == null || node.spawnPoints.Count == 0) return fallback;
        if (spawnIndex < 0 || spawnIndex >= node.spawnPoints.Count) return node.spawnPoints[0];
        return node.spawnPoints[spawnIndex];
    }
}
