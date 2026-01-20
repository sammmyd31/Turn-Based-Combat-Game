using UnityEngine;

[System.Serializable]
public class StatusEffect
{
    public StatusEffectBase statusEffectBase;

    int turnsLeft;

    public StatusEffectType EffectType => statusEffectBase.type;

    public StatusEffect(StatusEffectBase b, bool isSelf)
    {
        statusEffectBase = b;
        turnsLeft = statusEffectBase.turns;

        // add an extra turn if it is applied to itself
        // without this, the effect would have one less turn due to decrementing happening after the turn is over
        // so, if applied to itself, it decrements the same turn that it is applied
        if (isSelf) turnsLeft++;
    }

    public bool DecrementTurnsLeft()
    {
        turnsLeft--;
        return turnsLeft == 0;
    }

    public void ResetEffect()
    {
        turnsLeft = statusEffectBase.turns;
    }

    public bool InGroup(GroupImmunity group)
    {
        switch (group)
        {
            case GroupImmunity.All:
                return true;
            case GroupImmunity.Positive:
                return statusEffectBase.type == StatusEffectType.Positive;
            case GroupImmunity.Negative:
                return statusEffectBase.type == StatusEffectType.Negative;
            case GroupImmunity.Control:
                return statusEffectBase.isControl;
            case GroupImmunity.Torture:
                return statusEffectBase.isTorture;
            default:
                Debug.LogWarning("Unhandled group immunity type: " + group);
                return false;
        }
    }
}