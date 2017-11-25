using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOL.Talents
{
    public interface IUseableTalent : ITalent
    {
        /// <summary>
        /// Invoked when something (GamePlayer or GameLiving) tries to use an IUseableTalent
        /// </summary>
        void Use();
    }
}
