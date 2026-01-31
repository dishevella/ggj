using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventorySlotUI : MonoBehaviour,IPointerEnterHandler,IPointerExitHandler
{
    public Image icon;
    PartDefinition _part;
    ItemTooltipFollow _tooltip;
    public void Bind(PartDefinition part, ItemTooltipFollow tooltip)
    {
        _part = part;
        _tooltip = tooltip;

        if (_part == null)
        {
            icon.enabled = false;
            icon.sprite = null;
        }
        else
        {
            icon.enabled = true;
            icon.sprite = _part.originalSprite;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_part != null && _tooltip != null)
            _tooltip.Show(_part.id, _part.hintSprite);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _tooltip?.Hide();
    }

// Start is called once before the first execution of Update after the MonoBehaviour is created
void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
