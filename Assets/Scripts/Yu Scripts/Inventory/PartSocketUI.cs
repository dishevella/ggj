using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PartSocketUI : MonoBehaviour, IDropHandler, IPointerEnterHandler
{
    public Image targetImage;          // 拖自己这个Image进来
    public PartType acceptType;        // Eyes/Nose/Mouth
    void Awake()
    {
        if (targetImage)
        {
            var c = targetImage.color;
            c.a = 0f;
            targetImage.color = c;
            targetImage.sprite = null;
            targetImage.enabled = true; // 保留用于接收drop
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        Debug.Log("[Socket] PointerEnter: " + gameObject.name);
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

        var spriteToEquip = part.equipSprite ? part.equipSprite : part.originalSprite;
        Debug.Log("[Socket] spriteToEquip=" + (spriteToEquip ? spriteToEquip.name : "NULL"));

        targetImage.sprite = spriteToEquip;
        targetImage.enabled = (spriteToEquip != null);
        targetImage.color = Color.white;
        bool removed = InventoryManager.I.Remove(part.id);
        Debug.Log("[Socket] removedFromBag=" + removed);
    }

}
