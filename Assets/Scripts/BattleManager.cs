using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Class responsible for handling battle logic and turn behavior.
/// </summary>
public class BattleManager : MonoBehaviour
{
    [Header("Durations")]
    public float damageDuration = 3f;
    public float setupDuration = 2f;
    public float standardDuration = 1f;
    public float visualDuration = 1f;
    public float actionDuration = .5f;

    [Header("Other")]
    public int generateNumTurns = 5;
    public float cooldownPercentage = .4f;
    public float coolingPercentage = .1f;

    [Header("Objects")]
    public TMP_Text turnDescription;
    public GameObject moveButtonsObject;
    public GameObject moveInfo;
    public GameObject battleOver;

    List<Bot> bots;
    public static List<Bot> turnOrder;
    TMP_Text moveDescription;
    TMP_Text moveStats;
    TMP_Text winText;
    TMP_Text[] moveButtonsText;
    Button[] moveButtons;
    Bot currentBot;
    Bot currentTarget;
    Move currentMove;
    Move[] currentMoves;
    bool isSelectingMove = false;
    bool usedMove = false;
    bool usedCooldown = false;
    bool actionsFinished = false;
    string currentBotTeam;
    int moveID;
    float waitAfterActions;
    bool checkedTaunt;

    /// <summary>
    /// Starts the battle and performs necessary functions before starting the turn cycle.
    /// </summary>
    public void StartBattle()
    {
        GetComponents();

        turnDescription.text = "Battle Started.";

        bots = new List<Bot>(TeamManager.Instance.PlayerTeam);
        bots.AddRange(TeamManager.Instance.EnemyTeam);

        turnOrder = new List<Bot>();

        // break ties before calculating turns, loops until no ties are found
        HandleTies();

        CalculateTurns(generateNumTurns);

        Invoke("StartTurns", 2);
    }

    /// <summary>
    /// Gets various text and button components if they are not assigned.
    /// </summary>
    void GetComponents()
    {
        if (moveButtonsText == null) moveButtonsText = moveButtonsObject.GetComponentsInChildren<TMP_Text>();
        if (moveButtons == null) moveButtons = moveButtonsObject.GetComponentsInChildren<Button>();
        if (!moveDescription || !moveStats)
        {
            var tempMoveInfoTexts = moveInfo.GetComponentsInChildren<TMP_Text>();
            moveDescription = tempMoveInfoTexts[0];
            moveStats = tempMoveInfoTexts[1];
        }
        if (!winText) winText = battleOver.GetComponentInChildren<TMP_Text>();
    }

    /// <summary>
    /// Breaks ties for all Bots. Only needs to be done once at the start of the battle.
    /// </summary>
    void HandleTies()
    {
        bool done = false;
        while (!done)
            done = Bot.BreakTies(bots);
    }

    /// <summary>
    /// Calculates a given number of turns based on each Bot's speed, bots are added to turnOrder. Assumes ties have been handled.
    /// </summary>
    /// <param name="numOfTurns">The number of turns to be calculated and added to the turn order.</param>
    void CalculateTurns(int numOfTurns)
    {
        while (turnOrder.Count < numOfTurns)
        {
            List<Bot> readyBots = new List<Bot>();

            // readyBots contains all bots that are ready for a turn this cycle
            foreach (Bot bot in bots)
            {
                if (!bot.IsDead && bot.IncrementSpeedCounter() >= 1000)
                {
                    readyBots.Add(bot);
                    bot.DecrementSpeedCounter();
                }
            }

            if (readyBots.Count > 0)
            {
                readyBots.Sort((b1, b2) => b1.SpeedCounter.CompareTo(b2.SpeedCounter));
                readyBots.Reverse();

                turnOrder.AddRange(readyBots);
            }
        }
    }

    /// <summary>
    /// Wrapper method for DoTurn coroutine to allow use of Invoke() method.
    /// </summary>
    void StartTurns()
    {
        StartCoroutine(DoTurn());
    }

