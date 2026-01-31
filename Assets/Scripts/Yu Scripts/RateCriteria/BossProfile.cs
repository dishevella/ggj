using UnityEngine;

[CreateAssetMenu(menuName = "Boss/Boss Profile")]
public class BossProfile : ScriptableObject
{
    public string bossName;
    public EmotionVector target;        // Boss要求的5维标准
    public int passTotalDeviation = 8;  // 总偏差阈值
    public int passPerAxisDeviation = 3;// 单维偏差阈值
}
