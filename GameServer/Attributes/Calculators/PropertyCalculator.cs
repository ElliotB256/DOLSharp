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
using log4net;

namespace DOL.GS.PropertyCalc
{
    /// <summary>
    /// A calculator implements a formula to determine the value of a property.
    /// </summary>
    public class PropertyCalculator : IPropertyCalculator
	{
		protected static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public PropertyCalculator()
		{
		}

		/// <summary>
		/// calculates the final property value
		/// </summary>
		/// <param name="living"></param>
		/// <param name="property"></param>
		/// <returns></returns>
		public virtual int CalculateValue(GameLiving living, eProperty property, eCalculationType type) 
		{
			return 1;
		}

        /// <summary>
        /// Calculate the cap for this property.
        /// </summary>
        public virtual int GetCap()
        {
            return 0;
        }
	}
}