    /// <summary>
    /// Coroutine for completing turn cycles until the battle is over.
    /// </summary>
    IEnumerator DoTurn()
    {
        while (!IsBattleOver())
        {
            yield return StartCoroutine(SetupTurn());

            if (Bot.deathsThisTurn.Count == 0 && !currentBot.SkipTurn)
            {
                yield return StartCoroutine(ExecuteMove());                
            }

            yield return StartCoroutine(CheckDeaths());

            EffectsTurnEnd();
        }
        BattleOver();
    }

    /// <summary>
    /// Coroutine responsible for turn setup.
    /// </summary>
    IEnumerator SetupTurn()
    {
        // calculate more turns if turn order is low
        if (turnOrder.Count <= 1) CalculateTurns(generateNumTurns);

        currentBot = turnOrder[0];
        turnOrder.RemoveAt(0);

        yield return StartCoroutine(currentBot.EffectsTurnStart(standardDuration, damageDuration));

        if (currentBot.SkipTurn) yield break;

        currentMoves = currentBot.botBase.moves;

        if (currentBot.RandomAttack)
        {
            currentBot.UseRandomMove();
            yield break;
        }

        // set up descriptions
        if (currentBot.IsPlayer) currentBotTeam = "(Player 1)";
        else currentBotTeam = "Player 2";

        turnDescription.text = currentBotTeam + ": " + currentBot.Name + "'s turn.";

        currentBot.BotImage.SetHover(true);

        // set move buttons text based on if the move is usable
        for (int i = 0; i < moveButtonsText.Length - 1; i++)
        {
            var move = currentMoves[i];

            if (currentBot.InCooldown(i))
            {
                moveButtons[i].interactable = false;
                moveButtonsText[i].text = currentBot.GetCooldown(i) + " Turns Left";
            }
            else if (currentBot.CurrentEnergy >= currentBot.EnergyCapacity && move.energy > 0)
            {
                moveButtons[i].interactable = false;
                moveButtonsText[i].text = "Energy Capacity Full";
            }
            else
            {
                moveButtons[i].interactable = true;
                moveButtonsText[i].text = currentMoves[i].name;
            }
        }
    }

    /// <summary>
    /// Coroutine responsible for executing the player's move. Waits for user input, performs necessary functions for the
    /// move, and finishes once all actions have been completed.
    /// </summary>
    IEnumerator ExecuteMove()
    {
        if (!usedMove) InfoBarGoTo(moveButtonsObject);

        yield return new WaitUntil(() => usedMove);
        usedMove = false;

        currentBot.BotImage.SetHover(false); 

        if (usedCooldown)
        {
            usedCooldown = false;

            turnDescription.text = currentBotTeam + ": " + currentBot.Name + " used Cooldown";
            InfoBarGoTo(turnDescription.gameObject);

            currentBot.Cool(cooldownPercentage);

            yield return new WaitForSeconds(standardDuration);
        }
        else
        {
            // announce the move used
            turnDescription.text = currentBotTeam + ": " + currentBot.Name + " used " + currentMove.name + ".";
            InfoBarGoTo(turnDescription.gameObject);

            currentBot.StartCooldown(moveID);

            currentBot.ChangeEnergy(currentMove.energy);

            yield return new WaitForSeconds(standardDuration);

            // carry out actions in the move
            StartCoroutine(CarryOutActions(currentMove.actions));

            // wait for actions/animations to finish
            yield return new WaitUntil(() => actionsFinished);
            yield return new WaitForSeconds(waitAfterActions);
            waitAfterActions = 0f;
            actionsFinished = false;

            currentBot.Cool(coolingPercentage);

            yield return new WaitForSeconds(standardDuration);
        }

        // decrement cooldowns
        for (int i = 0; i < currentMoves.Length; i++) currentBot.DecrementCooldown(i);
    }

