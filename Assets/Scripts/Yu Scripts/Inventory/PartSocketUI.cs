using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class PartSocketUI : MonoBehaviour,
    IDropHandler, IPointerEnterHandler, IPointerClickHandler
{
    [Header("Socket")]
    public Image targetImage;
    public PartType acceptType;                 // Eyes/Nose/Mouth/Other
    public PlayerExpressionState expressionState;

    [Header("Visual")]
    [Range(0f, 1f)] public float emptyAlpha = 0f;
    public Color equippedColor = Color.white;

    PartDefinition _equipped;

    RectTransform _rt;
    Vector2 _basePos;
    Vector3 _baseScale;
    float _baseRotZ;

    void Awake()
    {
        if (targetImage)
        {
            targetImage.raycastTarget = true; // 确保右键能点到
            _rt = targetImage.rectTransform;
        }

        if (_rt)
        {
            _basePos = _rt.anchoredPosition;
            _baseScale = _rt.localScale;
            _baseRotZ = _rt.localEulerAngles.z;
        }

        SetEmptyVisual();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // 需要调试可以打开
        // Debug.Log("[Socket] PointerEnter: " + gameObject.name);
    }

    // 右键卸下
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Right) return;

        if (_equipped == null)
        {
            Debug.Log($"[Socket:{acceptType}] RightClick unequip: nothing equipped");
            return;
        }

        // 退回背包（满/重复会失败）
        bool ok = InventoryManager.I != null && InventoryManager.I.Add(_equipped);
        Debug.Log($"[Socket:{acceptType}] unequip -> back to bag={ok}");

        if (!ok)
        {
            Debug.LogWarning($"[Socket:{acceptType}] Cannot unequip (bag full or duplicate).");
            return;
        }

        expressionState?.Unequip(acceptType);
        _equipped = null;

        SetEmptyVisual();
        FindFirstObjectByType<InventoryUI>()?.Refresh();
    }

    // 拖拽装备
    public void OnDrop(PointerEventData eventData)
    {
        var pd = eventData.pointerDrag;
        var slot = pd ? pd.GetComponentInParent<InventorySlotUI>() : null;
        var part = slot ? slot.Part : null;

        if (part == null) return;

        if (part.type != acceptType)
        {
            Debug.Log($"[Socket:{acceptType}] type mismatch -> reject");
            return;
        }

        if (targetImage == null || _rt == null)
        {
            Debug.LogError($"[Socket:{acceptType}] targetImage/RectTransform is NULL");
            return;
        }

        if (_equipped == part)
        {
            Debug.Log($"[Socket:{acceptType}] same part already equipped -> ignore");
            return;
        }

        // 槽位已有旧装备：先退回背包（失败则拒绝替换，防丢）
        if (_equipped != null)
        {
            bool backOk = InventoryManager.I != null && InventoryManager.I.Add(_equipped);
            Debug.Log($"[Socket:{acceptType}] return old to bag={backOk}");

            if (!backOk)
            {
                Debug.LogWarning($"[Socket:{acceptType}] Bag full or duplicate -> cannot replace.");
                return;
            }
        }

        // 从背包移除新装备
        bool removed = InventoryManager.I != null && InventoryManager.I.Remove(part.id);
        Debug.Log($"[Socket:{acceptType}] removedFromBag={removed}");

        if (!removed)
        {
            Debug.LogWarning($"[Socket:{acceptType}] Part not found in bag -> reject.");
            return;
        }

        // 显示装备图
        var spriteToEquip = part.equipSprite ? part.equipSprite : part.originalSprite;
        targetImage.sprite = spriteToEquip;
        targetImage.enabled = true;
        targetImage.color = equippedColor;

        ApplyPlacement(part);

        _equipped = part;
        expressionState?.Equip(part);

        FindFirstObjectByType<InventoryUI>()?.Refresh();
    }

    // ====== 位置摆放：相对偏移（默认 + offset） ======
    void ApplyPlacement(PartDefinition part)
    {
        if (!_rt || part == null) return;

        var p = part.placement;

        if (!p.overridePlacement)
        {
            ResetPlacement();
            return;
        }

        _rt.anchoredPosition = _basePos + p.anchoredPos;
        _rt.localEulerAngles = new Vector3(0f, 0f, _baseRotZ + p.rotationZ);
        _rt.localScale = new Vector3(_baseScale.x * p.scale.x, _baseScale.y * p.scale.y, 1f);
    }

    void ResetPlacement()
    {
        if (!_rt) return;
        _rt.anchoredPosition = _basePos;
        _rt.localEulerAngles = new Vector3(0f, 0f, _baseRotZ);
        _rt.localScale = _baseScale;
    }

    // 空槽位：透明 + 清 sprite + 重置摆放
    void SetEmptyVisual()
    {
        ResetPlacement();

        if (!targetImage) return;

        targetImage.sprite = null;
        targetImage.enabled = true; // 保留用于接收 drop

        var c = targetImage.color;
        c.a = emptyAlpha;
        targetImage.color = c;
    }

    // 外部强制设置装备（加载/重进场景用）
    public void SetEquipped(PartDefinition part)
    {
        _equipped = part;

        if (part == null)
        {
            expressionState?.Unequip(acceptType);
            SetEmptyVisual();
            return;
        }

        var spriteToEquip = part.equipSprite ? part.equipSprite : part.originalSprite;
        targetImage.sprite = spriteToEquip;
        targetImage.enabled = true;
        targetImage.color = equippedColor;

        ApplyPlacement(part);
        expressionState?.Equip(part);
    }

#if UNITY_EDITOR
    // ✅ 在 Inspector 右上角齿轮菜单/右键组件里可点：保存当前摆放到 PartDefinition
    [ContextMenu("Save Placement To PartDefinition (equipped)")]
    void SavePlacementToEquippedPart()
    {
        if (_rt == null)
        {
            Debug.LogError("[Socket] RectTransform is null. Make sure targetImage exists.");
            return;
        }

        if (_equipped == null)
        {
            Debug.LogError("[Socket] No equipped part. Equip a part first, then adjust position.");
            return;
        }

        // 计算“相对偏移”
        Vector2 offsetPos = _rt.anchoredPosition - _basePos;

        float rotNow = _rt.localEulerAngles.z;
        float rotOffset = Mathf.DeltaAngle(_baseRotZ, rotNow);

        Vector3 scNow = _rt.localScale;
        Vector2 scaleMul = new Vector2(
            _baseScale.x == 0 ? 1f : scNow.x / _baseScale.x,
            _baseScale.y == 0 ? 1f : scNow.y / _baseScale.y
        );

        var p = _equipped.placement;
        p.overridePlacement = true;
        p.anchoredPos = offsetPos;
        p.rotationZ = rotOffset;
        p.scale = scaleMul;
        _equipped.placement = p;

        EditorUtility.SetDirty(_equipped);
        AssetDatabase.SaveAssets();

        Debug.Log($"[Socket] Saved placement to '{_equipped.name}': " +
                  $"offset={offsetPos}, rotOffset={rotOffset}, scaleMul={scaleMul}");
    }
#endif
}
