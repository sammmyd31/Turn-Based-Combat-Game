using UnityEngine;

[CreateAssetMenu(fileName = "Bot", menuName = "Bot")]
public class BotBase : ScriptableObject
{
    public enum Type { Cyborg, Android, Mech, Flying }

    public int id;
    public string name;
    public Sprite sprite;
    public Type type;

    // base stats
    public int health;
    public int armorHealth;
    public int strength;
    public int speed;
    public int energyCapactiy;

    // moves
    public Move[] moves;

    void OnEnable()
    {
#if UNITY_EDITOR
        // Only run in Play mode to avoid messing with editing
        if (!UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) return;
#endif
        foreach (var move in moves)
        {
            move?.GetAttributes();
        }
    }
}