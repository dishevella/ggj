using UnityEngine;

public class InventoryUI : MonoBehaviour
{
    [Header("UI")]
    public CanvasGroup root;             
    public InventorySlotUI[] slots;      
    public ItemTooltipFollow tooltip;

    [Header("Control")]
    public KeyCode toggleKey = KeyCode.I;

    public bool IsOpen { get; private set; }
    public static event System.Action<bool> OnInventoryOpenChanged;

    void Awake()
    {
        if (!root) root = GetComponent<CanvasGroup>();
        Close();        
        Refresh();      
    }

    void OnEnable()
    {
        if (InventoryManager.I != null)
            InventoryManager.I.OnChanged += Refresh;
    }

    void OnDisable()
    {
        if (InventoryManager.I != null)
            InventoryManager.I.OnChanged -= Refresh;
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            if (IsOpen) Close();
            else Open();
        }
    }

    public void Open()
    {
        IsOpen = true;
        root.alpha = 1;
        root.blocksRaycasts = true;   
        root.interactable = true;
        Refresh();
        OnInventoryOpenChanged?.Invoke(true);
    }

    public void Close()
    {
        IsOpen = false;
        root.alpha = 0;
        root.blocksRaycasts = false;  
        root.interactable = false;
        tooltip?.Hide();
        OnInventoryOpenChanged?.Invoke(false);
    }

    public void Refresh()
    {
        
        for (int i = 0; i < slots.Length; i++)
            slots[i].Bind(null, tooltip);

        tooltip?.Hide();

        
        if (InventoryManager.I == null) return;

        var list = InventoryManager.I.parts;
        int n = Mathf.Min(list.Count, slots.Length);
        for (int i = 0; i < n; i++)
            slots[i].Bind(list[i], tooltip);
    }
}
