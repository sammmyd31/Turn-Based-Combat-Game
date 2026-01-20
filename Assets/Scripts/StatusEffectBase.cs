using UnityEngine;

public enum StatusEffectType { Positive, Negative }

[CreateAssetMenu(fileName = "Status Effect", menuName = "Status Effect")]
public class StatusEffectBase : ScriptableObject
{
    [Header("Base Settings")]
    public string effectName;
    public int turns;
    public StatusEffectType type;
    public bool isControl;
    public bool isTorture;
    
    [Header("Effect Settings")]
    public float armorPercentChange = 0;
    public float standardPercentChange = 0;
    public float healthPercentChange = 0;
    public float energyPercentChange = 0;
    public float accuracyPercentChange = 0;
    public float damagePercentChange = 0;
    public float coolChange = 0;
    public float blockDamage = 0;
    public float healMultiplier = 0;

    public bool blockAllEffects = false;
    public bool blockNegativeEffects = false;
    public bool blockPositiveEffects = false;
    public bool blockControlEffects = false;
    public bool blockTortureEffects = false;
    public bool skipTurn = false;
    public bool directAttacks = false;
    public bool randomMove = false;
}