    /// <summary>
    /// Checks for deaths this turn, and makes the neccessary adjustments based on it.
    /// </summary>
    IEnumerator CheckDeaths()
    {
        if (Bot.deathsThisTurn.Count > 0)
        {
            foreach (Bot bot in Bot.deathsThisTurn)
            {
                turnOrder.RemoveAll((x) => x == bot);
                bot.BotImage.SetImage(false);

                yield return new WaitForSeconds(standardDuration);
            }

            Bot.deathsThisTurn.Clear();
        }
    }

    /// <summary>
    /// Given a list of actions, carrys each action out sequentially.
    /// </summary>
    /// <param name="actions">List of actions to be executed.</param>
    IEnumerator CarryOutActions(List<MoveAction> actions)
    {
        // stores whether the attack hit or missed for each enemy targeted
        Dictionary<Bot, bool> hitResults = new Dictionary<Bot, bool>();

        // stores whether the visual for a missed attack has been shown or not
        HashSet<Bot> missVisualShown = new HashSet<Bot>();

        foreach (MoveAction action in actions)
        {
            var targets = GetTargets(action.targetType);

            foreach (Bot target in targets)
            {
                if (RollAccuracy(target, action.targetType, hitResults))
                {
                    action.CarryOutAction(target, currentBot);
                }
                else if (!missVisualShown.Contains(target))
                {
                    target.BotImage.ShowVisual("Missed!");
                    missVisualShown.Add(target);
                }
            }

            // calculates time needed to wait after the action is finished to let animations play out
            // visualDuration is the standard wait type, damageDuration is a different, longer wait time
            var waitDuration = action.GetType() == typeof(DamageAction) ? damageDuration : visualDuration;

            // add more wait time if it is greater than the current wait time
            // prevents the turn from continuing early before the animations have completed.
            if (waitDuration > waitAfterActions) waitAfterActions += waitDuration;

            yield return new WaitForSeconds(actionDuration);

            waitAfterActions -= actionDuration;
        }
        actionsFinished = true;
        checkedTaunt = false;
    }

    /// <summary>
    /// Returns a list of targets using the bots list from the given targetType. TargetType values of SingleEnemy, SingleAlly,
    /// and Self return a list containing a single Bot, while AllEnemies and Allies return lists containing up to three Bots
    /// (only Bots that are still alive).
    /// </summary>
    /// <param name="targetType">Type of bots that have been targeted.</param>
    /// <returns>List of Bots that are targeted.</returns>
    List<Bot> GetTargets(TargetType targetType)
    {
        switch (targetType)
        {
            case TargetType.SingleEnemy:
                CheckTargetOverride();
                return new List<Bot> {currentTarget};
                
            case TargetType.SingleAlly:
            case TargetType.DeadAlly:
                return new List<Bot> {currentTarget};
            
            case TargetType.Self:
                return new List<Bot> {currentBot};

            case TargetType.AllEnemies:
                // targets should be on the same team as the selected target (currentTarget)
                return bots.Where(bot => bot.IsPlayer == currentTarget.IsPlayer && !bot.IsDead).ToList();

            case TargetType.AllAllies:
                // targets should be on the same team as the current bot (currentBot)
                return bots.Where(bot => bot.IsPlayer == currentBot.IsPlayer && !bot.IsDead).ToList();

            default:
                Debug.LogWarning("Unhandled target type: " + targetType);
                return new List<Bot>();
        }
    }

    /// <summary>
    /// Checks if the other team currently has a Bot that is directing single target attacks. Updates currentTarget if
    /// one is found.
    /// </summary>
    void CheckTargetOverride()
    {
        if (checkedTaunt == true) return;

        checkedTaunt = true;

        var tauntingBots = bots.Where(bot => bot.IsPlayer == currentTarget.IsPlayer &&
                                             !bot.IsDead &&
                                             bot.IsDirectingAttacks).ToList();

        var count = tauntingBots.Count;

        if (count == 0) return;

        if (count == 1)
        {
            currentTarget = tauntingBots[0];
            return;
        }

        var highestLife = tauntingBots[0];
        foreach (Bot bot in tauntingBots)
        {
            var currentHealth = highestLife.CurrentArmor + highestLife.CurrentHealth;
            var newHealth = bot.CurrentArmor + bot.CurrentHealth;

            highestLife = currentHealth >= newHealth ? highestLife : bot;
        }
        currentTarget = highestLife;
        return;
    }

