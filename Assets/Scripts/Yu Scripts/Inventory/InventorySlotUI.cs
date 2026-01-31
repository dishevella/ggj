using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventorySlotUI : MonoBehaviour,IPointerEnterHandler, IPointerExitHandler,IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public Image icon;
    public Canvas canvas;
    PartDefinition _part;
    ItemTooltipFollow _tooltip;
    private RectTransform _dragRt;
    private Image _dragImg;
    public PartDefinition Part => _part;
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
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (_part == null || canvas == null) return;

        _tooltip?.Hide(); 

        
        var go = new GameObject("DragIcon");
        go.transform.SetParent(canvas.transform, false); // set the drag icon as the child of the cursor

        _dragRt = go.AddComponent<RectTransform>();
        _dragImg = go.AddComponent<Image>();

        _dragImg.sprite = icon.sprite;
        _dragImg.raycastTarget = false; 

        _dragRt.sizeDelta = new Vector2(90, 90); // you can adjust the size here
        _dragRt.position = eventData.position;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_dragRt != null)
            _dragRt.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (_dragRt != null)
            Destroy(_dragRt.gameObject);

        _dragRt = null;
        _dragImg = null;
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
