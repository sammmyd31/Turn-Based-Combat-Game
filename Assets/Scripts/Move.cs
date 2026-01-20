using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public enum TargetType { None, SingleEnemy, AllEnemies, Self, SingleAlly, AllAllies, DeadAlly }

[System.Serializable]
public class Move
{
    public string name;
    public int energy;
    public int cooldown;
    public TargetType selectedTarget;

    [SerializeReference]
    public List<MoveAction> actions;

    string description;
    int attackPower;

    public int AttackPower => attackPower;

    public string Description => description;

    public void GetAttributes()
    {
        if (selectedTarget == TargetType.None)
        {
            Debug.LogWarning("Move does not have a selected target type.");
            return;
        }

        StringBuilder desc = new StringBuilder();

        attackPower = actions.OfType<DamageAction>().FirstOrDefault()?.attackPower ?? 0;

        StatusEffectAction.GenerateDescription(actions);

        foreach (MoveAction action in actions) desc.Append(action.GetDescription());

        description = desc.ToString();
    }

    public static int CalculateDamage(int ap, int strength, float multiplier)
    {
        return -Mathf.RoundToInt((strength / 100f) * ap * multiplier);
    }
}