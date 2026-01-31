using UnityEngine;
using UnityEngine.InputSystem;

public class ClickNPC : MonoBehaviour
{
    [Header("Dialogue")]
    public DialogueGroupSO group;
    public DialogueManager dialogueManager;

    [Header("UI Hint")]
    public GameObject hintUI;
    public bool hideHintWhenNoGroup = true;

    [Header("Range")]
    [Tooltip("交互范围Collider2D（一般是子物体 HintRange），建议 isTrigger=true")]
    public Collider2D triggerRange;

    [Tooltip("玩家 Transform（必须拖）")]
    public Transform player;

    [Tooltip("当 triggerRange 为空时，使用距离判定")]
    public bool useDistanceFallback = false;
    public float interactRadius = 2.0f;

    [Header("Click Detection (Body Only)")]
    [Tooltip("只把 NPC 身体的 Collider2D 拖进来（ClickRange/BodyCollider）")]
    public Collider2D npcCollider;

    [Tooltip("只勾 NPC 身体所在的 Layer（不要把 HintRange 的 Layer 也算进去）")]
    public LayerMask clickableMask;

    [Header("Options")]
    public bool requireInRange = true;
    public bool allowClickWhenHintHidden = false;

    private bool _inRange;

    void Awake()
    {
        // npcCollider 必须是“身体”
        if (npcCollider == null)
            npcCollider = GetComponentInChildren<Collider2D>();

        if (dialogueManager == null)
            dialogueManager = Object.FindFirstObjectByType<DialogueManager>();

        if (hintUI != null)
            hintUI.SetActive(false);
    }

    void Update()
    {
        UpdateRangeState();
        UpdateHintUI();

        if (Mouse.current == null) return;
        if (!Mouse.current.leftButton.wasPressedThisFrame) return;

        // 1) 必须在范围内
        if (requireInRange && !_inRange) return;

        // 2) 提示UI不显示就不允许点（你自己控制）
        if (!allowClickWhenHintHidden && hintUI != null && !hintUI.activeSelf) return;

        // 3) 必须点到“身体Collider”
        if (!IsClickedBody()) return;

        // 4) 必须有组、有 StateManager
        if (group == null) return;
        if (StateManager.I == null)
        {
            Debug.LogError("[ClickNPC] StateManager.I is null. 场景里需要有 StateManager 并在 Awake 里赋值 I。");
            return;
        }

        StateManager.I.TryStartDialogue(group);
    }

    void UpdateRangeState()
    {
        // 优先用 triggerRange（最稳）
        if (triggerRange != null && player != null)
        {
            _inRange = triggerRange.OverlapPoint(player.position);
            return;
        }

        // 备用：距离判定
        if (useDistanceFallback && player != null)
        {
            float dist = Vector2.Distance(player.position, transform.position);
            _inRange = dist <= interactRadius;
            return;
        }

        // 没有任何范围手段：默认不可交互（避免“全图都能点”）
        _inRange = false;
    }

    void UpdateHintUI()
    {
        if (hintUI == null) return;

        bool shouldShow = _inRange;

        if (hideHintWhenNoGroup && group == null)
            shouldShow = false;

        hintUI.SetActive(shouldShow);
    }

    bool IsClickedBody()
    {
        if (npcCollider == null) return false;

        Camera cam = Camera.main;
        if (cam == null) return false;

        Vector2 worldPoint = cam.ScreenToWorldPoint(Mouse.current.position.ReadValue());

        // 用 All：避免点到别的 collider（比如 HintRange）导致误判
        var hits = Physics2D.OverlapPointAll(worldPoint, clickableMask);
        if (hits == null || hits.Length == 0) return false;

        foreach (var h in hits)
        {
            // ✅ 如果你把 triggerRange 的 Layer 也勾进 clickableMask，这里会跳过它
            if (triggerRange != null && h == triggerRange) continue;

            // ✅ 必须命中身体 collider 或其子物体（你有多个身体 collider 时也兼容）
            if (h == npcCollider || h.transform.IsChildOf(npcCollider.transform))
                return true;
        }

        return false;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (useDistanceFallback && player != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, interactRadius);
        }
    }
#endif
}
