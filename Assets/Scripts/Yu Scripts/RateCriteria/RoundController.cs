using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RoundController : MonoBehaviour
{
    public StateManager SM;
    [Header("Data")]
    public BossProfile boss;
    public PlayerExpressionState player;
    public PlayerVital vital;

    [Header("Round")]
    public float roundSeconds = 30f;

    [Header("Damage Rule")]
    public int freeDev = 5;         // ���������ƫ��
    public int damagePerDev = 2;    // ÿ��1ƫ��۶���Ѫ

    [Header("UI")]
    public GameObject memoPanel;
    public CanvasGroup backgroundLock;
    public TMP_Text timerText;
    public Button submitButton;
    public GameObject resultPanel;
    public TMP_Text resultText;

    float _timeLeft;
    bool _running;

    [Header("UI Rules")]
    public bool showSubmitOnlyWhenInventoryOpen = true;
    [Header("After Result")]
    public float resultShowSeconds = 2f;     // �����ʾ��ú���ʧ
    public bool hideTimerAfterResult = true; // �Ƿ����ص���ʱ����
    public bool autoNextRoundOnFail = false; // ʧ�ܵ�û�����Ƿ��Զ���һ��
    public float nextRoundDelay = 1.0f;      // ��һ���ӳ�
    Coroutine _resultCR;

    [Header("After Battle Dialogue")]
    public DialogueGroupSO winDialogue;
    public DialogueGroupSO failDialogue;      // FAIL 但没死（还能继续）
    public DialogueGroupSO deadDialogue;      // FAIL 且死了（GameOver）

    [Tooltip("结算后延迟多久再弹出对话（给 UI 显示 PASS/FAIL 的时间）")]
    public float dialogueDelayAfterResult = 0.1f;

    private bool _battleFinishedFired = false; // 防止重复触发


    void Awake()
    {
        if (submitButton) submitButton.onClick.AddListener(Submit);
        InventoryUI.OnInventoryOpenChanged += OnInventoryOpenChanged;
        ShowMemo(true);
        LockBackground(true);
        SetResult(false, "");
        // ���ֲ���ʾ��ʱ��
        SetTimerVisible(false);
        var inv = FindFirstObjectByType<InventoryUI>();
        OnInventoryOpenChanged(inv != null && inv.IsOpen);

    }
    void Start()
    {
        
    }
    public void StartRound()
    {
        _battleFinishedFired = false;   // ✅ 每轮开始重置
        SM.EnterTask();
        //FindFirstObjectByType<InventoryUI>()?.SetInputLocked(false);
        SetTimerVisible(true);
        _timeLeft = roundSeconds;
        _running = true;
        ShowMemo(false);
        LockBackground(false);
        SetResult(false, "");
        UpdateTimerUI(_timeLeft);
        if (submitButton && submitButton.gameObject.activeSelf)
            submitButton.interactable = true;
    }
    void OnDestroy()
    {
        InventoryUI.OnInventoryOpenChanged -= OnInventoryOpenChanged;
    }

    void Update()
    {
        if (!_running) return;

        _timeLeft -= Time.deltaTime;
        if (_timeLeft < 0f) _timeLeft = 0f;

        UpdateTimerUI(_timeLeft);

        if (_timeLeft <= 0f)
        {
            _running = false;
            EvaluateAndApply(auto: true);
        }
    }
    void OnInventoryOpenChanged(bool open)
    {
        if (!showSubmitOnlyWhenInventoryOpen) return;

        if (submitButton)
        {
            // ������ -> ��ʾ�������� -> ����
            submitButton.gameObject.SetActive(open);

            // ��ѡ�������ϣ��������������ǰ���ڻغ��С�Ҳ���ܵ�
            submitButton.interactable = open && _running;
        }
    }

    public void Submit()
    {
        if (!_running) return;
        _running = false;
        
        EvaluateAndApply(auto: false);
    }

    void EvaluateAndApply(bool auto)
    {   
        // ✅ 防重必须放最前面（Submit/Timeout 同帧触发时很关键）
        if (_battleFinishedFired) return;
        _battleFinishedFired = true;

        //FindFirstObjectByType<InventoryUI>()?.SetInputLocked(true);
        if (submitButton) submitButton.interactable = false;
        // 1) ������ҵ�ǰ����ά
        EmotionVector playerVec = player.GetTotal();

        // 2) ��ƫ�����ֵ֮�ͣ�
        int d1 = Mathf.Abs(playerVec.hostility - boss.target.hostility);
        int d2 = Mathf.Abs(playerVec.anger - boss.target.anger);
        int d3 = Mathf.Abs(playerVec.sadness - boss.target.sadness);
        int d4 = Mathf.Abs(playerVec.dominance - boss.target.dominance);
        int d5 = Mathf.Abs(playerVec.honesty - boss.target.honesty);
        int totalDev = d1 + d2 + d3 + d4 + d5;

        // 3) �ж������Ƿ�ɹ�����Ҳ����ֱ���� totalDev <= ��ֵ��
        bool pass = totalDev <= boss.passTotalDeviation;

        // 4) ���ʧ�ܣ�����ƫ���Ѫ
        int dmg = 0;
        if (!pass)
        {
            int over = Mathf.Max(0, totalDev - freeDev);
            dmg = over * damagePerDev;
            if (vital) vital.Damage(dmg);
        }

        // 5) ���չʾ
        LockBackground(true);
        bool dead = false;
        if (pass)
        {
            SetResult(true, $"PASS\nDeviation={totalDev}");
            // TODO: ���Ž������� / ������һ��
        }
        else
        {
            dead = (vital != null && vital.IsDead);
            if (dead)
            {
                SetResult(true, $"FAIL (GAME OVER)\n Deviation ={totalDev}\nDeduction={dmg}");
            }
            else
            {
                SetResult(true, $"FAIL\n Deviation={totalDev}\nDeduction={dmg}\nHP={vital.hp}/{vital.maxHP}");
                // TODO: ����������һ�֣���ʾMEMO -> �ؿ�
                // ����2����Զ���ʼ��һ��
                //Invoke(nameof(StartRoundWithMemo), 1.0f);
            }
        }
        if (_resultCR != null) StopCoroutine(_resultCR);
        _resultCR = StartCoroutine(Co_AfterResult(pass, dead));

        Debug.Log($"[Round] auto={auto} pass={pass} totalDev={totalDev} dmg={dmg} " +
                  $"player={playerVec} target={boss.target}");

        // ✅ 在协程里处理：隐藏结果、退出 Task、弹对话
        if (_resultCR != null) StopCoroutine(_resultCR);
        _resultCR = StartCoroutine(Co_AfterResult(pass, dead));

    }
    void SetTimerVisible(bool show)
{
    if (!timerText) return;
    timerText.gameObject.SetActive(show);
}

void HideResultAndTimer()
{
    SetResult(false, "");
    if (hideTimerAfterResult) SetTimerVisible(false);
}

System.Collections.IEnumerator Co_AfterResult(bool pass, bool dead)
{
    // 先让 PASS/FAIL 面板显示一会儿
    yield return new WaitForSeconds(resultShowSeconds);

    HideResultAndTimer();

    // ✅ 选择要播放的对话
    DialogueGroupSO next = null;
    if (pass) next = winDialogue;
    else if (dead) next = deadDialogue;
    else next = failDialogue;

    Debug.Log($"[RC] Co_AfterResult ENTER pass={pass} dead={dead}", this);

    yield return new WaitForSeconds(resultShowSeconds);

    Debug.Log($"[RC] pick next = {(next ? next.name : "NULL")}", this);

    // ✅ 退出 Task（无论胜负/死亡都退），再开对话
    // 给一帧/一点点延迟，避免 UI 状态同帧冲突
    yield return new WaitForSeconds(dialogueDelayAfterResult);

    // 如果你想：死亡时不弹对话，而是直接 GameOver UI，就在这里 return
    // if (dead) yield break;
    Debug.Log("[RC] about to PlayAfterDialogue()", this);
    PlayAfterDialogue(next);

    // ✅ 如果失败且没死，并且你想自动下一轮，那么应该等对话结束再开始
    // 这个最好交给对话的 EndAction 或者 StateManager 的回调做
}

    void StartRoundWithMemo()
    {
        ShowMemo(true);
        // ��Ҳ����Ҫ����ҵ㡰��������ť�ٿ�ʼ
        Invoke(nameof(StartRound), 1.0f);
    }

    void ShowMemo(bool show)
    {
        if (memoPanel) memoPanel.SetActive(show);
    }

    void LockBackground(bool locked)
    {
        if (!backgroundLock) return;
        backgroundLock.alpha = locked ? 1f : 0f;
        backgroundLock.blocksRaycasts = locked;
        backgroundLock.interactable = locked;
    }

    void UpdateTimerUI(float t)
    {
        if (!timerText) return;
        timerText.text = Mathf.CeilToInt(t).ToString();
    }

    void SetResult(bool show, string text)
    {
        if (resultPanel) resultPanel.SetActive(show);
        if (resultText) resultText.text = text;

    }
    bool _bossActive;

    public void TriggerBossBattle(BossProfile bossProfile, float? seconds = null)
    {
        if (_bossActive) return; // �Ѿ���Bossս�����ظ�����

        _bossActive = true;

        if (bossProfile != null) boss = bossProfile;
        if (seconds.HasValue) roundSeconds = seconds.Value;

        Debug.Log($"[BossBattle] START boss={(boss ? boss.name : "NULL")}");
        StartRound();
    }


    void PlayAfterDialogue(DialogueGroupSO group)
    {
        if (group == null) return;
        if (StateManager.I == null)
        {
            Debug.LogError("[RoundController] StateManager.I is null.");
            return;
        }

        // ✅ 确保退出 Task 后再开对话（避免 Click/Task 输入冲突）
        StateManager.I.ExitTask();

        // ✅ 直接让 StateManager 打开对话（它会切到 Dialogue 状态并锁玩家移动）
        StateManager.I.TryStartDialogue(group);
    }
}
