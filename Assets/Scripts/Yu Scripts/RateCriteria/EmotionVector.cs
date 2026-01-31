using UnityEngine;

[System.Serializable]
public struct EmotionVector
{
    public int hostility;  // -µÐÒâ +ÓÑÉÆ
    public int anger;      // -·ßÅ­ +Æ½¾²
    public int sadness;    // -±¯ÉË +¿ªÐÄ
    public int dominance;  // -Ë³´Ó +Ö§Åä
    public int honesty;    // -ÐéÎ± +³ÏÊµ

    public static EmotionVector Zero => new EmotionVector();

    public static EmotionVector operator +(EmotionVector a, EmotionVector b)
    {
        a.hostility += b.hostility;
        a.anger += b.anger;
        a.sadness += b.sadness;
        a.dominance += b.dominance;
        a.honesty += b.honesty;
        return a;
    }
}
