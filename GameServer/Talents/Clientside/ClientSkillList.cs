using System.Linq;
using System.Collections.Generic;

namespace DOL.Talents.Clientside
{
    /// <summary>
    /// A list of spells as presented to the client.
    /// </summary>
    public class ClientSkillList
    {
        public ClientSkillList(SkillGroupTalent line)
        {
            Talents = new List<ITalent>();
            SpellLine = line;
        }

        public SkillGroupTalent SpellLine { get; private set; }
        public List<ITalent> Talents { get; private set; }

        /// <summary>
        /// List of Talents implemented clientside as spells
        /// </summary>
        public List<ITalent> Spells { get
            {
                return new List<ITalent>(Talents.Where(p => p.ClientImplementation is ClientSpellImplementation));
            }
        }

        /// <summary>
        /// List of Talents implemented clientside as styles
        /// </summary>
        public List<ITalent> Styles
        {
            get
            {
                return new List<ITalent>(Talents.Where(p => p.ClientImplementation is ClientStyleImplementation));
            }
        }
    }
}
