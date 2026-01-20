public class HealthChange
{

    public int ArmorAmount { get; private set; }
    public int HealthAmount { get; private set; }

    public HealthChange(int armor, int health)
    {
        ArmorAmount = armor;
        HealthAmount = health;
    }

    public void Add(HealthChange healthChange)
    {
        ArmorAmount += healthChange.ArmorAmount;
        HealthAmount += healthChange.HealthAmount;
    }
}