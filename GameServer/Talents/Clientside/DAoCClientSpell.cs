using System;

namespace DOL.Talents.Clientside
{
    /// <summary>
    /// Implements a talent as a spell icon
    /// </summary>
    public class DAoCClientSpell : ITalentClientImplementation
    {
        public byte Level { get; set; }
        public ushort InternalID { get; set; }
        public ushort SpellLineIndex { get { return 0xFE; } }
        public ushort Icon { get; set; }
        public String Name { get; set; }
    }
}
