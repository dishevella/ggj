using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Linq;
using UnityEngine.UI;

public class DialogueManager : MonoBehaviour
{
    private bool _isActive = true;   // 对话系统是否可运行
    
    // =========================
    // UI
    // =========================
    [Header("UI")]
    public TextMeshProUGUI dialogueText;
    public Transform selectionRoot;           // 选项按钮父物体
    public Button selectionButtonPrefab;      // 选项按钮预制体（里面带 TMP 文本）

    // =========================
    // Presentation UI - Narration
    // =========================
    [Header("UI - Narration")]
    public GameObject narrationRoot;
    public TextMeshProUGUI narrationContentText;

    // =========================
    // Presentation UI - Left Speaker
    // =========================
    [Header("UI - Left Speaker")]
    public GameObject leftRoot;
    public Image leftPortrait;
    public TextMeshProUGUI leftNameText;
    public TextMeshProUGUI leftContentText;

    // =========================
    // Presentation UI - Right Speaker
    // =========================
    [Header("UI - Right Speaker")]
    public GameObject rightRoot;
    public Image rightPortrait;
    public TextMeshProUGUI rightNameText;
    public TextMeshProUGUI rightContentText;

    // 当前用于显示“内容”的 TMP（打字机/探索/跳过都写它）
    private TextMeshProUGUI _activeContentText;

    // =========================
    // Dialogue Group
    // =========================
    [Header("Dialogue Group")]
    public DialogueGroupSO currentGroup;      // ✅ 把 DialogueGroupSO asset 拖进来
    public int overrideStartIndex = -1;       // 可选：临时从某个 index 开始（-1=用 group.startIndex）

    // =========================
    // Typewriter Settings
    // =========================
    [Header("Typewriter")]
    public float charInterval = 0.04f;   // 每个字出现的间隔
    public float autoDelay = 1.2f;       // Auto 模式下，打完字后等待多久

    // =========================
    // Auto Mode
    // =========================
    [Header("Auto Mode")]
    public bool autoPlay = false;         // Auto 开关（可绑定 UI Toggle）

    // =========================
    // Runtime State
    // =========================
    private int _currentIndex;
    private Coroutine _typingCoroutine;

    private bool _isTyping = false;       // 是否正在打字
    private bool _isLineFinished = false; // 当前行是否完整显示

    private bool _waitingSelection = false;

    // =========================
    // Helpers (Current Node)
    // =========================
    private bool HasGroupAndNodes =>
        currentGroup != null && currentGroup.nodes != null && currentGroup.nodes.Count > 0;

    private bool IsValidIndex(int idx) =>
        HasGroupAndNodes && idx >= 0 && idx < currentGroup.nodes.Count;

    private DialogueGroupSO.DialogueNode CurrentNode =>
        IsValidIndex(_currentIndex) ? currentGroup.nodes[_currentIndex] : null;

    private bool CurrentHasSelection =>
        CurrentNode != null && CurrentNode.selection != null && CurrentNode.selection.Count > 0;

    private bool CurrentisLocked =>
        CurrentNode != null && CurrentNode.isLocked;

    // =========================
    // Unity
    // =========================
    void Start()
    {
        if (!HasGroupAndNodes)
        {
            if (dialogueText != null)
            {
                dialogueText.text = "(No Dialogue Group)";
                dialogueText.maxVisibleCharacters = int.MaxValue;
            }

            if (selectionRoot != null)
                selectionRoot.gameObject.SetActive(false);

            autoPlay = false;
            _isActive = false;   // ✅ 安全停机：不工作，但也不报错/不崩
            return;
        }

        int start = (overrideStartIndex >= 0) ? overrideStartIndex : currentGroup.startIndex;
        _currentIndex = Mathf.Clamp(start, 0, currentGroup.nodes.Count - 1);

        if (selectionRoot != null) selectionRoot.gameObject.SetActive(false);

        PlayCurrentNode();
    }

