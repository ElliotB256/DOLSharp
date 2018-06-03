using System;

using DOL.GS.Keeps;

namespace DOL.GS.PropertyCalc
{
	/// <summary>
	/// Calculates the maximum health of a living
	/// </summary>
	[PropertyCalculator(eProperty.MaxHealth)]
	public class MaxHealthCalculator : PropertyCalculator
    {
        public override int CalculateValue(GameLiving living, eProperty property, eCalculationType type)
        {
            // base health starts at 50.
            int health = BASE_HEALTH;
            health += living.Attributes.GetProperty(eProperty.Constitution, type);
            return health;
        }

        public const int BASE_HEALTH = 50;

        public int GetBonusHitpoints(int con, int MaxHealth)
        {
            return con + MaxHealth;
        }

        public override int GetCap(eCalculationType type)
        {
            //no cap on max health - it is capped through the dependent stats
            return 0;
        }
    }
}
