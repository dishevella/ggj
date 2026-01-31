using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 角色部位槽位：支持拖拽装备、替换、右键卸下
/// - 拖入一个 Part：若类型匹配，则装备
/// - 若槽位已有装备：先把旧装备退回背包（背包满/重复则拒绝替换，防丢）
/// - 装备成功：从背包移除该 part，更新槽位图片，写入 PlayerExpressionState
/// - 右键点击槽位：卸下当前装备并放回背包（背包满/重复则拒绝卸下）
/// </summary>
public class PartSocketUI : MonoBehaviour,
    IDropHandler, IPointerEnterHandler, IPointerClickHandler
{
    [Header("Socket")]
    public Image targetImage;
    public PartType acceptType;                 // Eyes/Nose/Mouth/Other
    public PlayerExpressionState expressionState;

    [Header("Visual")]
    [Tooltip("空槽位时的透明度(0=完全看不到)")]
    [Range(0f, 1f)] public float emptyAlpha = 0f;

    [Tooltip("装备后图片颜色(一般保持白色)")]
    public Color equippedColor = Color.white;

    // 当前槽位装备的部件（用于替换/卸下）
    PartDefinition _equipped;

    void Awake()
    {
        SetEmptyVisual();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        Debug.Log("[Socket] PointerEnter: " + gameObject.name);
    }

    /// <summary>
    /// 右键卸下
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Right) return;

        if (_equipped == null)
        {
            Debug.Log("[Socket] RightClick unequip: nothing equipped");
            return;
        }

        // 退回背包（背包满/重复 -> 拒绝卸下，防丢）
        bool ok = InventoryManager.I != null && InventoryManager.I.Add(_equipped);
        Debug.Log("[Socket] unequip -> back to bag=" + ok);

        if (!ok)
        {
            Debug.LogWarning("[Socket] Cannot unequip (bag full or duplicate).");
            return;
        }

        // 清状态
        expressionState?.Unequip(acceptType);
        _equipped = null;

        // 清UI
        SetEmptyVisual();

        // 刷新背包 UI
        FindFirstObjectByType<InventoryUI>()?.Refresh();
    }

    /// <summary>
    /// 拖拽装备
    /// </summary>
    public void OnDrop(PointerEventData eventData)
    {
        Debug.Log("[Socket] OnDrop: " + gameObject.name);

        var pd = eventData.pointerDrag;
        Debug.Log("[Socket] pointerDrag=" + (pd ? pd.name : "NULL"));

        var slot = pd ? pd.GetComponentInParent<InventorySlotUI>() : null;
        Debug.Log("[Socket] slot=" + (slot ? slot.name : "NULL"));

        var part = slot ? slot.Part : null;
        Debug.Log("[Socket] part=" + (part ? part.id : "NULL"));

        if (part == null) return;

        Debug.Log("[Socket] accept=" + acceptType + " partType=" + part.type);

        // 类型不匹配直接拒绝
        if (part.type != acceptType)
        {
            Debug.Log("[Socket] type mismatch -> reject");
            return;
        }

        if (targetImage == null)
        {
            Debug.LogError("[Socket] targetImage is NULL (drag the Image here)");
            return;
        }

        // ✅ 如果拖入的是当前已经装备的同一个对象，直接忽略
        if (_equipped == part)
        {
            Debug.Log("[Socket] same part already equipped -> ignore");
            return;
        }

        // ✅ 1) 若槽位已有装备：先退回背包（失败则拒绝替换，防丢）
        if (_equipped != null)
        {
            bool backOk = InventoryManager.I != null && InventoryManager.I.Add(_equipped);
            Debug.Log("[Socket] return old to bag=" + backOk);

            if (!backOk)
            {
                Debug.LogWarning("[Socket] Bag full or duplicate -> cannot replace.");
                return;
            }
        }

        // ✅ 2) 从背包移除新装备（若移除失败则拒绝，防止复制/异常）
        bool removed = InventoryManager.I != null && InventoryManager.I.Remove(part.id);
        Debug.Log("[Socket] removedFromBag=" + removed);

        if (!removed)
        {
            Debug.LogWarning("[Socket] Part not found in bag -> reject.");
            return;
        }

        // ✅ 3) 更新槽位图片显示
        var spriteToEquip = part.equipSprite ? part.equipSprite : part.originalSprite;
        Debug.Log("[Socket] spriteToEquip=" + (spriteToEquip ? spriteToEquip.name : "NULL"));

        targetImage.sprite = spriteToEquip;
        targetImage.enabled = true;
        targetImage.color = equippedColor;

        // ✅ 4) 记录装备 + 写入 expressionState
        _equipped = part;
        expressionState?.Equip(part);

        // ✅ 5) 刷新背包 UI
        FindFirstObjectByType<InventoryUI>()?.Refresh();
    }

    // -----------------------
    // Helper: Visual states
    // -----------------------
    void SetEmptyVisual()
    {
        if (!targetImage) return;

        targetImage.sprite = null;
        targetImage.enabled = true; // 保留用于接收 drop
        var c = targetImage.color;
        c.a = emptyAlpha;
        targetImage.color = c;
    }

    /// <summary>
    /// 可选：外部（加载存档/重进场景）手动设置当前装备并刷新UI
    /// </summary>
    public void SetEquipped(PartDefinition part)
    {
        _equipped = part;

        if (part == null)
        {
            expressionState?.Unequip(acceptType);
            SetEmptyVisual();
            return;
        }

        if (targetImage)
        {
            var spriteToEquip = part.equipSprite ? part.equipSprite : part.originalSprite;
            targetImage.sprite = spriteToEquip;
            targetImage.enabled = true;
            targetImage.color = equippedColor;
        }

        expressionState?.Equip(part);
    }
}
