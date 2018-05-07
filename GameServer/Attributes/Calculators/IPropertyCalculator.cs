/*
 * DAWN OF LIGHT - The first free open source DAoC server emulator
 * 
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; either version 2
 * of the License, or (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
 *
 */

namespace DOL.GS.PropertyCalc
{
    public enum eCalculationType
    {
        All,        // Complete modified property value
        NotBuffs,   // Modified property value from all but buffs
        NotItems,   // Modified property value from all but items
        Bonus,      // Amount property value is increased from all bonuses (does not include base)
        Base        // Value of unmodified property
    }

    /// <summary>
    /// A calculator implements a formula to determine the value of a property.
    /// </summary>
    public interface IPropertyCalculator
	{
        /// <summary>
        /// Calculates the property value using a number of 
        /// </summary>
        int CalculateValue(GameLiving living, eProperty propery, eCalculationType type);

        /// <summary>
        /// Returns the cap for this property
        /// </summary>
        /// <returns></returns>
        int GetCap(eCalculationType type);
	}
}