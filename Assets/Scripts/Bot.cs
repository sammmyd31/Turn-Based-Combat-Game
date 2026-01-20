using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum GroupImmunity {All, Positive, Negative, Control, Torture}

[System.Serializable]
public class Bot
{
    public static List<Bot> deathsThisTurn = new List<Bot>();

    public BotBase botBase;
    public int level;

    int currentHealth;
    int currentArmor;
    int currentEnergy;
    int speedCounter;
    bool isDead;
    int power;
    bool isPlayer;
    float accuracy = 1;
    BotImage botImage;
    List<StatusEffect> statusEffects = new List<StatusEffect>();
    int[] cooldowns = new int[4];
    float coolMultiplier = 1;
    float takeDamageMultiplier = 1;
    float dealDamageMultiplier = 1;
    float healMultiplier = 1;
    Dictionary<GroupImmunity, bool> groupImmunities = new Dictionary<GroupImmunity, bool>();
    bool skipNextTurn = false;
    bool isDirectingAttacks = false;
    bool randomMove = false;

    public Bot(BotBase b, int l, bool p, BotImage bi)
    {
        botBase = b;
        level = l;
        isPlayer = p;
        botImage = bi;

        currentHealth = Health;
        currentArmor = ArmorHealth;
        currentEnergy = 0;

        speedCounter = 0;

        power = Health + ArmorHealth + Strength + Speed + EnergyCapacity;

        botImage.AssignBot(this);

        foreach (GroupImmunity group in System.Enum.GetValues(typeof(GroupImmunity)))
        {
            groupImmunities[group] = false;
        }
    }

    public string Name => botBase.name;

    public bool IsPlayer => isPlayer;

    public int Health => botBase.health * level;

    public int ArmorHealth => botBase.armorHealth * level;

    public int Strength => botBase.strength * level;

    public int Speed => botBase.speed * level;

    public int EnergyCapacity => botBase.energyCapactiy * level;

    public int SpeedCounter => speedCounter;

    public bool IsDead => isDead;

    public int Power => power;

    public int CurrentHealth => currentHealth;

    public int CurrentArmor => currentArmor;

    public int CurrentEnergy => currentEnergy;

    public float Accuracy => accuracy;

    public BotImage BotImage => botImage;

    public bool SkipTurn => skipNextTurn;

    public bool IsDirectingAttacks => isDirectingAttacks;

    public float DealDamageMultiplier => dealDamageMultiplier;

    public bool RandomAttack => randomMove;

    public int IncrementSpeedCounter()
    {
        speedCounter += Speed;
        return speedCounter;
    }

    public void DecrementSpeedCounter()
    {
        speedCounter -= 1000;
    }

    // find first tie and add one to speed counter of tiebreaker
    public static bool BreakTies(List<Bot> bots)
    {
        var sortedBots = new List<Bot>(bots);
        sortedBots.Sort((b1, b2) => b1.Speed.CompareTo(b2.Speed));

        for (int i = 0; i < sortedBots.Count - 1; i++)
        {
            var bot1 = sortedBots[i];
            var bot2 = sortedBots[i + 1];

            if (bot1.Speed == bot2.Speed && bot1.speedCounter == bot2.speedCounter)
            {
                // highest power wins tie
                if (bot1.power > bot2.power)
                    bot1.speedCounter++;

                else if (bot1.power < bot2.power)
                    bot2.speedCounter++;

                // random if same power
                else
                {
                    if (Random.Range(1, 3) == 1)
                        bot1.speedCounter++;
                    else
                        bot2.speedCounter++;
                }
                return false;
            }
        }
        return true;
    }

    public void ChangeEnergy(int amount)
    {
        currentEnergy = Mathf.Max(currentEnergy += amount, 0);

        botImage.ShowVisual("Value Text", amount, ValueType.Energy);

        botImage.SetEnergyBarStarter();
    }

    public void ChangeHealth(HealthChange change)
    {
        int displayedValue = 0;

        if (change.ArmorAmount != 0) displayedValue += ApplyChange(ref currentArmor, ArmorHealth, change.ArmorAmount);

        if (change.HealthAmount != 0) displayedValue += ApplyChange(ref currentHealth, Health, change.HealthAmount);

        // check if dead (no health)
        if (currentHealth == 0 && !isDead)
        {
            isDead = true;
            deathsThisTurn.Add(this);
        }

        // UI updates
        botImage.ShowVisual("Value Text", displayedValue, ValueType.Health);
        botImage.SetHealthBarsStarter(currentHealth, currentArmor);
    }

    int ApplyChange(ref int current, int max, int amount)
    {
        // healing
        if (amount > 0)
        {
            int healed = Mathf.Min(max - current, amount);
            current += healed;
            return healed;
        }

        // damage
        else
        {
            int dmg = Mathf.Min(current, -amount);
            current -= dmg;
            return -dmg;
        }
    }

