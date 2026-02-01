using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class ClickNPC : MonoBehaviour
{
    [Header("Dialogue Rules")]
    [Tooltip("根据 score 选择对话。默认策略：选 selectScore <= score 的最大一条。")]
    public List<DialogueRule> rules = new List<DialogueRule>();

    [Tooltip("当前系数（可外部累加）")]
    public int score = 0;

    [Tooltip("每次点击前先给 score 叠加（可选：例如每点一次更熟悉/更紧张）")]
    public int clickAddScore = 0;

    [Tooltip("对话正在进行时是否禁止再次点击触发")]
    public bool blockWhenDialogueOpen = true;

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
    private bool _pendingEndAdd = false;
    private int _pendingAddValue = 0;

    void Awake()
    {
        if (npcCollider == null)
            npcCollider = GetComponentInChildren<Collider2D>();

        if (dialogueManager == null)
            dialogueManager = Object.FindFirstObjectByType<DialogueManager>();

        if (hintUI != null)
            hintUI.SetActive(false);

        // 订阅对话结束：用于加 endAddScore
        if (dialogueManager != null)
            dialogueManager.OnDialogueClosed += OnDialogueClosed;
    }

    void OnDestroy()
    {
        if (dialogueManager != null)
            dialogueManager.OnDialogueClosed -= OnDialogueClosed;
    }

    void Update()
    {
        UpdateRangeState();
        UpdateHintUI();

        if (Mouse.current == null) return;
        if (!Mouse.current.leftButton.wasPressedThisFrame) return;

        if (blockWhenDialogueOpen && dialogueManager != null && dialogueManager.IsOpen)
            return;

        // 1) 必须在范围内
        if (requireInRange && !_inRange) return;

        // 2) 提示UI不显示就不允许点
        if (!allowClickWhenHintHidden && hintUI != null && !hintUI.activeSelf) return;

        // 3) 必须点到“身体Collider”
        if (!IsClickedBody()) return;

        // 4) 选择规则
        if (rules == null || rules.Count == 0) return;

        // 点击累加（可选）
        if (clickAddScore != 0)
            score += clickAddScore;

        DialogueRule rule = PickRuleByScore(score);
        if (rule == null || rule.group == null)
            return;

        if (StateManager.I == null)
        {
            Debug.LogError("[ClickNPC] StateManager.I is null. 场景里需要有 StateManager 并在 Awake 里赋值 I。");
            return;
        }

        // 准备：对话结束后加 endAddScore
        _pendingEndAdd = true;
        _pendingAddValue = rule.endAddScore;

        // 启动对话
        StateManager.I.TryStartDialogue(rule.group);
    }

    // =========================
    // 外部接口：给别的系统累加系数
    // =========================
    public void AddScore(int delta)
    {
        score += delta;
    }

    public void SetScore(int value)
    {
        score = value;
    }

    // =========================
    // 规则选择：selectScore <= score 中最大者
    // =========================
    DialogueRule PickRuleByScore(int currentScore)
    {
        DialogueRule best = null;
        int bestThreshold = int.MinValue;

        for (int i = 0; i < rules.Count; i++)
        {
            var r = rules[i];
            if (r == null || r.group == null) continue;

            if (r.selectScore <= currentScore && r.selectScore >= bestThreshold)
            {
                bestThreshold = r.selectScore;
                best = r;
            }
        }

        // 如果一个都没命中，你也可以选择：返回最小 selectScore 的那条作为兜底
        // 这里我给你做成“兜底最小阈值”更友好：
        if (best == null)
        {
            int min = int.MaxValue;
            for (int i = 0; i < rules.Count; i++)
            {
                var r = rules[i];
                if (r == null || r.group == null) continue;
                if (r.selectScore < min) { min = r.selectScore; best = r; }
            }
        }

        return best;
    }

    // =========================
    // 对话结束：叠加 endAddScore
    // =========================
    void OnDialogueClosed()
    {
        if (!_pendingEndAdd) return;

        score += _pendingAddValue;

        _pendingEndAdd = false;
        _pendingAddValue = 0;

        // Debug.Log($"[ClickNPC] Dialogue end add applied. score={score}", this);
    }

    // =========================
    // Range & Hint (原样保留)
    // =========================
    void UpdateRangeState()
    {
        if (triggerRange != null && player != null)
        {
            _inRange = triggerRange.OverlapPoint(player.position);
            return;
        }

        if (useDistanceFallback && player != null)
        {
            float dist = Vector2.Distance(player.position, transform.position);
            _inRange = dist <= interactRadius;
            return;
        }

        _inRange = false;
    }

    void UpdateHintUI()
    {
        if (hintUI == null) return;

        bool shouldShow = _inRange;

        if (hideHintWhenNoGroup)
        {
            // 只要 rules 里没可用 group，就隐藏
            bool hasAny = false;
            if (rules != null)
            {
                for (int i = 0; i < rules.Count; i++)
                {
                    if (rules[i] != null && rules[i].group != null) { hasAny = true; break; }
                }
            }
            if (!hasAny) shouldShow = false;
        }

        hintUI.SetActive(shouldShow);
    }

    bool IsClickedBody()
    {
        if (npcCollider == null) return false;

        Camera cam = Camera.main;
        if (cam == null) return false;

        Vector2 worldPoint = cam.ScreenToWorldPoint(Mouse.current.position.ReadValue());

        var hits = Physics2D.OverlapPointAll(worldPoint, clickableMask);
        if (hits == null || hits.Length == 0) return false;

        foreach (var h in hits)
        {
            if (triggerRange != null && h == triggerRange) continue;
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

[System.Serializable]
public class DialogueRule
{
    public int selectScore;
    public DialogueGroupSO group;
    public int endAddScore;
}
