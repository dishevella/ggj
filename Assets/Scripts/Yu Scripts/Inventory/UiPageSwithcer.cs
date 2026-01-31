using UnityEngine;
using UnityEngine.UI;

public class PartTabButtons : MonoBehaviour
{
    public InventoryUI inventoryUI;

    [Header("Buttons")]
    public Button btnEyes;
    public Button btnNose;
    public Button btnMouth;
    public Button btnOther;

    void Awake()
    {
        if (btnEyes) btnEyes.onClick.AddListener(() => inventoryUI.SetFilter(PartType.Eyes));
        if (btnNose) btnNose.onClick.AddListener(() => inventoryUI.SetFilter(PartType.Nose));
        if (btnMouth) btnMouth.onClick.AddListener(() => inventoryUI.SetFilter(PartType.Mouth));
        if (btnOther) btnOther.onClick.AddListener(() => inventoryUI.SetFilter(PartType.Other));
    }
}
