using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Class responsible for handling user input and selecting Bots for each team.
/// </summary>
public class BotSelectManager : MonoBehaviour
{
    public BotBase[] bots;

    [Header("Display Settings")]
    public TMP_Text nameText;
    public Image botImage;
    public TMP_Text typeText;
    public TMP_Text healthText;
    public TMP_Text armorHealthText;
    public TMP_Text strengthText;
    public TMP_Text speedText;
    public TMP_Text energyCapacityText;
    public Button selectBotButton;
    public Button battleButton;
    public GameObject botSelectScreen;
    public GameObject battleScreen;
    public Image[] teamPreview;

    BattleManager battleManager;
    int selectedId;

    void Start()
    {
        battleScreen.SetActive(false);
        botSelectScreen.SetActive(true);
        battleManager = FindAnyObjectByType<BattleManager>();
    }

    /// <summary>
    /// Updates the menu to display the currently selected Bot when the user clicks on a Bot's button.
    /// </summary>
    /// <param name="botBase">Bot to be previewed.</param>
    public void PreviewBot(BotBase botBase)
    {
        nameText.text = botBase.name;

        botImage.sprite = botBase.sprite;
        botImage.color = Color.white;

        // stats
        typeText.text = botBase.type.ToString();
        healthText.text = botBase.health.ToString();
        armorHealthText.text = botBase.armorHealth.ToString();
        strengthText.text = botBase.strength.ToString();
        speedText.text = botBase.speed.ToString();
        energyCapacityText.text = botBase.energyCapactiy.ToString();

        selectedId = botBase.id;

        selectBotButton.interactable = true;
    }

    /// <summary>
    /// Adds the currently previewed Bot to a team when the user clicks the "Select" button.
    /// Displays "Battle" button once all slots are filled.
    /// </summary>
    public void SelectBot()
    {
        TeamManager.Instance.AddBot(bots[selectedId]);

        if (TeamManager.Instance.ReadyToBattle())
        {
            selectBotButton.gameObject.SetActive(false);
            battleButton.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// Starts the battle when the user clicks the "Battle" button.
    /// </summary>
    public void StartBattle()
    {
        botSelectScreen.SetActive(false);
        battleScreen.SetActive(true);
        battleManager.StartBattle();
    }

    /// <summary>
    /// Resets teams and returns to the Bot select screen.
    /// </summary>
    public void ResetSelectScreen()
    {
        TeamManager.Instance.ResetTeams();

        foreach (Image image in teamPreview)
        {
            image.sprite = null;
            image.color = Color.clear;
        }

        battleButton.gameObject.SetActive(false);
        selectBotButton.gameObject.SetActive(true);

        battleScreen.SetActive(false);
        botSelectScreen.SetActive(true);
    }

    public void SetPreviewImage(int index, Sprite sprite)
    {
        var previewImage = teamPreview[index];
        previewImage.sprite = sprite;
        previewImage.color = Color.white;
    }
}