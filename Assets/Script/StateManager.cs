using UnityEngine;

public enum GameState
{
    Exploration, // 街道自由行动
    Scan,        // 拍照 / 搜索
    Dialogue,    // 对话 / 文本搜索
    Cutscene,    // 过场 / 结算
    Task         // 统一任务态（面试 / 社交等）
}

public enum TaskPhase
{
    None,        // 未处于任务
    Question,   // NPC 提问 / 反馈
    Adjust,     // 玩家调整表情
    Result      // 本轮结算（过渡）
}

public class StateManager : MonoBehaviour
{
    [Header("State")]
    public GameState state = GameState.Exploration;
    public TaskPhase taskPhase = TaskPhase.None;

    void Update()
    {
        switch (state)
        {
            case GameState.Exploration: UpdateExploration();    break;
            case GameState.Scan:        UpdateScan();           break;
            case GameState.Dialogue:    UpdateDialogue();       break;
            case GameState.Cutscene:    UpdateCutscene();       break;
            case GameState.Task:        UpdateTask();           break;
        }
    }

    // =========================
    // State Update Methods
    // =========================
    void UpdateExploration()
    {
        // 移动 / 进入扫描 / 进入对话
    }

    void UpdateScan()
    {
        // 拍照逻辑
    }

    void UpdateDialogue()
    {
        // 对话推进
    }

    void UpdateCutscene()
    {
        // 禁止输入，等动画/计时结束
    }

    void UpdateTask()
    {
        switch (taskPhase)
        {
            case TaskPhase.Question:    UpdateTaskQuestion();   break;
            case TaskPhase.Adjust:      UpdateTaskAdjust();     break;
            case TaskPhase.Result:      UpdateTaskResult();     break;
        }
    }

    // =========================
    // Task Phase Update
    // =========================

    void UpdateTaskQuestion()
    {
        // NPC 发问 / 给反馈
    }

    void UpdateTaskAdjust()
    {
        // 玩家限时调整表情
    }

    void UpdateTaskResult()
    {
        // 计算满意度，决定下一轮 or 结束任务
    }

    // =========================
    // State Change API（非常重要）
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
            case GameState.Task:
                taskPhase = TaskPhase.Question;
                break;
        }
    }

    void ExitState(GameState s)
    {
        // 清理 UI / 解锁输入 / 停止计时器
    }
}
