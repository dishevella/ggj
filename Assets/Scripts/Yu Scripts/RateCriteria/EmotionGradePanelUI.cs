using UnityEngine;
using TMPro;

public class EmotionGradePanelUI : MonoBehaviour
{
    [Header("Refs")]
    public PlayerExpressionState player;
    public InventoryUI inventoryUI;
    public CanvasGroup cg;

    [Header("Texts")]
    public TMP_Text hostilityText;
    public TMP_Text angerText;
    public TMP_Text sadnessText;
    public TMP_Text dominanceText;
    public TMP_Text honestyText;

    [Header("Display Range")]
    public int minValue = -10;
    public int maxValue = 10;

    void Awake()
    {
        if (!cg) cg = GetComponent<CanvasGroup>();
        if (!inventoryUI) inventoryUI = FindFirstObjectByType<InventoryUI>();

        InventoryUI.OnInventoryOpenChanged += OnInventoryOpenChanged;

        if (player != null)
            player.OnChanged += Refresh;

        SetVisible(false);
        Refresh();
    }

    void OnDestroy()
    {
        InventoryUI.OnInventoryOpenChanged -= OnInventoryOpenChanged;

        if (player != null)
            player.OnChanged -= Refresh;
    }

    void OnInventoryOpenChanged(bool open)
    {
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

        if (hostilityText) hostilityText.text = FormatAxis("Hostility-Friendliness", v.hostility, "Hostility", "Friendliness");
        if (angerText) angerText.text = FormatAxis("Anger-Serenity", v.anger, "Anger", "Serenity");
        if (sadnessText) sadnessText.text = FormatAxis("Sadness-Joy", v.sadness, "Sadness", "Joy");
        if (dominanceText) dominanceText.text = FormatAxis("Docile-Dominant", v.dominance, "Docile", "Dominant");
        if (honestyText) honestyText.text = FormatAxis("Hypocrisy-Honesty", v.honesty, "Hypocrisy", "Honesty");
    }

    string FormatAxis(string title, int raw, string negLabel, string posLabel)
    {
        int val = Mathf.Clamp(raw, minValue, maxValue);

        string side = val < 0 ? negLabel : (val > 0 ? posLabel : "Neutral");
        string sign = val > 0 ? "+" : ""; // »√’˝ ˝œ‘ æ +3
        return $"{title}: {sign}{val} ({side})";
    }
}
