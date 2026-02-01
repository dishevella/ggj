using UnityEngine;

public class BackgroundSceneManager : MonoBehaviour
{
    [Header("Graph")]
    public SceneGraphSO graph;
    public int startNodeId = 0;
    public int startSpawnIndex = 0;

    [Header("References")]
    public SpriteRenderer backgroundRenderer;   // 用来显示背景图的 SpriteRenderer
    public Transform player;                    // 玩家 Transform
    public PlayerController playerController;   // 可选：用于 SetCanMove / 清速度（你之前写的那个）
    
    [Header("World Bounds (X)")]
    public float leftExitX = -9f;   // 玩家 x < 这个值：尝试切到左邻居
    public float rightExitX = 9f;   // 玩家 x > 这个值：尝试切到右邻居

    [Header("Air Walls (2D)")]
    public BoxCollider2D leftWall;   // 放在左边界位置
    public BoxCollider2D rightWall;  // 放在右边界位置
    public bool autoPlaceWalls = true;
    public float wallThickness = 0.5f;
    public float wallHeight = 20f;   // 足够高即可覆盖玩家可活动范围
    public float wallY = 0f;         // 空气墙中心Y（按你的场景来）

    [Header("Enter Behavior")]
    public bool freezePlayerDuringEnter = true;
    public bool clearPlayerVelocityOnEnter = true;

    [Header("Anti-Spam")]
    public float transitionCooldown = 0.15f;
    private float _nextAllowedTransitionTime = 0f;

    [Header("Runtime")]
    public int currentNodeId;
    private SceneGraphSO.SceneNode _currentNode;

    public System.Action<bool, bool> OnReachabilityChanged; // left, right

    private bool _lastCanLeft;
    private bool _lastCanRight;

    void Start()
    {
        ForceEnter(startNodeId, startSpawnIndex);
    }

    void Update()
    {
        if (graph == null || player == null) return;
        if (Time.time < _nextAllowedTransitionTime) return;

        // 若当前节点锁定，就不允许左右出界切换
        if (_currentNode != null && _currentNode.nodeLocked)
            return;

        float px = player.position.x;

        if (px < leftExitX)
        {
            TryExit(LinkDirection.Left);
        }
        else if (px > rightExitX)
        {
            TryExit(LinkDirection.Right);
        }
    }

    void TryExit(LinkDirection dir)
    {
        if (_currentNode == null) return;

        // 找边
        if (!graph.TryGetLink(currentNodeId, dir, out SceneGraphSO.NeighborLink link) || link == null)
        {
            // 没有邻居：视为锁定（空气墙会挡）
            SnapBackIntoBounds(dir);
            return;
        }

        // 边锁定：不切，弹回
        if (link.locked)
        {
            SnapBackIntoBounds(dir);
            return;
        }

        // 有邻居且不锁：切入
        _nextAllowedTransitionTime = Time.time + transitionCooldown;
        ForceEnter(link.neighborNodeId, link.enterSpawnIndex);
    }

    public bool CanGo(LinkDirection dir)
    {
        if (graph == null) return false;
        if (_currentNode == null) return false;
        if (_currentNode.nodeLocked) return false;

        if (!graph.TryGetLink(currentNodeId, dir, out SceneGraphSO.NeighborLink link) || link == null)
            return false;

        return !link.locked;
    }

    public bool CanGoLeft()  => CanGo(LinkDirection.Left);
    public bool CanGoRight() => CanGo(LinkDirection.Right);

    public bool SetNeighborLocked(LinkDirection dir, bool locked)
    {
        if (graph == null)
        {
            Debug.LogWarning("[BackgroundSceneManager] graph is null.");
            return false;
        }

        var node = graph.GetNode(currentNodeId);
        if (node == null)
        {
            Debug.LogWarning("[BackgroundSceneManager] current node is null.");
            return false;
        }

        // 找到这条边并修改 locked
        for (int i = 0; i < node.neighbors.Count; i++)
        {
            var l = node.neighbors[i];
            if (l != null && l.direction == dir)
            {
                l.locked = locked;

                // 立刻刷新墙 + UI
                UpdateWalls();
                NotifyReachabilityIfChanged(force: true);
                return true;
            }
        }

        Debug.LogWarning($"[BackgroundSceneManager] No link on {dir} from node {currentNodeId}.");
        return false;
    }

