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
    }
}
