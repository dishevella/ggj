using UnityEngine;

public class BossJudge : MonoBehaviour
{
    public BossProfile boss;
    public PlayerExpressionState player;

    public bool Evaluate(out int totalDev, out EmotionVector playerTotal)
    {
        playerTotal = player.GetTotal();

        int d1 = Mathf.Abs(playerTotal.hostility - boss.target.hostility);
        int d2 = Mathf.Abs(playerTotal.anger - boss.target.anger);
        int d3 = Mathf.Abs(playerTotal.sadness - boss.target.sadness);
        int d4 = Mathf.Abs(playerTotal.dominance - boss.target.dominance);
        int d5 = Mathf.Abs(playerTotal.honesty - boss.target.honesty);

       
        if (boss.passPerAxisDeviation > 0)
        {
            if (d1 > boss.passPerAxisDeviation) { totalDev = d1 + d2 + d3 + d4 + d5; return false; }
            if (d2 > boss.passPerAxisDeviation) { totalDev = d1 + d2 + d3 + d4 + d5; return false; }
            if (d3 > boss.passPerAxisDeviation) { totalDev = d1 + d2 + d3 + d4 + d5; return false; }
            if (d4 > boss.passPerAxisDeviation) { totalDev = d1 + d2 + d3 + d4 + d5; return false; }
            if (d5 > boss.passPerAxisDeviation) { totalDev = d1 + d2 + d3 + d4 + d5; return false; }
        }

        totalDev = d1 + d2 + d3 + d4 + d5;
        return totalDev <= boss.passTotalDeviation;
    }

    
    public void Submit()
    {
        bool pass = Evaluate(out int total, out var vec);
        Debug.Log($"[Judge] player={vec.hostility},{vec.anger},{vec.sadness},{vec.dominance},{vec.honesty}  totalDev={total}  pass={pass}");

        if (pass)
        {
            // TODO: 播放胜利动画 / 进入下一关
        }
        else
        {
            // TODO: 失败逻辑（清零？san值？被驱逐？）
        }
    }
}