    void NotifyReachabilityIfChanged(bool force = false)
    {
        bool canL = CanGo(LinkDirection.Left);
        bool canR = CanGo(LinkDirection.Right);

        if (force || canL != _lastCanLeft || canR != _lastCanRight)
        {
            _lastCanLeft = canL;
            _lastCanRight = canR;
            OnReachabilityChanged?.Invoke(canL, canR);
        }
    }


    void SnapBackIntoBounds(LinkDirection dir)
    {
        // 万一你不放墙，只靠边界检测，这里给一个“轻微弹回”
        Vector3 p = player.position;
        if (dir == LinkDirection.Left) p.x = leftExitX + 0.01f;
        else p.x = rightExitX - 0.01f;
        player.position = p;
    }

    /// <summary>
    /// 外部可调用：立即切入某个节点（并设置玩家 spawn）
    /// </summary>
    public void ForceEnter(int nodeId, int spawnIndex = 0)
    {
        if (graph == null)
        {
            Debug.LogError("[BackgroundSceneManager] graph is null.");
            return;
        }

        var node = graph.GetNode(nodeId);
        if (node == null)
        {
            Debug.LogError($"[BackgroundSceneManager] Node not found: {nodeId}");
            return;
        }

        // 进入时冻结移动（可选）
        if (freezePlayerDuringEnter && playerController != null)
            playerController.SetCanMove(false);

        currentNodeId = nodeId;
        _currentNode = node;

        // 换背景
        if (backgroundRenderer != null)
            backgroundRenderer.sprite = node.backgroundSprite;

        // 设置玩家位置
        Vector2 spawn = graph.GetSpawnPointOrDefault(node, spawnIndex, player.position);
        player.position = new Vector3(spawn.x, spawn.y, player.position.z);

        // 清速度（可选）
        if (clearPlayerVelocityOnEnter && playerController != null)
        {
            // 你的 PlayerController 里有 velocity/speed 变量的话，就让它停下来
            // 这里不强依赖字段名，直接调用 SetCanMove(true) 时它也会刷新 lastPos
            // 如果你愿意，也可以在 PlayerController 里加一个 public StopImmediately()
        }

        // 更新空气墙
        UpdateWalls();
        
        // ⭐加这一行：刷新箭头显示
        NotifyReachabilityIfChanged(force: true);

        // 解冻移动（可选）
        if (freezePlayerDuringEnter && playerController != null)
            playerController.SetCanMove(true);
    }

    void UpdateWalls()
    {
        bool lockLeft = ShouldLockSide(LinkDirection.Left);
        bool lockRight = ShouldLockSide(LinkDirection.Right);

        if (leftWall != null)
        {
            leftWall.enabled = lockLeft;
            if (autoPlaceWalls) PlaceWall(leftWall, leftExitX);
        }

        if (rightWall != null)
        {
            rightWall.enabled = lockRight;
            if (autoPlaceWalls) PlaceWall(rightWall, rightExitX);
        }
    }

    bool ShouldLockSide(LinkDirection dir)
    {
        if (_currentNode == null) return true;

        // 节点整体锁定：两边锁
        if (_currentNode.nodeLocked) return true;

        // 没有边：锁
        if (!graph.TryGetLink(currentNodeId, dir, out SceneGraphSO.NeighborLink link) || link == null)
            return true;

        // 边锁定：锁
        return link.locked;
    }

    void PlaceWall(BoxCollider2D wall, float xPos)
    {
        Transform t = wall.transform;

        // 放在边界位置
        t.position = new Vector3(xPos, wallY, t.position.z);

        // 设置碰撞体尺寸（local）
        wall.size = new Vector2(wallThickness, wallHeight);
        wall.offset = Vector2.zero;
    }
}


//上锁与解锁：
//sceneManager.SetNeighborLocked(LinkDirection.Left, false); // 解锁左边
//sceneManager.SetNeighborLocked(LinkDirection.Right, true); // 上锁右边
