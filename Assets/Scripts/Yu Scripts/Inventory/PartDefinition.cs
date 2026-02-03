using UnityEngine;

public enum PartType { Eyes, Nose, Mouth, Other}

[CreateAssetMenu(menuName = "Part Definition")]
public class PartDefinition : ScriptableObject
{
    public string id;
    public PartType type;
    public Sprite originalSprite;
    public Sprite hintSprite;
    public Sprite equipSprite;
    public EmotionVector effect;
    [System.Serializable]
    public struct UIPlacement
    {
        public bool overridePlacement;      // 勾上才启用自定义
        public Vector2 anchoredPos;         // UI位置偏移（RectTransform.anchoredPosition）
        public float rotationZ;             // Z轴旋转
        public Vector2 scale;               // UI缩放（x,y）
    }

    public UIPlacement placement = new UIPlacement
    {
        overridePlacement = false,
        anchoredPos = Vector2.zero,
        rotationZ = 0f,
        scale = new Vector2(1f, 1f),
    };
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
