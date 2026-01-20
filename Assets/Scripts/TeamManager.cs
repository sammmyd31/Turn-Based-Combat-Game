using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Singleton class responsible for managing both teams.
/// </summary>
public class TeamManager : MonoBehaviour
{
    public static TeamManager Instance;

    public BotImage[] playerBotImages;
    public BotImage[] enemyBotImages;

    List<Bot> playerTeam;
    List<Bot> enemyTeam;
    BotSelectManager botSelectManager;

    public List<Bot> PlayerTeam => playerTeam;
    public List<Bot> EnemyTeam => enemyTeam;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        botSelectManager = FindAnyObjectByType<BotSelectManager>();

        playerTeam = new List<Bot>();
        enemyTeam = new List<Bot>();
    }

    /// <summary>
    /// Boolean value representing if both teams are full and the battle can start.
    /// </summary>
    public bool ReadyToBattle()
    {
        return playerTeam.Count == 3 && enemyTeam.Count == 3;
    }

    /// <summary>
    /// Resets both teams to a new list of Bots.
    /// </summary>
    public void ResetTeams()
    {
        playerTeam = new List<Bot>();
        enemyTeam = new List<Bot>();
    }

    /// <summary>
    /// Creates a new Bot using the given BotBase and adds it to the next available slot.
    /// Ordering is Player Bots 1-3, then Enemy Bots 1-3.
    /// </summary>
    /// <param name="botBase">The selected Bot with its base data.</param>
    public void AddBot(BotBase botBase)
    {
        if (playerTeam.Count < 3)
        {
            botSelectManager.SetPreviewImage(playerTeam.Count, botBase.sprite);

            var botImage = playerBotImages[playerTeam.Count];
            var battleImage = botImage.GetComponentInChildren<Image>();
            battleImage.sprite = botBase.sprite;
            battleImage.color = Color.white;

            playerTeam.Add(new Bot(botBase, 1, true, botImage));
        }
        else if (enemyTeam.Count < 3)
        {
            botSelectManager.SetPreviewImage(enemyTeam.Count + playerTeam.Count, botBase.sprite);

            var botImage = enemyBotImages[enemyTeam.Count];
            var battleImage = botImage.GetComponentInChildren<Image>();
            battleImage.sprite = botBase.sprite;
            battleImage.color = Color.white;

            enemyTeam.Add(new Bot(botBase, 1, false, botImage));
        }
    }
}