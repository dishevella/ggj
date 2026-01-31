using UnityEngine;
using TMPro;

public class PlayerVital : MonoBehaviour
{
    public int maxHP = 100;
    public int hp = 100;

    [Header("UI (optional)")]
    public TMP_Text hpText;

    void Awake()
    {
        hp = Mathf.Clamp(hp, 0, maxHP);
        RefreshUI();
    }

    public void Damage(int amount)
    {
        amount = Mathf.Max(0, amount);
        hp = Mathf.Clamp(hp - amount, 0, maxHP);
        RefreshUI();
        Debug.Log($"[HP] -{amount}, hp={hp}/{maxHP}");
    }

    public bool IsDead => hp <= 0;

    public void Heal(int amount)
    {
        amount = Mathf.Max(0, amount);
        hp = Mathf.Clamp(hp + amount, 0, maxHP);
        RefreshUI();
    }

    void RefreshUI()
    {
        if (hpText) hpText.text = $"{hp}/{maxHP}";
    }
}