    // determine total change based on the type of health
    public HealthChange MakeHealthChange(int amount, HealthType type, DamageType damageType)
    {
        if (amount > 0)  amount = Mathf.CeilToInt(amount * healMultiplier);
        else if (amount < 0)
        {
            amount = Mathf.CeilToInt(amount * takeDamageMultiplier);

            StatusEffect kineticArmor = statusEffects.Find(e => e.statusEffectBase.name == "Kinetic Armor");
            if (kineticArmor != null)
            {
                amount = 0;
                statusEffects.Remove(kineticArmor);
            }
        }

        float armorBonus = 1f;
        if (damageType == DamageType.Blunt) armorBonus += .25f;

        int healthChange = 0;
        int armorChange = 0;

        if (type == HealthType.Health)
        {
            healthChange = amount;
        }
        else if (type == HealthType.Armor)
        {
            armorChange = Mathf.CeilToInt(amount * armorBonus);
        }
        else if (type == HealthType.Standard)
        {
            if (amount < 0)
            {
                int armorBreakPoint = Mathf.CeilToInt(currentArmor / armorBonus);
                if (Mathf.Abs(amount) <= armorBreakPoint)
                {
                    armorChange = Mathf.CeilToInt(amount * armorBonus);
                }
                else
                {
                    armorChange = amount;
                    healthChange = armorBreakPoint + amount;
                }
            }
            else
            {
                healthChange = amount;
                armorChange = Mathf.Max(amount - (Health - currentHealth), 0);
            }
        }
        return new HealthChange(armorChange, healthChange);
    }

    bool DisplayAccuracy()
    {
        if (accuracy != 1f)
        {
            var value = "";
            if (accuracy > 1) value = "+";
            value += ((accuracy - 1f) * 100) + "%";

            var message = $"{value} Accuracy";
            
            botImage.ShowVisual(message);
            return true;
        }
        else return false;
    }

    bool DisplayDamage()
    {
        if (dealDamageMultiplier != 1f)
        {
            var value = "";
            if (dealDamageMultiplier > 1) value = "+";
            value += ((dealDamageMultiplier - 1f) * 100) + "%";

            var message = $"{value} Damage";
            
            botImage.ShowVisual(message);
            return true;
        }
        else return false;
    }

    // applies the effects of each status effect at the start of the turn
    public IEnumerator EffectsTurnStart(float standardDuration, float damageDuration)
    {
        float totalArmorChange = 0f;
        float totalStandardChange = 0f;
        float totalHealthChange = 0f;
        float totalEnergyChange = 0f;

        bool skipTurn = false;
        bool doRandomMove = false;

        foreach (StatusEffect effect in statusEffects)
        {
            totalArmorChange += effect.statusEffectBase.armorPercentChange;
            totalStandardChange += effect.statusEffectBase.standardPercentChange;
            totalHealthChange += effect.statusEffectBase.healthPercentChange;
            totalEnergyChange += effect.statusEffectBase.energyPercentChange;
            skipTurn |= effect.statusEffectBase.skipTurn;
            doRandomMove |= effect.statusEffectBase.randomMove;
        }

        if (totalArmorChange != 0 || totalStandardChange != 0 || totalHealthChange != 0)
        {
            var armorAmount = (int)(ArmorHealth * totalArmorChange);
            var standardAmount = (int)((Health + ArmorHealth) * totalStandardChange);
            var healthAmount = (int)(Health * totalHealthChange);

            HealthChange change = new HealthChange(armorAmount, healthAmount);
            change.Add(MakeHealthChange(standardAmount, HealthType.Standard, DamageType.None));

            ChangeHealth(change);

            yield return new WaitForSeconds(damageDuration);

            // check and stop if bot died
            if (Bot.deathsThisTurn.Count > 0) yield break;
        }

        if (totalEnergyChange != 0)
        {
            var amount = (int)(EnergyCapacity * totalEnergyChange);

            ChangeEnergy(amount);

            yield return new WaitForSeconds(standardDuration);
        }

        skipNextTurn = skipTurn;
        if (skipNextTurn)
        {
            botImage.ShowVisual("Turn Lost");
            yield return new WaitForSeconds(standardDuration);
        }

        randomMove = doRandomMove;
        if (randomMove)
        {
            botImage.ShowVisual("Corrupted");
            yield return new WaitForSeconds(standardDuration);
        }

        if (DisplayAccuracy()) yield return new WaitForSeconds(standardDuration);

        if (DisplayDamage()) yield return new WaitForSeconds(standardDuration);
    }

    public void StartCooldown(int moveNum)
    {
        cooldowns[moveNum] = botBase.moves[moveNum].cooldown + 1;
    }

    public void DecrementCooldown(int moveNum)
    {
        if (cooldowns[moveNum] > 0) cooldowns[moveNum]--;
    }

    public bool InCooldown(int moveNum)
    {
        return cooldowns[moveNum] > 0;
    }

    public int GetCooldown(int moveNum)
    {
        return cooldowns[moveNum];
    }

