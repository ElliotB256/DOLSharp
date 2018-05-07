using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOL.GS.Combat
{
    /// <summary>
    /// Manages combat for a GameLiving
    /// </summary>
    public class Combat
    {
        public Combat(GameLiving living)
        {
            m_owner = living;
        }

        protected GameLiving m_owner;
    }
}
