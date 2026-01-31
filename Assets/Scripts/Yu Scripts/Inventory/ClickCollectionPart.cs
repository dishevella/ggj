using UnityEngine;
using UnityEngine.EventSystems;

public class ClickCollection : MonoBehaviour,IPointerClickHandler
{
    public PartDefinition part;
    public bool disableOnCollect = true;
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        if (part == null) return;
        bool ok = InventoryManager.I.Add(part);
        if(ok)
        {
            if (disableOnCollect) gameObject.SetActive(false);
        }

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
