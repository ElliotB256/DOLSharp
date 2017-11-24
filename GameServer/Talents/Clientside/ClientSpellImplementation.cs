using System;

namespace DOL.Talents.Clientside
{
    /// <summary>
    /// Implements a talent as a spell icon
    /// </summary>
    public class ClientSpellImplementation : ITalentClientImplementation
    {
        public byte Level { get; set; }
        public ushort InternalID { get; set; }
        public SkillGroupTalent SpellLine {get;set;}
        public ushort Icon { get; set; }
        public String Name { get; set; }
    }
}
