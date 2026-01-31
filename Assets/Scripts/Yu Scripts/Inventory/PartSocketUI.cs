using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


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

   
    PartDefinition _equipped;

    void Awake()
    {
        SetEmptyVisual();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        Debug.Log("[Socket] PointerEnter: " + gameObject.name);
    }


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

        
        if (_equipped == part)
        {
            Debug.Log("[Socket] same part already equipped -> ignore");
            return;
        }

       
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

        
        bool removed = InventoryManager.I != null && InventoryManager.I.Remove(part.id);
        Debug.Log("[Socket] removedFromBag=" + removed);

        if (!removed)
        {
            Debug.LogWarning("[Socket] Part not found in bag -> reject.");
            return;
        }

        
        var spriteToEquip = part.equipSprite ? part.equipSprite : part.originalSprite;
        Debug.Log("[Socket] spriteToEquip=" + (spriteToEquip ? spriteToEquip.name : "NULL"));

        targetImage.sprite = spriteToEquip;
        targetImage.enabled = true;
        targetImage.color = equippedColor;

       
        _equipped = part;
        expressionState?.Equip(part);

       
        FindFirstObjectByType<InventoryUI>()?.Refresh();
    }

 
    void SetEmptyVisual()
    {
        if (!targetImage) return;

        targetImage.sprite = null;
        targetImage.enabled = true; 
        var c = targetImage.color;
        c.a = emptyAlpha;
        targetImage.color = c;
    }

 
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
