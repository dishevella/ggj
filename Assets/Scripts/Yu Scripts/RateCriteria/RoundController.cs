using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RoundController : MonoBehaviour
{
    [Header("Data")]
    public BossProfile boss;
    public PlayerExpressionState player;
    public PlayerVital vital;

    [Header("Round")]
    public float roundSeconds = 30f;

    [Header("Damage Rule")]
    public int freeDev = 5;         // 允许的免费偏差
    public int damagePerDev = 2;    // 每超1偏差扣多少血

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
    public float resultShowSeconds = 2f;     // 结果显示多久后消失
    public bool hideTimerAfterResult = true; // 是否隐藏倒计时文字
    public bool autoNextRoundOnFail = false; // 失败但没死：是否自动下一轮
    public float nextRoundDelay = 1.0f;      // 下一轮延迟
    Coroutine _resultCR;


    void Awake()
    {
        if (submitButton) submitButton.onClick.AddListener(Submit);
        InventoryUI.OnInventoryOpenChanged += OnInventoryOpenChanged;
        ShowMemo(true);
        LockBackground(true);
        SetResult(false, "");
        UpdateTimerUI(roundSeconds);
        var inv = FindFirstObjectByType<InventoryUI>();
        OnInventoryOpenChanged(inv != null && inv.IsOpen);
    }
    void Start()
    {
        StartRound();
    }
    public void StartRound()
    {
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
            // 背包开 -> 显示；背包关 -> 隐藏
            submitButton.gameObject.SetActive(open);

            // 可选：如果你希望“背包开但当前不在回合中”也不能点
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
        //FindFirstObjectByType<InventoryUI>()?.SetInputLocked(true);
        if (submitButton) submitButton.interactable = false;
        // 1) 计算玩家当前总五维
        EmotionVector playerVec = player.GetTotal();

        // 2) 算偏差（绝对值之和）
        int d1 = Mathf.Abs(playerVec.hostility - boss.target.hostility);
        int d2 = Mathf.Abs(playerVec.anger - boss.target.anger);
        int d3 = Mathf.Abs(playerVec.sadness - boss.target.sadness);
        int d4 = Mathf.Abs(playerVec.dominance - boss.target.dominance);
        int d5 = Mathf.Abs(playerVec.honesty - boss.target.honesty);
        int totalDev = d1 + d2 + d3 + d4 + d5;

        // 3) 判定本轮是否成功（你也可以直接用 totalDev <= 阈值）
        bool pass = totalDev <= boss.passTotalDeviation;

        // 4) 如果失败：根据偏差扣血
        int dmg = 0;
        if (!pass)
        {
            int over = Mathf.Max(0, totalDev - freeDev);
            dmg = over * damagePerDev;
            if (vital) vital.Damage(dmg);
        }

        // 5) 结果展示
        LockBackground(true);
        bool dead = false;
        if (pass)
        {
            SetResult(true, $"PASS\nDeviation={totalDev}");
            // TODO: 播放结束动画 / 进入下一关
        }
        else
        {
            dead = (vital != null && vital.IsDead);
            if (dead)
            {
                SetResult(true, $"FAIL (GAME OVER)\n Deviation ={totalDev}\nDeduction={dmg}");
                // TODO: 游戏失败/被驱逐/回主菜单
            }
            else
            {
                SetResult(true, $"FAIL\n Deviation={totalDev}\nDeduction={dmg}\nHP={vital.hp}/{vital.maxHP}");
                // TODO: 允许继续下一轮：显示MEMO -> 重开
                // 例：2秒后自动开始下一轮
                //Invoke(nameof(StartRoundWithMemo), 1.0f);
            }
        }
        if (_resultCR != null) StopCoroutine(_resultCR);
        _resultCR = StartCoroutine(Co_AfterResult(pass, dead));

        Debug.Log($"[Round] auto={auto} pass={pass} totalDev={totalDev} dmg={dmg} " +
                  $"player={playerVec} target={boss.target}");
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
    // 等一会让玩家看到结果
    yield return new WaitForSeconds(resultShowSeconds);

    // 隐藏结果 & 倒计时
    HideResultAndTimer();

    // 这里决定下一步做什么（按你想要的流程）
    if (dead)
    {
        // Game Over：你可以在这里回主菜单/重开等
        yield break;
    }

    if (!pass && autoNextRoundOnFail)
    {
        // 失败但没死：自动下一轮
        yield return new WaitForSeconds(nextRoundDelay);
        StartRoundWithMemo(); // 或 StartRound()
    }
    // pass 的话：你可以在这里进入下一关 / 播动画
}

    void StartRoundWithMemo()
    {
        ShowMemo(true);
        // 你也可以要求玩家点“继续”按钮再开始
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
}
