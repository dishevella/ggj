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
    

    void Awake()
    {
        if (submitButton) submitButton.onClick.AddListener(Submit);
        ShowMemo(true);
        LockBackground(true);
        SetResult(false, "");
        UpdateTimerUI(roundSeconds);
    }
    void Start()
    {
        
        StartRound();
    }
    public void StartRound()
    {
        _timeLeft = roundSeconds;
        _running = true;
        ShowMemo(false);
        LockBackground(false);
        SetResult(false, "");
        UpdateTimerUI(_timeLeft);
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

    public void Submit()
    {
        if (!_running) return;
        _running = false;
        
        EvaluateAndApply(auto: false);
    }

    void EvaluateAndApply(bool auto)
    {
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

        if (pass)
        {
            SetResult(true, $"PASS\n偏差={totalDev}");
            // TODO: 播放结束动画 / 进入下一关
        }
        else
        {
            bool dead = (vital != null && vital.IsDead);
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

        Debug.Log($"[Round] auto={auto} pass={pass} totalDev={totalDev} dmg={dmg} " +
                  $"player={playerVec} target={boss.target}");
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
