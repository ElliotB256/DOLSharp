using System;

namespace DOL.Talents
{
    /// <summary>
    /// A talent is removed from the ITalentSet
    /// </summary>
    public class TalentRemovedEventArgs : EventArgs
    {
        public ITalent Talent { get; private set; }

        public TalentRemovedEventArgs(ITalent talent)
        {
            Talent = talent;
        }
    }
}
