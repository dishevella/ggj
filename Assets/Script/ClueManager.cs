using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

public class ClueManager : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public static ClueManager I { get; private set; }

    [Header("Data")]
    [Tooltip("是否去重（建议开）")]
    public bool uniqueClues = true;

    // 维持“顺序”用 List；“去重/快速查找”用 HashSet
    private readonly List<string> _clues = new();
    private HashSet<string> _set;

    public IReadOnlyList<string> Clues => _clues;
    public event Action OnChanged;

    [Header("Memo UI (Drawer)")]
    [Tooltip("侧边栏的 RectTransform（就是这个 Panel 自己也行）")]
    public RectTransform drawer;
    [Tooltip("显示线索的 TMP 文本")]
    public TextMeshProUGUI clueText;

    [Tooltip("抽屉收起位置（anchoredPosition）")]
    public Vector2 hiddenPos = new Vector2(-320f, 0f);
    [Tooltip("抽屉拉出位置（anchoredPosition）")]
    public Vector2 shownPos = new Vector2(0f, 0f);

    [Tooltip("拉出/收回速度（越大越快）")]
    public float slideSpeed = 12f;

    [Tooltip("打开抽屉时刷新并打印一次列表")]
    public bool refreshAndLogOnOpen = true;

    [Header("Auto Peek On Update")]
    public float autoPeekStayTime = 2f;   // 拉出后停留时间

    Coroutine _autoPeekRoutine;
    bool _forceOpen;   // 是否被“系统强制拉出”


    private bool _hover;
    private bool _isOpen;

    void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;

        if (uniqueClues) _set = new HashSet<string>();
        if (drawer == null) drawer = GetComponent<RectTransform>();
    }

    void Start()
    {
        // 开局强制收起
        if (drawer != null) drawer.anchoredPosition = hiddenPos;
        _isOpen = false;
    }

    void Update()
    {
        if (drawer == null) return;

        Vector2 target = (_hover || _forceOpen) ? shownPos : hiddenPos;

        drawer.anchoredPosition = Vector2.Lerp(drawer.anchoredPosition, target, Time.deltaTime * slideSpeed);

        // “打开/关闭”的阈值判定（避免每帧刷新）
        bool nowOpen = Vector2.Distance(drawer.anchoredPosition, shownPos) < 1.0f;

        if (nowOpen && !_isOpen)
        {
            _isOpen = true;
            RefreshMemoUI();

            if (refreshAndLogOnOpen)
                Debug.Log($"[ClueManager] Memo opened. clues={_clues.Count}\n{BuildClueString()}");
        }
        else if (!nowOpen && _isOpen)
        {
            _isOpen = false;
        }
    }

    // -------------------------
    // Public API
    // -------------------------
    public bool AddClue(string clue)
    {
        if (string.IsNullOrWhiteSpace(clue)) return false;

        clue = clue.Trim();

        if (uniqueClues)
        {
            if (_set.Contains(clue)) return false;
            _set.Add(clue);
        }

        _clues.Add(clue);
        OnChanged?.Invoke();

        // 👉 自动拉出一次 Memo
        if (_autoPeekRoutine != null)
            StopCoroutine(_autoPeekRoutine);

        _autoPeekRoutine = StartCoroutine(CoAutoPeek());

        // 如果抽屉正开着，实时刷新（可选）
        if (_isOpen) RefreshMemoUI();

        return true;
    }

    public bool DeleteClue(string clue)
    {
        if (string.IsNullOrWhiteSpace(clue)) return false;

        clue = clue.Trim();

        bool removed = false;

        // 从列表移除（保持顺序逻辑）
        for (int i = _clues.Count - 1; i >= 0; i--)
        {
            if (string.Equals(_clues[i], clue, StringComparison.Ordinal))
            {
                _clues.RemoveAt(i);
                removed = true;
            }
        }

        if (uniqueClues && removed)
        {
            _set.Remove(clue);
        }

        if (removed)
        {
            OnChanged?.Invoke();
            if (_isOpen) RefreshMemoUI();
        }

        return removed;
    }

    public void ClearAll()
    {
        _clues.Clear();
        _set?.Clear();
        OnChanged?.Invoke();
        if (_isOpen) RefreshMemoUI();
    }

    IEnumerator CoAutoPeek()
    {
        _forceOpen = true;

        // 等抽屉完全拉出
        yield return new WaitUntil(() =>
            Vector2.Distance(drawer.anchoredPosition, shownPos) < 1f);

        // 停留
        yield return new WaitForSeconds(autoPeekStayTime);

        _forceOpen = false;
    }


    // -------------------------
    // Memo UI
    // -------------------------
    public void RefreshMemoUI()
    {
        if (clueText == null) return;
        clueText.text = BuildClueString();
    }

    private string BuildClueString()
    {
        if (_clues.Count == 0) return "（暂无线索）";

        var sb = new StringBuilder();
        for (int i = 0; i < _clues.Count; i++)
        {
            sb.Append("• ");
            sb.Append(_clues[i]);
            sb.Append('\n');
        }
        return sb.ToString();
    }

    // -------------------------
    // Hover Events
    // -------------------------
    public void OnPointerEnter(PointerEventData eventData) => _hover = true;
    public void OnPointerExit(PointerEventData eventData) => _hover = false;
}
