using UnityEngine;

public enum GameState
{
    Exploration,
    Scan,
    Dialogue,
    Cutscene,
    Task
}

public enum TaskPhase
{
    None,
    Question,
    Adjust,
    Result
}

public class StateManager : MonoBehaviour
{
    public static StateManager I { get; private set; }

    [Header("State")]
    public GameState state = GameState.Exploration;
    public TaskPhase taskPhase = TaskPhase.None;

    [Header("Refs")]
    public DialogueManager dialogueManager;
    public PlayerController player; // 你的2D玩家控制器（里面有 canMove）
    
    void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;

        if (dialogueManager == null) dialogueManager = FindFirstObjectByType<DialogueManager>();
        if (player == null) player = FindFirstObjectByType<PlayerController>();
    }

    void OnEnable()
    {
        // ✅ 监听对话结束：自动回到探索
        if (dialogueManager != null)
        {
            dialogueManager.OnDialogueOpened += HandleDialogueOpened;
            dialogueManager.OnDialogueClosed += HandleDialogueClosed;
        }
    }

    void OnDisable()
    {
        if (dialogueManager != null)
        {
            dialogueManager.OnDialogueOpened -= HandleDialogueOpened;
            dialogueManager.OnDialogueClosed -= HandleDialogueClosed;
        }
    }

    void Update()
    {
        switch (state)
        {
            case GameState.Exploration: UpdateExploration(); break;
            case GameState.Dialogue:    UpdateDialogue();    break;
            case GameState.Scan:        UpdateScan();        break;
            case GameState.Cutscene:    UpdateCutscene();    break;
            case GameState.Task:        UpdateTask();        break;
        }
    }

    // =========================
    // External API (ClickNPC 会调用)
    // =========================

    public bool TryStartDialogue(DialogueGroupSO group)
    {
        if (group == null) return false;
        if (dialogueManager == null) return false;

        // ✅ 对话中禁止再次触发
        if (state == GameState.Dialogue) return false;
        if (dialogueManager.IsOpen) return false;

        ChangeState(GameState.Dialogue);
        dialogueManager.PlayGroup(group); // PlayGroup 内会打开 UI
        return true;
    }

    // =========================
    // State Update Methods
    // =========================

    void UpdateExploration()
    {
        // 移动逻辑在 PlayerController 里做，这里一般不用写
        // 如果你还有“按键进入 Scan”等，就写在这里（但要注意别在 Dialogue 状态触发）
    }

    void UpdateDialogue()
    {
        // 对话推进由 DialogueManager 自己处理（鼠标点击/选项）
        // 这里一般也不写
        // 如果你想加 ESC 退出对话，可以在这里判断后 dialogueManager.HideDialogue();
    }

    void UpdateScan() { }
    void UpdateCutscene() { }

    void UpdateTask()
    {
        switch (taskPhase)
        {
            case TaskPhase.Question: UpdateTaskQuestion(); break;
            case TaskPhase.Adjust:   UpdateTaskAdjust();   break;
            case TaskPhase.Result:   UpdateTaskResult();   break;
        }
    }

    void UpdateTaskQuestion() { }
    void UpdateTaskAdjust() { }
    void UpdateTaskResult() { }

    // =========================
    // State Change API
    // =========================

    public void ChangeState(GameState newState)
    {
        if (state == newState) return;

        ExitState(state);
        state = newState;
        EnterState(state);
    }

    void EnterState(GameState s)
    {
        switch (s)
        {
            case GameState.Exploration:
                SetPlayerMove(true);
                break;

            case GameState.Dialogue:
                SetPlayerMove(false);
                break;

            case GameState.Task:
                taskPhase = TaskPhase.Question;
                SetPlayerMove(false);
                break;
        }
    }

    void ExitState(GameState s)
    {
        // 目前 Exploration/Dialogue 不需要额外清理
    }

    // =========================
    // Dialogue Callbacks
    // =========================

    void HandleDialogueOpened()
    {
        // 确保状态一致（例如你将来可能从别的地方打开对话）
        if (state != GameState.Dialogue)
            ChangeState(GameState.Dialogue);
    }

    void HandleDialogueClosed()
    {
        // ✅ 对话结束 -> 回探索
        if (state == GameState.Dialogue)
            ChangeState(GameState.Exploration);
    }

    void SetPlayerMove(bool canMove)
    {
        if (player != null)
            player.canMove = canMove;
    }
}