    public bool AddStatusEffect(StatusEffect newEffect)
    {
        foreach (var (group, hasImmunity) in groupImmunities)
        {
            if (hasImmunity && newEffect.InGroup(group)) return false;
        }

        // reset turns left if reapplying an effect that is already on the Bot
        int index = statusEffects.FindIndex(effect => effect.statusEffectBase == newEffect.statusEffectBase);
        if (index != -1) statusEffects[index].ResetEffect();
        else statusEffects.Add(newEffect);

        if (newEffect.statusEffectBase.blockControlEffects)
            statusEffects.RemoveAll(effect => effect.statusEffectBase.isControl);
        
        if (newEffect.statusEffectBase.blockTortureEffects)
            statusEffects.RemoveAll(effect => effect.statusEffectBase.isTorture);

        CalculateEffectChanges();

        return true;
    }

    public void DecrementStatusEffects()
    {
        if (statusEffects.RemoveAll(effect => effect.DecrementTurnsLeft()) > 0) CalculateEffectChanges();
    }

    public void CalculateEffectChanges()
    {
        float totalAccuracyChange = 0f;
        float totalCoolChange = 0f;
        float totalBlockDamage = 0f;
        float totalDealDamage = 0f;
        float totalHealMultiplier = 0f;

        bool blockAllEffects = false;
        bool blockNegativeEffects = false;
        bool blockPositiveEffects = false;
        bool blockControlEffects = false;
        bool blockTortureEffects = false;
        bool directAttacks = false;

        foreach (StatusEffect effect in statusEffects)
        {
            totalAccuracyChange += effect.statusEffectBase.accuracyPercentChange;
            totalCoolChange += effect.statusEffectBase.coolChange;
            totalBlockDamage += effect.statusEffectBase.blockDamage;
            totalDealDamage += effect.statusEffectBase.damagePercentChange;
            totalHealMultiplier += effect.statusEffectBase.healMultiplier;

            blockAllEffects |= effect.statusEffectBase.blockAllEffects;
            blockNegativeEffects |= effect.statusEffectBase.blockNegativeEffects;
            blockPositiveEffects |= effect.statusEffectBase.blockPositiveEffects;
            blockControlEffects |= effect.statusEffectBase.blockControlEffects;
            blockTortureEffects |= effect.statusEffectBase.blockTortureEffects;
            directAttacks |= effect.statusEffectBase.directAttacks;
        }

        accuracy = 1 + totalAccuracyChange;
        coolMultiplier = 1 + totalCoolChange;
        takeDamageMultiplier = 1 + totalBlockDamage;
        dealDamageMultiplier = 1 + totalDealDamage;
        healMultiplier = 1 + totalHealMultiplier;

        groupImmunities[GroupImmunity.All] = blockAllEffects;
        groupImmunities[GroupImmunity.Negative] = blockNegativeEffects;
        groupImmunities[GroupImmunity.Positive] = blockPositiveEffects;
        groupImmunities[GroupImmunity.Control] = blockControlEffects;
        groupImmunities[GroupImmunity.Torture] = blockTortureEffects;

        isDirectingAttacks = directAttacks;
    }

    public void Cool(float coolPercentage)
    {
        ChangeEnergy(Mathf.RoundToInt(coolPercentage * coolMultiplier * EnergyCapacity * -1));
    }

    public void RemoveEffects(StatusEffectType type)
    {
        statusEffects.RemoveAll(effect => effect.EffectType == type);

        CalculateEffectChanges();
    }

    public void UseRandomMove()
    {
        BattleManager battleManager = GameObject.FindAnyObjectByType<BattleManager>();

        int randomIndex = Random.Range(0, 5);

        while (!MoveUsable(randomIndex)) randomIndex = Random.Range(0, 5);
        
        battleManager.SelectMove(randomIndex);

        if (randomIndex == 4) return;

        int randomTarget = battleManager.PickRandomOppositeIndex();
        battleManager.UseMove(randomTarget);
    }

    public bool MoveUsable(int index)
    {
        if (index == 4) return true;

        Move move = botBase.moves[index];

        return !InCooldown(index) && (CurrentEnergy < EnergyCapacity || move.energy == 0);
    }

    public int PercentOf(float percentage, HealthType type)
    {
        var total = type switch
        {
            HealthType.Armor => ArmorHealth,
            HealthType.Health => Health,
            HealthType.Standard => ArmorHealth + Health,
            _ => 0
        };

        return (int)(total * percentage);
    }

    public void ReviveBot(float healthPercentage, float armorPercentage)
    {
        currentHealth = Mathf.CeilToInt(Health * healthPercentage);
        currentArmor = Mathf.CeilToInt(ArmorHealth * armorPercentage);
        statusEffects = new List<StatusEffect>();

        isDead = false;
        botImage.SetImage(true);
        botImage.SetHealthBarsStarter(currentHealth, currentArmor);
    }
}