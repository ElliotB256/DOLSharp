using System;

namespace DOL.Talents.Clientside
{
    /// <summary>
    /// Implements a talent as an ability
    /// </summary>
    public class ClientAbilityImplementation : ITalentClientImplementation
    {
        public byte Level { get; set; }
        public ushort InternalID { get; set; }
        public ushort Icon { get; set; }
        public String Name { get; set; }
    }
}
