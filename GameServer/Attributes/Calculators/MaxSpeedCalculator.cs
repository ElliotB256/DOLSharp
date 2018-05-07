using System;

using DOL.GS.Keeps;

namespace DOL.GS.PropertyCalc
{
	/// <summary>
	/// Calculates the maximum speed of a living
	/// </summary>
	[PropertyCalculator(eProperty.MaxHealth)]
	public class MaxSpeedCalculator : PropertyCalculator
    {
        public override int CalculateValue(GameLiving living, eProperty property, eCalculationType type)
        {
            // base speed starts at 191
            int speed = 191;

            //speed bonus is percentage.
            int speedPercent = living.Attributes.GetProperty(eProperty.MaxSpeed, type);
            speedPercent = Math.Min(speedPercent, GetCap());

            speed = (int)((float)speedPercent / 100f * speed);
            return speed;
        }

        public override int GetCap(eCalculationType type)
        {
            return 250;
        }
    }
}
