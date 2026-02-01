using UnityEngine;
using TMPro;

public class EmotionGradePanelUI : MonoBehaviour
{
    [Header("Refs")]
    public PlayerExpressionState player;
    public InventoryUI inventoryUI; // 可不拖：自动找
    public CanvasGroup cg;          // 挂在 EmotionRatePanel 上

    [Header("Texts")]
    public TMP_Text hostilityText;
    public TMP_Text angerText;
    public TMP_Text sadnessText;
    public TMP_Text dominanceText;
    public TMP_Text honestyText;

    [Header("Grade Thresholds (by ABS value)")]
    public int S_minAbs = 8;
    public int A_minAbs = 6;
    public int B_minAbs = 4;
    public int C_minAbs = 2;

    void Awake()
    {
        if (!cg) cg = GetComponent<CanvasGroup>();
        if (!inventoryUI) inventoryUI = FindFirstObjectByType<InventoryUI>();

        // 关键：这里就订阅（不依赖面板显示与否）
        InventoryUI.OnInventoryOpenChanged += OnInventoryOpenChanged;

        if (player != null)
            player.OnChanged += Refresh;

        SetVisible(false);
        Refresh();

        Debug.Log("[EmotionGradePanelUI] Awake subscribed.");
    }

    void OnDestroy()
    {
        InventoryUI.OnInventoryOpenChanged -= OnInventoryOpenChanged;

        if (player != null)
            player.OnChanged -= Refresh;

        Debug.Log("[EmotionGradePanelUI] Unsubscribed.");
    }

    void OnInventoryOpenChanged(bool open)
    {
        Debug.Log("[EmotionGradePanelUI] inventory open = " + open);
        SetVisible(open);
        if (open) Refresh();
    }

    void SetVisible(bool show)
    {
        if (!cg) return;
        cg.alpha = show ? 1f : 0f;
        cg.blocksRaycasts = show;
        cg.interactable = show;
    }

    public void Refresh()
    {
        if (player == null) return;

        EmotionVector v = player.GetTotal();

        if (hostilityText) hostilityText.text = $"Hostility-{ToGrade(v.hostility)}";
        if (angerText) angerText.text = $"Serenity-{ToGrade(v.anger)}";
        if (sadnessText) sadnessText.text = $"Sadness-{ToGrade(v.sadness)}";
        if (dominanceText) dominanceText.text = $"Docile-{ToGrade(v.dominance)}";
        if (honestyText) honestyText.text = $"Hypocrisy-{ToGrade(v.honesty)}";
    }

    string ToGrade(int value)
    {
        int a = Mathf.Abs(value);
        if (a >= S_minAbs) return "S";
        if (a >= A_minAbs) return "A";
        if (a >= B_minAbs) return "B";
        if (a >= C_minAbs) return "C";
        return "D";
    }
}
