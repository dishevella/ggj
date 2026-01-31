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
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
