using UnityEngine;
using UnityEngine.EventSystems;

public class ClickCollection : MonoBehaviour,IPointerClickHandler
{
    public PartDefinition part;
    public bool disableOnCollect = true;
    PickupAnim anim;
    bool _inventoryOpen;
    public DialogueGroupSO group;  //★★★
    public DialogueManager DM;  //★★★

    void Awake()
    {
        anim = GetComponent<PickupAnim>();
    }
    void OnEnable()
    {
        InventoryUI.OnInventoryOpenChanged += HandleInv;
    }

    void OnDisable()
    {
        InventoryUI.OnInventoryOpenChanged -= HandleInv;
    }

    void HandleInv(bool open)
    {
        _inventoryOpen = open;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (_inventoryOpen) return;
        if (eventData.button != PointerEventData.InputButton.Left) return;
        if (part == null) return;

        
        if (group!=null) //★★★
        {
            DM.PlayGroup(group);
        }
    
        bool ok = InventoryManager.I.Add(part);
        if(ok)
        {
            if(anim!=null)
            {
                anim.PlayAndDisable();
            }
            else
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