    /// <summary>
    /// Returns a boolean indicating whether the current move will hit or miss. Only attacks that target enemies have the
    /// potential to miss, attacks targeting allies or themself will always hit.
    /// </summary>
    /// <param name="target">The targeted bot.</param>
    /// <param name="targetType">The TargetType of the action.</param>
    /// <param name="hitResults">A dictionary that has a boolean key for each target Bot.</param>
    /// <returns></returns>
    bool RollAccuracy(Bot target, TargetType targetType, Dictionary<Bot, bool> hitResults)
    {
        // only attacks targetting enemies can miss
        if (targetType != TargetType.SingleEnemy && targetType != TargetType.AllEnemies) return true;

        // this check allows a move to have a single accuracy check during the first action
        // if the first action hits or misses, the following actions do the same
        if (!hitResults.ContainsKey(target))
        {
            hitResults[target] = Random.value <= currentBot.Accuracy;
        }
        return hitResults[target];
    }

    /// <summary>
    /// Changes the info bar display screen to show the given target screen.
    /// </summary>
    /// <param name="targetScreen">Screen to show. Should be either turnDescription.gameObject, moveButtonsObject,
    /// or moveInfo.</param>
    void InfoBarGoTo(GameObject targetScreen)
    {
        turnDescription.gameObject.SetActive(false);
        moveButtonsObject.SetActive(false);
        moveInfo.SetActive(false);

        targetScreen.SetActive(true);
    }

    /// <summary>
    /// When the user selects and previews a move. Updates the info bar and previews damage on targetable bots. If cooldown
    /// is selected, it is used immediately.
    /// </summary>
    /// <param name="moveNum">The index of the move to be used. Must be an integer 1-5.</param>
    public void SelectMoveButton(int moveNum)
    {
        SelectMove(moveNum);

        moveDescription.text = currentMove.Description;
        moveStats.text = currentMove.AttackPower + " AP\n" + currentMove.energy + " E\n" + currentMove.cooldown + " CD";

        var target = currentMove.selectedTarget;

        // highlight targetable bots
        foreach (Bot bot in bots)
        {
            if (target == TargetType.DeadAlly && bot.IsDead && bot.IsPlayer == currentBot.IsPlayer)
            {
                bot.BotImage.SetImage(true);
            }

            bool shouldGreyOut = target switch
            {
                TargetType.SingleEnemy or 
                TargetType.AllEnemies => bot.IsPlayer == currentBot.IsPlayer,

                TargetType.SingleAlly or 
                TargetType.AllAllies => bot.IsPlayer != currentBot.IsPlayer,

                TargetType.DeadAlly => !bot.IsDead || bot.IsPlayer != currentBot.IsPlayer,
                
                TargetType.Self => bot != currentBot,

                _ => false
            };


            if (shouldGreyOut) bot.BotImage.GreyOut();
        }

        // preview move damage and energy
        foreach (Bot bot in bots)
        {
            if (currentMove.AttackPower > 0 && bot.BotImage.IsTargetable())
            {
                bot.BotImage.DamagePreview(-Move.CalculateDamage(currentMove.AttackPower, 
                                                                 currentBot.Strength, 
                                                                 currentBot.DealDamageMultiplier));
            }
         
        }

        if (currentMove.energy > 0)
            currentBot.BotImage.EnergyPreview(currentMove.energy);

        InfoBarGoTo(moveInfo);

        isSelectingMove = true;
    }

    public void SelectMove(int moveNum)
    {
        // use cooldown immediately
        if (moveNum == 4)
        {
            usedCooldown = true;
            usedMove = true;
            return;
        }

        currentMove = currentMoves[moveNum];
        moveID = moveNum;
    }

