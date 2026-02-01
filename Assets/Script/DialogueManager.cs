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
    [Header("UI - Root")]
    public GameObject dialogueUIRoot; // 整个对话UI的父物体（建议就是 Canvas 或 DialogueUI）

    // Selection UI
    [Header("SelectionButton")]
    public Button choiceLeft;
    public TextMeshProUGUI choiceLeftText;

    public Button choiceRight;
    public TextMeshProUGUI choiceRightText;

    [Header("Fade")]
    public float fadeDuration = 0.25f;

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
    public Image leftNameImage;
    public TextMeshProUGUI leftContentText;

    // =========================
    // Presentation UI - Right Speaker
    // =========================
    [Header("UI - Right Speaker")]
    public GameObject rightRoot;
    public Image rightPortrait;
    public Image rightNameImage;
    public TextMeshProUGUI rightContentText;

    // 当前用于显示“内容”的 TMP（打字机/探索/跳过都写它）
    private TextMeshProUGUI _activeContentText;


    [Header("Special Visibility")]
    public GameObject specialObject; // 你的特殊物体


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
    // Events
    // =========================
    public System.Action OnDialogueOpened;
    public System.Action OnDialogueClosed;

    public bool IsOpen => dialogueUIRoot != null && dialogueUIRoot.activeSelf;

    private bool _endActionFired = false;

    [Header("Pickup Visual (optional)")]
    public Transform pickupSpawnAnchor;   // 生成位置（可拖 Player 或一个空物体）

    // Token 颜色
    private const string TOKEN_COLOR_DEFAULT = "#DF2328"; // 红
    private const string TOKEN_COLOR_CLICKED = "#888888"; // 灰
    
    private Coroutine _uiDisableCo;
    private Coroutine _presentSwitchCo;
    private GameObject _currentPresentationRoot;


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
                // ✅ 开局隐藏整套UI
            if (dialogueUIRoot != null)
                dialogueUIRoot.SetActive(false);

            autoPlay = false;
            _isActive = false;   // ✅ 安全停机：不工作，但也不报错/不崩
            return;
        }

        int start = (overrideStartIndex >= 0) ? overrideStartIndex : currentGroup.startIndex;
        _currentIndex = Mathf.Clamp(start, 0, currentGroup.nodes.Count - 1);

        ClearSelections();
        PlayCurrentNode();
        RefreshSpecial();
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
        string display = BuildContentWithTokenColors(CurrentNode);
        _typingCoroutine = StartCoroutine(TypeLine(display));
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

    void RefreshSpecial()
    {
        if (specialObject == null) return;
        bool rightActive = (rightRoot != null) && rightRoot.activeInHierarchy;
        specialObject.SetActive(!rightActive);
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
        target.text = CurrentNode != null  ? BuildContentWithTokenColors(CurrentNode) : "";
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
            EndDialogue();
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
        if (token.getPart != null)
        {
            bool ok = InventoryManager.I != null && InventoryManager.I.Add(token.getPart);
            Debug.Log($"[DialogueManager] Add Part '{token.getPart.id}' => {ok}");

            if (ok)
            {
                SpawnPickupVisual(token); // ✅ 新增：生成一个“凭空出现的拾取动画”
            }
        }

        if(token.getClue != null)
        {
            if (!string.IsNullOrWhiteSpace(token.getClue))
            {
                if (ClueManager.I == null)
                {
                    Debug.LogError("[DialogueManager] ClueManager.I is null. 场景里要放一个 ClueManager。");
                }
                else
                {
                    bool ok = ClueManager.I.AddClue(token.getClue);
                    Debug.Log($"[DialogueManager] Add Clue '{token.getClue}' => {ok}");
                }
            }
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

    // 把 node.content 里所有 <link=id>...</link> 的“内部文字”上色：未点红、已点灰
    private string BuildContentWithTokenColors(DialogueGroupSO.DialogueNode node)
    {
        if (node == null) return "";
        if (node.tokens == null || node.tokens.Count == 0) return node.content;

        string raw = node.content;

        foreach (var t in node.tokens)
        {
            if (t == null || string.IsNullOrWhiteSpace(t.id)) continue;

            string open = $"<link={t.id}>";
            string close = "</link>";

            int searchFrom = 0;

            // 可能同一个 token 在文本里出现多次，所以用 while 全部处理
            while (true)
            {
                int a = raw.IndexOf(open, searchFrom, StringComparison.Ordinal);
                if (a < 0) break;

                int b = raw.IndexOf(close, a, StringComparison.Ordinal);
                if (b < 0) break;

                int innerStart = a + open.Length;
                string inner = raw.Substring(innerStart, b - innerStart);

                // 如果你手动写过 <color=>，就不覆盖（避免套娃）
                if (!inner.Contains("<color=", StringComparison.OrdinalIgnoreCase))
                {
                    string color = t.clicked ? TOKEN_COLOR_CLICKED : TOKEN_COLOR_DEFAULT;
                    string replacedInner = $"<color={color}>{inner}</color>";

                    raw = raw.Substring(0, innerStart) + replacedInner + raw.Substring(b);
                    // 更新 searchFrom：跳过这次处理过的部分，继续往后找
                    searchFrom = innerStart + replacedInner.Length + close.Length;
                }
                else
                {
                    // 内部本来就有 color，直接跳过这个 token 出现位置
                    searchFrom = b + close.Length;
                }
            }
        }

        return raw;
    }

    private void RefreshContentWithTokenState()
    {
        if (CurrentNode == null) return;

        var node = CurrentNode;
        string display = BuildContentWithTokenColors(node);

        var target = _activeContentText != null ? _activeContentText : dialogueText;
        if (target == null) return;

        target.text = display;
        target.ForceMeshUpdate();
        target.maxVisibleCharacters = target.textInfo.characterCount;

        _isTyping = false;
        _isLineFinished = true;

        if (CurrentHasSelection)
            EnterSelectionMode();

        target.ForceMeshUpdate();

        // 刷新后，保持当前“可见字符数” = 全显示（因为通常点击探索词时你希望看到完整句）
        target.maxVisibleCharacters = target.textInfo.characterCount;
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
        ClearSelections();
        _waitingSelection = true;
        autoPlay = false;
        ShowFixedSelections();   // ✅ 用新的固定按钮逻辑
    }

    private void ShowFixedSelections()
    {
        ClearSelections();

        var node = CurrentNode;
        if (node == null || node.selection == null) return;

        if (node.selection.Count > 0 && choiceLeft != null)
        {
            var sel0 = node.selection[0];
            choiceLeftText.text = sel0.content;
            choiceLeft.onClick.RemoveAllListeners();
            choiceLeft.onClick.AddListener(() => OnSelect(sel0));

            choiceLeft.gameObject.SetActive(true);
            FadeIn(choiceLeft.gameObject);
        }

        if (node.selection.Count > 1 && choiceRight != null)
        {
            var sel1 = node.selection[1];
            choiceRightText.text = sel1.content;
            choiceRight.onClick.RemoveAllListeners();
            choiceRight.onClick.AddListener(() => OnSelect(sel1));

            choiceRight.gameObject.SetActive(true);
            FadeIn(choiceRight.gameObject);
        }
    }

    private void FadeIn(GameObject go)
    {
        var cg = go.GetComponent<CanvasGroup>();
        if (cg == null) return;
        cg.alpha = 0f;
        StartCoroutine(FadeCanvasGroup(cg, 0f, 1f));
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
        if (choiceLeft != null)
            choiceLeft.gameObject.SetActive(false);

        if (choiceRight != null)
            choiceRight.gameObject.SetActive(false);
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

    private void ApplySpeakerUI(Image portrait, Image nameImage, Sprite nameSprite, Sprite portraitSprite)
    {
        if (portrait != null)
        {
            bool has = portraitSprite != null;
            portrait.gameObject.SetActive(has);
            if (has) portrait.sprite = portraitSprite;
        }

        if (nameImage != null)
        {
            bool show = nameSprite != null;
            nameImage.gameObject.SetActive(show);
            if (show) nameImage.sprite = nameSprite;
        }
    }
    private void ApplyPresentation(DialogueGroupSO.DialogueNode node)
    {
        SwitchPresentationRoot(GetTargetRoot(node));

        // 如果你还在用文字内容，就保留；如果你之后要全图片对话，这里会改成 _activeContentImage
        _activeContentText = narrationContentText != null ? narrationContentText : dialogueText;

        if (node == null)
        {
            if (narrationRoot != null) narrationRoot.SetActive(true);
            return;
        }

        var speaker = GetSpeaker(node);

        bool isNarration =
            node.channel == DialogueGroupSO.DialogueChannel.Narration || speaker == null;

        if (isNarration)
        {
            if (narrationRoot != null) narrationRoot.SetActive(true);
            _activeContentText = narrationContentText != null ? narrationContentText : dialogueText;
            return;
        }

        // ✅ Node override 优先（Sprite）
        Sprite finalNameSprite = node.nameOverride != null
            ? node.nameOverride
            : speaker.displayName;

        Sprite finalPortrait = node.portraitOverride != null
            ? node.portraitOverride
            : speaker.defaultPortrait;

        if (node.channel == DialogueGroupSO.DialogueChannel.Left)
        {
            if (leftRoot != null) leftRoot.SetActive(true);
            _activeContentText = leftContentText != null ? leftContentText : dialogueText;

            // 这里要求你把 leftNameText 改成 Image：leftNameImage
            ApplySpeakerUI(leftPortrait, leftNameImage, finalNameSprite, finalPortrait);
        }
        else // Right
        {
            if (rightRoot != null) rightRoot.SetActive(true);
            _activeContentText = rightContentText != null ? rightContentText : dialogueText;

            ApplySpeakerUI(rightPortrait, rightNameImage, finalNameSprite, finalPortrait);
        }
    }
    private GameObject GetTargetRoot(DialogueGroupSO.DialogueNode node)
    {
        if (node == null) return narrationRoot;

        var speaker = GetSpeaker(node);
        bool isNarration = node.channel == DialogueGroupSO.DialogueChannel.Narration || speaker == null;
        if (isNarration) return narrationRoot;

        return (node.channel == DialogueGroupSO.DialogueChannel.Left) ? leftRoot : rightRoot;
    }

    private void SwitchPresentationRoot(GameObject nextRoot)
    {
        if (nextRoot == null) return;
        if (_currentPresentationRoot == nextRoot && nextRoot.activeSelf) return;

        // ✅ 取消上一次切换协程，防止并发切换
        if (_presentSwitchCo != null) StopCoroutine(_presentSwitchCo);
        _presentSwitchCo = StartCoroutine(CoSwitchPresentation(nextRoot));
    }

    private IEnumerator CoSwitchPresentation(GameObject nextRoot)
    {
        // 1) 旧的淡出
        if (_currentPresentationRoot != null && _currentPresentationRoot != nextRoot)
        {
            var oldCg = _currentPresentationRoot.GetComponent<CanvasGroup>();
            if (oldCg != null)
                yield return FadeCanvasGroup(oldCg, oldCg.alpha, 0f);

            _currentPresentationRoot.SetActive(false);
        }

        // 2) 新的淡入
        nextRoot.SetActive(true);
        var newCg = nextRoot.GetComponent<CanvasGroup>();
        if (newCg != null)
        {
            newCg.alpha = 0f;
            yield return FadeCanvasGroup(newCg, 0f, 1f);
        }

        _currentPresentationRoot = nextRoot;
        RefreshSpecial();
    }

    public void HideDialogue()
    {
        _isActive = false;

        if (_typingCoroutine != null)
        {
            StopCoroutine(_typingCoroutine);
            _typingCoroutine = null;
        }

        ClearSelections();
        // 注意：这里别再直接 SetActive(false) 三个 root 了，交给“切换协程”或直接隐藏 UIRoot
        SetAllPresentationOffImmediate(); // 我下面给你一个“立即关 root”的安全版本（不 fade）

        if (dialogueUIRoot != null)
        {
            // ✅ 取消上一次的延迟关闭（最关键）
            if (_uiDisableCo != null) StopCoroutine(_uiDisableCo);

            var cg = dialogueUIRoot.GetComponent<CanvasGroup>();
            if (cg != null)
                StartCoroutine(FadeCanvasGroup(cg, cg.alpha, 0f));

            _uiDisableCo = StartCoroutine(DisableAfterFade(dialogueUIRoot));
        }

        OnDialogueClosed?.Invoke();
    }


    IEnumerator DisableAfterFade(GameObject go)
    {
        yield return new WaitForSeconds(fadeDuration);
        if (go != null) go.SetActive(false);
    }

    // =========================
    // Switch Group at Runtime
    // =========================
    public void PlayGroup(DialogueGroupSO group)
    {
        _endActionFired = false;

        if (group == null || group.nodes == null || group.nodes.Count == 0)
        {
            currentGroup = null;
            if (dialogueText != null)
            {
                dialogueText.text = "(No Dialogue Group)";
                dialogueText.maxVisibleCharacters = int.MaxValue;
            }
            HideDialogue();
            _isActive = false;
            return;
        }

        _isActive = true;

        if (dialogueUIRoot != null)
        {
            // ✅ 取消旧的延迟关闭（最关键）
            if (_uiDisableCo != null) StopCoroutine(_uiDisableCo);

            dialogueUIRoot.SetActive(true);

            var cg = dialogueUIRoot.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.alpha = 0f; // 确保从 0 淡入，不会闪
                StartCoroutine(FadeCanvasGroup(cg, 0f, 1f));
            }
        }

        OnDialogueOpened?.Invoke();

        currentGroup = group;
        int start = (overrideStartIndex >= 0) ? overrideStartIndex : group.startIndex;
        _currentIndex = Mathf.Clamp(start, 0, group.nodes.Count - 1);

        if (_typingCoroutine != null) StopCoroutine(_typingCoroutine);
        _typingCoroutine = null;

        _waitingSelection = false;
        autoPlay = false;
        _isTyping = false;
        _isLineFinished = false;

        ClearSelections();
        PlayCurrentNode();
    }


    // 统一结束入口：保证动作只执行一次
    private void EndDialogue()
    {
        if (_endActionFired) return;
        _endActionFired = true;

        // ✅ 在真正关闭 UI 前，执行当前节点的结束动作
        TryInvokeEndAction(currentGroup);

        // ✅ 对话结束：把 DialogueGroupSO 里配置的线索加进 ClueManager
        AddEndCluesFromGroup(currentGroup);
    
        HideDialogue();
    }

    private void TryInvokeEndAction(DialogueGroupSO group)
    {
        if (group == null) return;

        // 为空就不做任何事
        if (string.IsNullOrWhiteSpace(group.itemObjectName)) return;
        if (string.IsNullOrWhiteSpace(group.functionName)) return;

        var go = GameObject.Find(group.itemObjectName);
        if (go == null)
        {
            Debug.LogWarning($"[DialogueManager] EndAction target not found: {group.itemObjectName}");
            return;
        }

        // ✅ 调用无参函数：public void FunctionName()
        go.SendMessage(group.functionName, SendMessageOptions.DontRequireReceiver);

        // 如果你想调用带一个参数的函数，比如 public void AddItem(string id)
        // 可以把 SendMessage 改成 go.SendMessage(node.functionName, node.itemObjectName, DontRequireReceiver)
    }

    void AddEndCluesFromGroup(DialogueGroupSO group)
    {
        if (group == null || group.endClues == null || group.endClues.Count == 0) return;

        if (ClueManager.I == null)
        {
            Debug.LogError("[DialogueManager] ClueManager.I is null. Cannot add end clues.");
            return;
        }

        if (group.deduplicateEndClues)
        {
            // 去重提交（同一组里重复的 clue 只加一次）
            var set = new System.Collections.Generic.HashSet<string>();
            foreach (var clueId in group.endClues)
            {
                if (string.IsNullOrWhiteSpace(clueId)) continue;
                if (!set.Add(clueId)) continue;

                bool ok = ClueManager.I.AddClue(clueId);
                Debug.Log($"[DialogueManager] EndClue '{clueId}' => {ok}");
            }
        }
        else
        {
            foreach (var clueId in group.endClues)
            {
                if (string.IsNullOrWhiteSpace(clueId)) continue;
                bool ok = ClueManager.I.AddClue(clueId);
                Debug.Log($"[DialogueManager] EndClue '{clueId}' => {ok}");
            }
        }
    }

    void SpawnPickupVisual(DialogueGroupSO.SearchToken token)
    {
        if (token == null) return;

        // 优先用 token 自己的 prefab；没有就用全局默认
        GameObject prefab = token.pickupAnimPrefab;

        if (prefab == null) return;

        Vector3 pos = Vector3.zero;
        if (pickupSpawnAnchor != null) pos = pickupSpawnAnchor.position;
        else pos = transform.position; // 兜底：没锚点就用DialogueManager的位置（你也可换成玩家位置）

        GameObject go = Instantiate(prefab, pos, Quaternion.identity);

        // 关键：播放后让它自己消失
        var anim = go.GetComponent<PickupAnim>();
        if (anim != null)
        {
            anim.PlayAndDisable(() => Destroy(go)); // ✅ 播完禁用后销毁实例
        }
        else
        {
            // prefab 没挂 PickupAnim 就直接给个短命销毁，避免残留
            Destroy(go, 1f);
        }
    }

    IEnumerator FadeCanvasGroup(CanvasGroup cg, float from, float to)
    {
        if (cg == null) yield break;

        cg.alpha = from;

        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            cg.alpha = Mathf.Lerp(from, to, t / fadeDuration);
            yield return null;
        }

        cg.alpha = to;
    }

    private void SetAllPresentationOffImmediate()
    {
        if (narrationRoot != null) narrationRoot.SetActive(false);
        if (leftRoot != null) leftRoot.SetActive(false);
        if (rightRoot != null) rightRoot.SetActive(false);
        _currentPresentationRoot = null;
    }
}
