using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum ValueType { Health, Energy }

public class BotImage : MonoBehaviour
{
    [Header("Durations")]
    public float healthBarDuration;
    public float energyBarDuration;

    public Image image;
    public Slider healthBar;
    public Slider armorBar;
    public Slider energyBar;
    public TMP_Text healthText;
    public TMP_Text armorText;
    public TMP_Text energyText;
    public Image energyFillImage;
    public Color fullEnergyColor;
    public Image healthPreviewBar;
    public Image armorPreviewBar;
    public Image energyPreviewBar;
    public Color damageColor;
    public Color healColor;
    public Color energyColor;
    public Color positiveEffectColor;
    public Color negativeEffectColor;
    public Color extraTurnColor;
    public GameObject[] moveVisuals;

    Bot bot;
    Color originalEnergyColor;
    Animator animator;

    public void AssignBot(Bot b)
    {
        bot = b;

        healthBar.maxValue = bot.Health;
        healthBar.value = healthBar.maxValue;
        healthText.text = healthBar.value + "/" + healthBar.maxValue;

        armorBar.maxValue = bot.ArmorHealth;
        armorBar.value = armorBar.maxValue;
        armorText.text = armorBar.value + "/" + armorBar.maxValue;

        energyBar.maxValue = bot.EnergyCapacity;
        energyBar.value = 0;
        energyText.text = energyBar.value + "/" + energyBar.maxValue;

        originalEnergyColor = energyFillImage.color;

        animator = GetComponentInChildren<Animator>();
    }

    public void GreyOut()
    {
        image.color = Color.gray;
    }

    public void RevertGreyOut()
    {
        image.color = Color.white;
    }

    public bool IsTargetable()
    {
        return image.color == Color.white;
    }

    public void SetImage(bool b)
    {
        gameObject.SetActive(b);
        armorBar.gameObject.SetActive(b);
    }

    public void SetHover(bool b)
    {
        animator.SetBool("isHovering", b);
    }

    public void SetHealthBarsStarter(int health, int armor)
    {
        StartCoroutine(SetHealthBars(health, armor));
    }

    IEnumerator SetHealthBars(int health, int armor)
    {
        float startArmor = armorBar.value;
        float targetArmor = armor;

        float startHealth = healthBar.value;
        float targetHealth = health;

        float armorDelta = targetArmor - startArmor;
        float healthDelta = targetHealth - startHealth;

        float totalChange = Mathf.Abs(armorDelta) + Mathf.Abs(healthDelta);

        float elapsed = 0f;

        while (elapsed < healthBarDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / healthBarDuration);
            float easedT = EaseOut(t);

            float progressedChange = easedT * totalChange;

            float currentArmor = startArmor;
            float currentHealth = startHealth;

            bool healing = armorDelta > 0 || healthDelta > 0;

            if (healing)
            {
                // health animates first, then armor
                if (Mathf.Abs(healthDelta) > 0)
                {
                    float healthProgress = Mathf.Min(progressedChange, Mathf.Abs(healthDelta));
                    currentHealth = startHealth + Mathf.Sign(healthDelta) * healthProgress;
                    progressedChange -= healthProgress;
                }

                if (Mathf.Abs(armorDelta) > 0 && progressedChange > 0)
                {
                    float armorProgress = Mathf.Min(progressedChange, Mathf.Abs(armorDelta));
                    currentArmor = startArmor + Mathf.Sign(armorDelta) * armorProgress;
                }
            }
            else
            {
                // armor animates first, then health
                if (Mathf.Abs(armorDelta) > 0)
                {
                    float armorProgress = Mathf.Min(progressedChange, Mathf.Abs(armorDelta));
                    currentArmor = startArmor + Mathf.Sign(armorDelta) * armorProgress;
                    progressedChange -= armorProgress;
                }

                if (Mathf.Abs(healthDelta) > 0 && progressedChange > 0)
                {
                    float healthProgress = Mathf.Min(progressedChange, Mathf.Abs(healthDelta));
                    currentHealth = startHealth + Mathf.Sign(healthDelta) * healthProgress;
                }
            }

            armorBar.value = currentArmor;
            armorText.text = Mathf.CeilToInt(currentArmor) + "/" + armorBar.maxValue;
            armorBar.gameObject.SetActive(currentArmor > 0);

            healthBar.value = currentHealth;
            healthText.text = Mathf.CeilToInt(currentHealth) + "/" + healthBar.maxValue;

            yield return null;
        }

