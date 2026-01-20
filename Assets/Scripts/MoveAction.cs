using System.Collections.Generic;
using System.Linq;
using System.Text;

public enum DamageType { None, Pierce, Laser, Explosive, Blunt, Electric, Fire }
public enum HealthType { Standard, Health, Armor }

[System.Serializable]
public abstract class MoveAction
{
    public TargetType targetType;

    public abstract void CarryOutAction(Bot target, Bot current);
    public abstract string GetDescription();

    public static string TargetToString(TargetType target)
    {
        return target switch
        {
            TargetType.SingleEnemy => "enemy",
            TargetType.AllEnemies => "all enemies",
            TargetType.Self => "self",
            TargetType.SingleAlly => "ally",
            TargetType.AllAllies => "all allies",
            TargetType.DeadAlly => "dead ally",
            _ => "unknown target"
        };
    }
}

public class DamageAction : MoveAction
{
    public int attackPower;
    public DamageType damageType;

    public override void CarryOutAction(Bot target, Bot current)
    {
        int damage = Move.CalculateDamage(attackPower, current.Strength, current.DealDamageMultiplier);

        HealthChange change = target.MakeHealthChange(damage, HealthType.Standard, damageType);

        target.ChangeHealth(change);
    }

    public override string GetDescription()
    {
        string damageDescriptor = attackPower switch
        {
            > 50 => "very heavy",
            > 40 => "heavy",
            > 20 => "moderate",
            _ => "light"
        };

        string target = TargetToString(targetType);

        return $"Deals {damageDescriptor} {damageType.ToString().ToLower()} damage to {target}. ";
    }
}

public class StatusEffectAction : MoveAction
{
    public StatusEffectBase statusEffectBase;

    static string description;

    public override void CarryOutAction(Bot target, Bot current)
    {
        if (target.AddStatusEffect(new StatusEffect(statusEffectBase, target == current)))
        {
            target.BotImage.ShowVisual(statusEffectBase.name, statusEffectBase.type);
        }
        else
        {
            target.BotImage.ShowVisual($"{statusEffectBase.name} blocked");
        }
    }

    public static void GenerateDescription(List<MoveAction> actions)
    {
        var sb = new StringBuilder();

        var groupedStatusEffects = actions
            .OfType<StatusEffectAction>()
            .GroupBy(effect => effect.targetType);

        foreach (var group in groupedStatusEffects)
        {
            var names = group.Select(e => e.statusEffectBase.name).ToList();
            string effectList = FormatEffectList(names).ToLower();
            string target = TargetToString(group.Key);

            sb.Append($"Applies {effectList} to {target}. ");
        }

        description = sb.ToString();
    }

    static string FormatEffectList(List<string> names)
    {
        if (names.Count == 1)
            return names[0];
        if (names.Count == 2)
            return $"{names[0]} and {names[1]}";

        return string.Join(", ", names.Take(names.Count - 1)) + $", and {names.Last()}";
    }

    public override string GetDescription()
    {
        string temp = description;
        description = "";
        return temp;
    }
}

public class ExtraTurnAction : MoveAction
{
    public override void CarryOutAction(Bot target, Bot current)
    {
        target.BotImage.ShowVisual("Extra Turn", StatusEffectType.Positive);
        BattleManager.turnOrder.Insert(0, target);
    }

    public override string GetDescription()
    {
        return $"Gives extra turn to {TargetToString(targetType)}. ";
    }
}

public class RemoveNegEffectsAction : MoveAction
{
    public override void CarryOutAction(Bot target, Bot current)
    {
        target.BotImage.ShowVisual("Removed Negative Effects", StatusEffectType.Positive);
        target.RemoveEffects(StatusEffectType.Negative);
    }

    public override string GetDescription()
    {
        return $"Removes negative status effects from {TargetToString(targetType)}. ";
    }
}

public class RemovePosEffectsAction : MoveAction
{
    public override void CarryOutAction(Bot target, Bot current)
    {
        target.BotImage.ShowVisual("Removed Positive Effects", StatusEffectType.Negative);
        target.RemoveEffects(StatusEffectType.Positive);
    }

    public override string GetDescription()
    {
        return $"Removes positive status effects from {TargetToString(targetType)}. ";
    }
}

public class HealAction : MoveAction
{
    public HealthType healType;
    public float healPercentage;

    public override void CarryOutAction(Bot target, Bot current)
    {
        var amount = target.PercentOf(healPercentage, healType);

        HealthChange change = target.MakeHealthChange(amount, healType, DamageType.None);

        target.ChangeHealth(change);
    }

    public override string GetDescription()
    {
        string type = healType switch
        {
            HealthType.Armor => "Restores armor of",
            HealthType.Health => "Restores health of",
            HealthType.Standard => "Restores combined health of",
            _ => "???"
        };

        return $"{type} {TargetToString(targetType)} by {healPercentage * 100}%. ";
    }
}

public class ReviveAction : MoveAction
{
    public float healthPercentage;
    public float armorPercentage;

    public override void CarryOutAction(Bot target, Bot current)
    {
        target.ReviveBot(healthPercentage, armorPercentage);
    }

    public override string GetDescription()
    {
        string end = armorPercentage > 0 ? $" and {armorPercentage * 100}% armor" : "";

        return $"Revives {TargetToString(targetType)} with {healthPercentage * 100}% health{end}.";
    }
}