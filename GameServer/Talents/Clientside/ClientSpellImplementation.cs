using System;

namespace DOL.Talents.Clientside
{
    /// <summary>
    /// Implements a talent as a spell icon
    /// </summary>
    public class ClientSpellImplementation : ITalentClientImplementation
    {
        public byte Level { get; set; } //not used
        public ushort InternalID { get; set; } //not yet used
        public SkillGroupTalent SkillGroup {get;set;}
        public ushort Icon { get; set; }
        public String Name { get; set; }
    }
}
