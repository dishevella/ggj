using UnityEngine;

public class ClickNPC : MonoBehaviour
{
    [Header("Dialogue")]
    public DialogueGroupSO group;
    public DialogueManager dialogueManager; // 可拖入；不拖则自动 FindFirstObjectByType

    [Header("UI Hint")]
    public GameObject hintUI;              // “点击提示”UI根物体
    public bool hideHintWhenNoGroup = true;

    [Header("Range (choose one)")]
    [Tooltip("推荐：给 NPC 加一个 CircleCollider2D (isTrigger=true) 作为交互范围，然后拖到这里")]
    public Collider2D triggerRange;        // 触发范围（isTrigger=true）
    [Tooltip("如果不想用 Trigger，可用距离判定")]
    public Transform player;
    public float interactRadius = 2.0f;
    public bool useDistanceFallback = false;

    [Header("Click Detection")]
    [Tooltip("NPC 本体需要 Collider2D；这里可选指定，否则自动用 GetComponentInChildren<Collider2D>()")]
    public Collider2D npcCollider;
    public LayerMask clickableMask;        // 建议只勾 NPC 层

    [Header("Options")]
    public bool requireInRange = true;
    public bool allowClickWhenHintHidden = false; // 默认：UI没显示就不允许点

    private bool _inRange;

    void Awake()
    {
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

        if (Input.GetMouseButtonDown(0))
        {
            if (requireInRange && !_inRange) return;
            if (!allowClickWhenHintHidden && hintUI != null && !hintUI.activeSelf) return;

            if (group == null) return;
            if (dialogueManager == null)
            {
                Debug.LogError("[ClickNPC] DialogueManager not found. 请在 Inspector 里拖入或确保场景里有 DialogueManager。");
                return;
            }

            if (!IsClickedThisNPC()) return;

            dialogueManager.PlayGroup(group);
        }
    }

    void UpdateRangeState()
    {
        // 方案1：Trigger 范围（推荐，最稳）
        if (triggerRange != null && triggerRange.isTrigger)
        {
            // _inRange 会由 OnTriggerEnter2D/Exit2D 维护
            return;
        }

        // 方案2：距离 fallback
        if (useDistanceFallback && player != null)
        {
            float dist = Vector2.Distance(player.position, transform.position);
            _inRange = dist <= interactRadius;
        }
        else
        {
            // 没有范围手段时：视为一直可交互（你也可以改成 false）
            _inRange = true;
        }
    }

    void UpdateHintUI()
    {
        if (hintUI == null) return;

        bool shouldShow = _inRange;

        if (hideHintWhenNoGroup && group == null)
            shouldShow = false;

        hintUI.SetActive(shouldShow);
    }

    bool IsClickedThisNPC()
    {
        if (npcCollider == null) return false;

        // 鼠标屏幕坐标 -> 世界坐标
        Vector2 worldPoint = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        // 用 OverlapPoint 精准点选（配合 clickableMask 更稳）
        Collider2D hit = Physics2D.OverlapPoint(worldPoint, clickableMask);
        if (hit == null) return false;

        // 允许点到子物体 collider
        return hit == npcCollider || hit.transform.IsChildOf(npcCollider.transform);
    }

    // =========================
    // Trigger Range (2D)
    // =========================
    void OnTriggerEnter2D(Collider2D other)
    {
        if (triggerRange == null) return;

        // 只处理进入 triggerRange 的事件
        // 注意：triggerRange 一般挂在 NPC 自己或子物体上
        if (!other.CompareTag("Player")) return;

        _inRange = true;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (triggerRange == null) return;
        if (!other.CompareTag("Player")) return;

        _inRange = false;
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
