using System.Collections.Generic;
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

    [Header("Filter")]
    public PartType currentFilter = PartType.Eyes;

    bool _inputLocked;

    void Awake()
    {
        if (!root) root = GetComponent<CanvasGroup>();
        if (InventoryManager.I != null)
            InventoryManager.I.OnChanged += Refresh;
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
        if (_inputLocked) return;
        if (Input.GetKeyDown(toggleKey))
        {
            Debug.Log("[Inventory] toggleKey DOWN -> " + toggleKey + "\n" + UnityEngine.StackTraceUtility.ExtractStackTrace());
            if (IsOpen) Close();
            else Open();
        }
    }

    public void Open()
    {
        Debug.Log("[Inventory] Open() CALLED\n" + UnityEngine.StackTraceUtility.ExtractStackTrace());
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

    public void SetInputLocked(bool locked)
    {
        _inputLocked = locked;
    }

    public void SetFilter(PartType type)
    {
        currentFilter = type;
        Debug.Log($"[InventoryUI] SetFilter -> {currentFilter}");
        Refresh();
    }
    public void Refresh()
    {
        // 清空 slots
        for (int i = 0; i < slots.Length; i++)
            slots[i].Bind(null, tooltip);

        tooltip?.Hide();

        if (InventoryManager.I == null) return;

        var all = InventoryManager.I.parts;
        int slotIndex = 0;

        for (int i = 0; i < all.Count; i++)
        {
            var p = all[i];
            if (p == null) continue;
            if (p.type != currentFilter) continue;

            if (slotIndex >= slots.Length) break;
            slots[slotIndex].Bind(p, tooltip);
            slotIndex++;
        }

        Debug.Log($"[InventoryUI] Filter={currentFilter}, shown={slotIndex}, total={all.Count}");
    }

}
