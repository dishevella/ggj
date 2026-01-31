using UnityEngine;
using System.Collections.Generic;
using System;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager I { get; private set; }
    public int capacity = 12;
    public List<PartDefinition> parts = new();
    public event Action OnChanged;
    private void Awake()
    {
        if(I != null) { Destroy(gameObject); return; }
        I = this;
    }
    public bool Add(PartDefinition part)
    {
        if (part == null) return false;
        if (parts.Count >= capacity) return false;
        if (parts.Exists(p => p != null && p.id == part.id)) return false;
        parts.Add(part);
        OnChanged?.Invoke();
        return true;
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