    void Update()
    {
        if (!_isActive) return;
        if (!HasGroupAndNodes) return;
        if (Mouse.current == null) return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            // ① 先处理探索点击（锁定时也允许探索）
            if (TryClickExploreWord()) return;

            // ✅ 等待选择时：禁止 Skip/GoNext
            if (_waitingSelection) return;

            // ② 锁定时：禁止普通点击（跳过/下一句）
            if (CurrentisLocked) return;

            // ③ 普通对话点击逻辑
            HandleClick();
        }
    }

    // =========================
    // Core Flow
    // =========================
    void PlayCurrentNode()
    {
        if (!IsValidIndex(_currentIndex))
        {
            if (dialogueText != null) dialogueText.text = "(Dialogue End)";
            return;
        }

        // 新增：进入节点时，初始化探索状态
        ResetExploreStateIfNeeded(CurrentNode);

        // 停止旧的打字协程
        if (_typingCoroutine != null)
            StopCoroutine(_typingCoroutine);

        ClearSelections();
        _waitingSelection = false;

        if (CurrentisLocked)
            autoPlay = false;

        ApplyPresentation(CurrentNode);
        _typingCoroutine = StartCoroutine(TypeLine(CurrentNode.content));
    }

    IEnumerator TypeLine(string content)
    {
        _isTyping = true;
        _isLineFinished = false;

        var target = _activeContentText != null ? _activeContentText : dialogueText;
        if (target == null) yield break;

        // 1) 一次性设置完整文本（含 <link> / <color>）
        target.text = content;

        // 2) 强制 TMP 生成 textInfo（解析富文本标签）
        target.ForceMeshUpdate();

        // 3) 从 0 个可见字符开始
        target.maxVisibleCharacters = 0;

        // TMP 解析后有 characterCount（可见字符计数，不含标签）
        int totalVisible = target.textInfo.characterCount;

        // 4) 逐步显示可见字符
        for (int i = 1; i <= totalVisible; i++)
        {
            target.maxVisibleCharacters = i;
            yield return new WaitForSeconds(charInterval);
        }

        // 5) 打字完成
        _isTyping = false;
        _isLineFinished = true;

        // ✅ 如果当前节点有玩家选项：进入等待选择状态
        if (CurrentHasSelection)
        {
            EnterSelectionMode();
            yield break; // 不再走 Auto / 不再 GoNext
        }

        // ✅ Auto：只有当前不锁定才自动跳
        if (autoPlay && !CurrentisLocked)
        {
            yield return new WaitForSeconds(autoDelay);

            // 再检查一次锁定（等待期间可能被锁）
            if (!CurrentisLocked)
                GoNext();
        }
    }

    // =========================
    // Input Logic
    // =========================
    void HandleClick()
    {
        // 情况 1：正在打字 → 直接显示全文
        if (_isTyping)
        {
            SkipTyping();
            return;
        }

        // 情况 2：已打完 → 手动进入下一句
        if (_isLineFinished && !autoPlay)
        {
            GoNext();
        }
    }

    void SkipTyping()
    {
        if (_typingCoroutine != null)
            StopCoroutine(_typingCoroutine);

        var target = _activeContentText != null ? _activeContentText : dialogueText;
        if (target == null) return;

        // 直接显示全句（包括富文本）
        target.text = CurrentNode != null ? CurrentNode.content : "";
        target.ForceMeshUpdate();
        target.maxVisibleCharacters = target.textInfo.characterCount;

        _isTyping = false;
        _isLineFinished = true;

        // 如果跳过后该节点有选项：立刻进入选项模式（保持一致）
        if (CurrentHasSelection)
            EnterSelectionMode();
    }

    // =========================
    // Node Transition
    // =========================
    void GoNext()
    {
        if (!IsValidIndex(_currentIndex)) return;

        int next = CurrentNode.nextIndex;

        if (next == -1)
        {
            _currentIndex = -1;
            dialogueText.text = "(Dialogue End)";
            return;
        }

        if (!IsValidIndex(next))
        {
            Debug.LogWarning($"[DialogueManager] nextIndex out of range: {next}");
            _currentIndex = -1;
            dialogueText.text = "(Dialogue End)";
            return;
        }

        _currentIndex = next;
        PlayCurrentNode();
    }

    // =========================
    // Exploration (click <link=id>...</link>)
    // =========================
    public void UnlockCurrentNode()
    {
        if (CurrentNode == null) return;
        CurrentNode.isLocked = false;
    }

    private bool TryClickExploreWord()
    {
        var target = _activeContentText != null ? _activeContentText : dialogueText;
        if (target == null) return false;

        if (CurrentNode == null) return false;
        if (Mouse.current == null) return false;

        // ✅ 确保 linkInfo 是最新的（尤其是打字机过程中）
        target.ForceMeshUpdate();

        Vector2 mousePos = Mouse.current.position.ReadValue();
        int linkIndex = TMP_TextUtilities.FindIntersectingLink(target, mousePos, null);
        if (linkIndex == -1) return false;

        var linkInfo = target.textInfo.linkInfo[linkIndex];
        string linkId = linkInfo.GetLinkID();

        var node = CurrentNode;
        if (node.tokens == null || node.tokens.Count == 0) return false;

        var token = node.tokens.Find(t => t.id == linkId);
        if (token == null) return false;

        // 点过一次就不可再点
        if (token.clicked) return true;

        // 标记已点击
        token.clicked = true;
        
        //======调用结算函数=======
        //探索完毕当前token，调用背包系统里面的函数，获得token对应的物品，或者获得对应的线索
        if(token.getObject != null)
        {
            //OnObjectFound(gameObject token.getObject)
        }

        if(token.getClue != null)
        {
            //AddClueToMemo(string token.getClue)
        }

        // 检查是否所有 required token 都点完 → 解锁
        if (node.isLocked)
        {
            bool allDone = node.tokens
                .Where(t => t.required)
                .All(t => t.clicked);

            if (allDone)
                node.isLocked = false;
        }

        // 点击后刷新显示（用来“变灰”）
        RefreshContentWithTokenState();

        return true;
    }

    private void RefreshContentWithTokenState()
    {
        if (CurrentNode == null) return;

        var node = CurrentNode;
        string raw = node.content;

        foreach (var t in node.tokens)
        {
            if (!t.clicked) continue;

            string open = $"<link={t.id}>";
            string close = "</link>";

            int a = raw.IndexOf(open, StringComparison.Ordinal);
            if (a < 0) continue;
            int b = raw.IndexOf(close, a, StringComparison.Ordinal);
            if (b < 0) continue;

            int start = a + open.Length;
            string inner = raw.Substring(start, b - start);

            if (inner.Contains("<color=")) continue;

            string replacedInner = $"<color=#888888>{inner}</color>";
            raw = raw.Substring(0, start) + replacedInner + raw.Substring(b);
        }

        var target = _activeContentText != null ? _activeContentText : dialogueText;
        if (target == null) return;

        target.text = raw;
        target.ForceMeshUpdate();

        // 刷新后，保持当前“可见字符数” = 全显示（因为通常点击探索词时你希望看到完整句）
        target.maxVisibleCharacters = target.textInfo.characterCount;
        _isTyping = false;
        _isLineFinished = true;

        // 若刷新后有选项，确保进入选项模式（避免出现“点探索词后没弹选项”）
        if (CurrentHasSelection)
            EnterSelectionMode();
    }

    private void ResetExploreStateIfNeeded(DialogueGroupSO.DialogueNode node)
    {
        if (node == null) return;
        if (node.tokens == null || node.tokens.Count == 0) return;

        // 只要有 Explore Tokens，就强制进入“探索锁定态”
        node.isLocked = true;

        foreach (var token in node.tokens)
        {
            token.clicked = false;
        }
    }

    // =========================
    // UI Hook
    // =========================
    public void SetAutoPlay()
    {
        // 等待选项时：强制关闭且禁止切换
        if (_waitingSelection)
        {
            autoPlay = false;
            return;
        }

        // 当前节点锁定：强制关闭，且不允许按钮改变状态
        if (CurrentisLocked)
        {
            autoPlay = false;
            return;
        }

        autoPlay = !autoPlay;
    }

    // =========================
    // Player Selection
    // =========================
    private void EnterSelectionMode()
    {
        if (selectionRoot == null || selectionButtonPrefab == null) return;

        _waitingSelection = true;
        autoPlay = false;

        BuildSelectionButtons();
    }

    private void BuildSelectionButtons()
    {
        ClearSelections();

        selectionRoot.gameObject.SetActive(true);

        var node = CurrentNode;
        if (node == null || node.selection == null) return;

        foreach (var sel in node.selection)
        {
            var btn = Instantiate(selectionButtonPrefab, selectionRoot);

            // 按钮文字
            var tmp = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null) tmp.text = sel.content;

            // 绑定点击 → 跳转
            btn.onClick.RemoveAllListeners();
            DialogueGroupSO.PlayerSelection captured = sel; // 防止闭包踩坑（老 Unity 版本更稳）
            btn.onClick.AddListener(() => OnSelect(captured));
        }
    }

    private void OnSelect(DialogueGroupSO.PlayerSelection sel)
    {
        _waitingSelection = false;
        ClearSelections();

        int target = sel.targetDialogueIndex;
        if (!IsValidIndex(target))
        {
            Debug.LogWarning($"[DialogueManager] targetDialogueIndex out of range: {target}");
            return;
        }

        _currentIndex = target;
        PlayCurrentNode();
    }

    private void ClearSelections()
    {
        if (selectionRoot == null) return;

        // 先删子按钮
        foreach (Transform t in selectionRoot)
            Destroy(t.gameObject);

        // 再隐藏 root
        selectionRoot.gameObject.SetActive(false);
    }

    // ========================
    // Speaker Information
    // ========================
        private DialogueGroupSO.Speaker GetSpeaker(DialogueGroupSO.DialogueNode node)
    {
        if (node == null) return null;
        if (currentGroup == null || currentGroup.speakers == null) return null;

        int i = node.speakerIndex;
        if (i < 0 || i >= currentGroup.speakers.Count) return null;

        return currentGroup.speakers[i];
    }

    private void SetAllPresentationOff()
    {
        if (narrationRoot != null) narrationRoot.SetActive(false);
        if (leftRoot != null) leftRoot.SetActive(false);
        if (rightRoot != null) rightRoot.SetActive(false);
    }

    private void ApplySpeakerUI(Image portrait, TextMeshProUGUI nameText, Sprite portraitSprite, string name)
    {
        if (portrait != null)
        {
            bool has = portraitSprite != null;
            portrait.gameObject.SetActive(has);
            if (has) portrait.sprite = portraitSprite;
        }

        if (nameText != null)
        {
            bool show = !string.IsNullOrEmpty(name);
            nameText.gameObject.SetActive(show);
            nameText.text = name;
        }
    }

    private void ApplyPresentation(DialogueGroupSO.DialogueNode node)
    {
        SetAllPresentationOff();

        // 默认 fallback（防止没配UI时 NRE）
        _activeContentText = narrationContentText != null ? narrationContentText : dialogueText;

        if (node == null)
        {
            if (narrationRoot != null) narrationRoot.SetActive(true);
            return;
        }

        var speaker = GetSpeaker(node);

        // speaker 无效 或 channel=旁白 => 旁白
        bool isNarration =
            node.channel == DialogueGroupSO.DialogueChannel.Narration || speaker == null;

        if (isNarration)
        {
            if (narrationRoot != null) narrationRoot.SetActive(true);
            _activeContentText = narrationContentText != null ? narrationContentText : dialogueText;
            return;
        }

        // Node override 优先
        string finalName = !string.IsNullOrEmpty(node.nameOverride)
            ? node.nameOverride
            : speaker.displayName;

        Sprite finalPortrait = node.portraitOverride != null
            ? node.portraitOverride
            : speaker.defaultPortrait;

        if (node.channel == DialogueGroupSO.DialogueChannel.Left)
        {
            if (leftRoot != null) leftRoot.SetActive(true);
            _activeContentText = leftContentText != null ? leftContentText : dialogueText;
            ApplySpeakerUI(leftPortrait, leftNameText, finalPortrait, finalName);
        }
        else // Right
        {
            if (rightRoot != null) rightRoot.SetActive(true);
            _activeContentText = rightContentText != null ? rightContentText : dialogueText;
            ApplySpeakerUI(rightPortrait, rightNameText, finalPortrait, finalName);
        }
    }

    // =========================
    // Switch Group at Runtime
    // =========================
    public void PlayGroup(DialogueGroupSO group)
    {
        if (group == null || group.nodes == null || group.nodes.Count == 0)
        {
            // 不要报错炸屏，保持“能跑但不工作”
            currentGroup = null;

            if (dialogueText != null)
            {
                dialogueText.text = "(No Dialogue Group)";
                dialogueText.maxVisibleCharacters = int.MaxValue;
            }

            autoPlay = false;
            _waitingSelection = false;
            _isTyping = false;
            _isLineFinished = false;

            if (selectionRoot != null) selectionRoot.gameObject.SetActive(false);

            _isActive = false; // 继续安全停机
            return;
        }

        // 只要点击按钮播放组，就必须恢复系统运行
        _isActive = true;

        currentGroup = group;

        int start = (overrideStartIndex >= 0) ? overrideStartIndex : group.startIndex;
        _currentIndex = Mathf.Clamp(start, 0, group.nodes.Count - 1);

        // 清理旧状态
        if (_typingCoroutine != null) StopCoroutine(_typingCoroutine);
        _typingCoroutine = null;

        _waitingSelection = false;
        autoPlay = false;

        _isTyping = false;
        _isLineFinished = false;

        ClearSelections();

        PlayCurrentNode();
    }

}