    /// <summary>
    /// When the player backs out of a selected move.
    /// </summary>
    public void BackButton()
    {
        foreach (Bot bot in bots)
        {
            bot.BotImage.HidePreview();
            bot.BotImage.RevertGreyOut();

            if (bot.IsDead)
            {
                bot.BotImage.SetImage(false);
            }
        }

        InfoBarGoTo(moveButtonsObject);

        isSelectingMove = false;
    }

    /// <summary>
    /// When the user selects a target with a move selected.
    /// </summary>
    /// <param name="index">The index of the Bot that was targeted. Must be an int 0-5.</param>
    public void UseMove(int index)
    {
        currentTarget = bots[index];

        if (currentBot.RandomAttack)
        {
            usedMove = true;
            return;
        }

        if (isSelectingMove && currentTarget.BotImage.IsTargetable())
        {
            foreach (Bot bot in bots)
            {
                bot.BotImage.HidePreview();
                bot.BotImage.RevertGreyOut();

                if (bot.IsDead)
                {
                    bot.BotImage.SetImage(false);
                }
            }

            isSelectingMove = false;
            usedMove = true;
        }
    }

    /// <summary>
    /// Returns and integer value corresponding to an active Bot. The index is numbered 0-5, and it randomly selects a Bot that
    /// is opposite to the target type (if the target is a single ally, it returns an enemy index). This is because this should
    /// be used for corrupt, using a random move against its own team.
    /// </summary>
    /// <returns>The index of the randomly selected Bot.</returns>
    public int PickRandomOppositeIndex()
    {
        TargetType targetType = currentMove.selectedTarget;

        var currentIndex = bots.FindIndex(bot => bot == currentBot);

        if (targetType == TargetType.Self) return currentIndex;

        while (true)
        {
            int randomIndex = Random.Range(0, 3);

            switch (targetType)
            {
                case TargetType.SingleEnemy:
                case TargetType.AllEnemies:
                    if (currentIndex > 2) randomIndex += 3;
                    if (bots[randomIndex].IsDead) break;
                    return randomIndex;
                
                case TargetType.SingleAlly:
                case TargetType.AllAllies:
                    if (currentIndex < 3) randomIndex += 3;
                    if (bots[randomIndex].IsDead) break;
                    return randomIndex;
                
                default:
                    Debug.LogWarning("No random target was found.");
                    return -1;
            }
        }
    }

    /// <summary>
    /// Decrements all status effects on the current Bot after the turn is over.
    /// </summary>
    void EffectsTurnEnd()
    {
        currentBot.DecrementStatusEffects();
    }

    /// <summary>
    /// Checks if all three Bots on either team are dead, indicating the battle is over.
    /// </summary>
    /// <returns>A boolean value representing if the battle is over.</returns>
    bool IsBattleOver()
    {
        int numDeadPlayer = TeamManager.Instance.PlayerTeam.Count(bot => bot.IsDead);
        int numDeadEnemy = TeamManager.Instance.EnemyTeam.Count(bot => bot.IsDead);

        return numDeadEnemy == 3 || numDeadPlayer == 3;
    }

    /// <summary>
    /// Run once the battle is over and the winner is determined.
    /// </summary>
    void BattleOver()
    {
        turnDescription.text = "";

        string winner;

        if (turnOrder.Count == 0) CalculateTurns(1);
        if (turnOrder[0].IsPlayer) winner = "Player 1";
        else winner = "Player 2";

        winText.text = winner + " wins!";
        battleOver.SetActive(true);
    }

    /// <summary>
    /// Once the user clicks the button to return to the Bot select screen after a battle is completed.
    /// </summary>
    public void ReturnToSelectScreen()
    {
        BotSelectManager botSelectManager = FindAnyObjectByType<BotSelectManager>();
        botSelectManager?.ResetSelectScreen();

        battleOver.SetActive(false);

        foreach (Bot bot in bots)
        {
            bot.BotImage.SetImage(true);
        }
    }
}