        // final snap
        armorBar.value = targetArmor;
        armorText.text = Mathf.CeilToInt(targetArmor) + "/" + armorBar.maxValue;
        armorBar.gameObject.SetActive(targetArmor > 0);

        healthBar.value = targetHealth;
        healthText.text = Mathf.CeilToInt(targetHealth) + "/" + healthBar.maxValue;
    }

    public void SetEnergyBarStarter()
    {
        StartCoroutine(SetEnergyBar());
    }

    public IEnumerator SetEnergyBar()
    {
        float startValue = float.Parse(energyText.text.Split('/')[0]); ;
        float currentEnergy = bot.CurrentEnergy;
        float elapsed = 0f;

        while (elapsed < energyBarDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / energyBarDuration);

            float currentAnimatedValue = Mathf.Lerp(startValue, currentEnergy, t);
            energyBar.value = Mathf.Min(currentAnimatedValue, energyBar.maxValue);

            energyText.text = Mathf.RoundToInt(currentAnimatedValue) + "/" + energyBar.maxValue;

            if (currentAnimatedValue >= energyBar.maxValue) energyFillImage.color = fullEnergyColor;
            else energyFillImage.color = originalEnergyColor;

            yield return null;
        }

        energyBar.value = Mathf.Min(currentEnergy, energyBar.maxValue);
        energyText.text = currentEnergy + "/" + energyBar.maxValue;
    }

    float EaseOut(float t)
    {
        return 1f - Mathf.Pow(1f - t, 3f);
    }

    public void DamagePreview(int damage)
    {
        int remainingDamage = damage;

        // apply damage to armor first
        int predictedArmor = Mathf.Max(bot.CurrentArmor - remainingDamage, 0);
        remainingDamage = Mathf.Max(remainingDamage - bot.CurrentArmor, 0);

        // then apply leftover damage to health
        int predictedHealth = Mathf.Max(bot.CurrentHealth - remainingDamage, 0);

        // fill amounts for preview
        float currentArmorFill = bot.CurrentArmor / armorBar.maxValue;
        float predictedArmorFill = predictedArmor / armorBar.maxValue;

        float currentHealthFill = bot.CurrentHealth / healthBar.maxValue;
        float predictedHealthFill = predictedHealth / healthBar.maxValue;

        // armor preview
        if (bot.CurrentArmor > 0)
        {
            armorPreviewBar.fillAmount = currentArmorFill;
            armorPreviewBar.rectTransform.anchorMin = new Vector2(predictedArmorFill, 0f);
            armorPreviewBar.rectTransform.anchorMax = new Vector2(currentArmorFill, 1f);
            armorPreviewBar.rectTransform.offsetMin = Vector2.zero;
            armorPreviewBar.rectTransform.offsetMax = Vector2.zero;
            armorPreviewBar.enabled = true;
        }
        else
        {
            armorPreviewBar.enabled = false;
        }

        // health preview
        if (damage > bot.CurrentArmor) // only preview health damage if armor breaks
        {
            healthPreviewBar.fillAmount = currentHealthFill;
            healthPreviewBar.rectTransform.anchorMin = new Vector2(predictedHealthFill, 0f);
            healthPreviewBar.rectTransform.anchorMax = new Vector2(currentHealthFill, 1f);
            healthPreviewBar.rectTransform.offsetMin = Vector2.zero;
            healthPreviewBar.rectTransform.offsetMax = Vector2.zero;
            healthPreviewBar.enabled = true;
        }
        else healthPreviewBar.enabled = false;

        // update predicted health text
        armorText.text = predictedArmor + "/" + armorBar.maxValue;
        healthText.text = predictedHealth + "/" + healthBar.maxValue;

        // preview damage output
        foreach (GameObject visual in moveVisuals)
        {
            if (visual.name.Equals("Value Text"))
            {
                var visualText = visual.GetComponent<TMP_Text>();
                var visualAnimator = visual.GetComponent<Animator>();

                visualText.text = "-" + damage;
                visualText.color = damageColor;

                visual.SetActive(true);

                visualAnimator.SetTrigger("preview");

                break;
            }
        }
    }

    public void EnergyPreview(int energy)
    {
        // energy preview
        int predictedEnergy = bot.CurrentEnergy + energy;
        int cappedEnergy = Mathf.Min(predictedEnergy, bot.EnergyCapacity);

        float currentFill = bot.CurrentEnergy / energyBar.maxValue;
        float predictedFill = cappedEnergy / energyBar.maxValue;

        float minFill = Mathf.Min(currentFill, predictedFill);
        float maxFill = Mathf.Max(currentFill, predictedFill);

        energyPreviewBar.fillAmount = maxFill;
        energyPreviewBar.rectTransform.anchorMin = new Vector2(minFill, 0f);
        energyPreviewBar.rectTransform.anchorMax = new Vector2(maxFill, 1f);
        energyPreviewBar.rectTransform.offsetMin = Vector2.zero;
        energyPreviewBar.rectTransform.offsetMax = Vector2.zero;
        energyPreviewBar.enabled = true;

        energyText.text = predictedEnergy + "/" + energyBar.maxValue;
    }

    public void HidePreview()
    {
        healthPreviewBar.enabled = false;
        armorPreviewBar.enabled = false;
        energyPreviewBar.enabled = false;
        healthText.text = bot.CurrentHealth + "/" + healthBar.maxValue;
        armorText.text = bot.CurrentArmor + "/" + armorBar.maxValue;
        energyText.text = bot.CurrentEnergy + "/" + energyBar.maxValue;

        if (!IsTargetable()) return;

        foreach (GameObject visual in moveVisuals)
        {
            if (visual.name.Equals("Value Text"))
            {
                var damageAnimator = visual.GetComponent<Animator>();

                damageAnimator.SetTrigger("endPreview");

                break;
            }
        }
    }

    public void ShowVisual(string effectName, StatusEffectType effectType)
    {
        foreach (GameObject visual in moveVisuals)
        {
            if (visual.name.Equals("Effect Text"))
            {
                var effectText = visual.GetComponent<TMP_Text>();
                var effectAnimator = visual.GetComponent<Animator>();

                if (effectType == StatusEffectType.Positive) effectText.color = positiveEffectColor;
                else effectText.color = negativeEffectColor;

                effectText.text = effectName;

                if (visual.activeSelf) effectAnimator.SetTrigger("wait");
                else
                {
                    visual.SetActive(true);
                    effectAnimator.SetTrigger("goUp");
                }
            }
        }
    }

    public void ShowVisual(string targetVisual, int amount, ValueType valueType)
    {
        foreach (GameObject visual in moveVisuals)
        {
            if (visual.name.Equals(targetVisual))
            {
                var valueText = visual.GetComponent<TMP_Text>();

                if (amount > 0) valueText.text = "+" + amount;
                else valueText.text = amount.ToString();

                valueText.color = valueType switch
                {
                    ValueType.Health when amount < 0 => damageColor,
                    ValueType.Health when amount > 0 => healColor,
                    ValueType.Energy => energyColor,
                    _ => Color.white,
                };

                visual.SetActive(true);

                var valueAnimator = visual.GetComponent<Animator>();
                valueAnimator.SetTrigger("activate");

                break;
            }
        }
    }

    public void ShowVisual(string message)
    {
        foreach (GameObject visual in moveVisuals)
        {
            if (visual.name.Equals("Message Text"))
            {
                var statText = visual.GetComponent<TMP_Text>();
                statText.text = message;

                visual.SetActive(true);

                break;
            }
        }
    }
}