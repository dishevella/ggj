using UnityEngine;

public class PlayerExpressionState : MonoBehaviour
{
    public PartDefinition equippedEyes;
    public PartDefinition equippedNose;
    public PartDefinition equippedMouth;
    public PartDefinition equippedOther;

    public EmotionVector GetTotal()
    {
        EmotionVector total = EmotionVector.Zero;
        if (equippedEyes) total += equippedEyes.effect;
        if (equippedNose) total += equippedNose.effect;
        if (equippedMouth) total += equippedMouth.effect;
        if (equippedOther) total += equippedOther.effect;
        return total;
    }

    public void Equip(PartDefinition part)
    {
        if (part == null) return;

        switch (part.type)
        {
            case PartType.Eyes: equippedEyes = part; break;
            case PartType.Nose: equippedNose = part; break;
            case PartType.Mouth: equippedMouth = part; break;
            case PartType.Other: equippedOther = part; break;
        }
    }
    public void Unequip(PartType type)
    {
        switch (type)
        {
            case PartType.Eyes:
                equippedEyes = null;
                break;

            case PartType.Nose:
                equippedNose = null;
                break;

            case PartType.Mouth:
                equippedMouth = null;
                break;

            case PartType.Other:
                equippedOther = null;
                break;
        }
    }


}
