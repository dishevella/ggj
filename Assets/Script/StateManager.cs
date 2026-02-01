using UnityEngine;

public enum GameState
{
    Exploration,
    Dialogue,
    Task
}
public class StateManager : MonoBehaviour
{
    public static StateManager I { get; private set; }

    [Header("State")]
    public GameState state = GameState.Exploration;

    [Header("Refs")]
    public DialogueManager dialogueManager;
    public PlayerController player;

    void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;

        if (dialogueManager == null)
            dialogueManager = FindFirstObjectByType<DialogueManager>();

        if (player == null)
            player = FindFirstObjectByType<PlayerController>();
    }

    void OnEnable()
    {
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
            case GameState.Exploration:
                UpdateExploration();
                break;

            case GameState.Dialogue:
                UpdateDialogue();
                break;

            case GameState.Task:
                UpdateTask();
                break;
        }
    }

    // =========================
    // External API
    // =========================

    public bool TryStartDialogue(DialogueGroupSO group)
    {
        Debug.Log($"[SM] TryStartDialogue called group={(group ? group.name : "NULL")} state={state} dm={(dialogueManager?dialogueManager.name:"NULL")} isOpen={(dialogueManager?dialogueManager.IsOpen:false)}");

        if (group == null) { Debug.LogWarning("[SM] blocked: group null"); return false; }
        if (dialogueManager == null) { Debug.LogWarning("[SM] blocked: dialogueManager null"); return false; }

        // ✅ 对话中禁止再次触发
        if (state == GameState.Dialogue) { Debug.LogWarning("[SM] blocked: state is Dialogue"); return false; }
        if (dialogueManager.IsOpen) { Debug.LogWarning("[SM] blocked: dialogueManager.IsOpen == true"); return false; }

        ChangeState(GameState.Dialogue);
        Debug.Log("[SM] approved -> calling PlayGroup()");
        dialogueManager.PlayGroup(group);
        return true;
    }


    // =========================
    // State Updates
    // =========================

    void UpdateExploration()
    {
        // 玩家自由移动（PlayerController 负责）
    }

    void UpdateDialogue()
    {
        // 推进逻辑在 DialogueManager 内
    }

    void UpdateTask()
    {
        // Task 是“锁定态”，具体逻辑交给 Task / Boss / RoundController
    }

    // =========================
    // State Control
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
                SetPlayerMove(false);
                break;
        }
    }

    void ExitState(GameState s)
    {
        // 当前无清理需求
    }

    // =========================
    // Dialogue Callbacks
    // =========================

    void HandleDialogueOpened()
    {
        if (state != GameState.Dialogue)
            ChangeState(GameState.Dialogue);
    }

    void HandleDialogueClosed()
    {
        if (state == GameState.Dialogue)
            ChangeState(GameState.Exploration);
    }

    // =========================
    // Helpers
    // =========================

    void SetPlayerMove(bool canMove)
    {
        if (player != null)
            player.canMove = canMove;
    }

    // =========================
    // Optional External API
    // =========================

    public void EnterTask()
    {
        ChangeState(GameState.Task);
    }

    public void ExitTask()
    {
        ChangeState(GameState.Exploration);
    }
}
