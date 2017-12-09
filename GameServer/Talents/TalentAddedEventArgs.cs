using System;

namespace DOL.Talents
{
    /// <summary>
    /// A talent is added to the ITalentSet
    /// </summary>
    public class TalentAddedEventArgs : EventArgs
    {
        public ITalent Talent { get; private set; }

        public TalentAddedEventArgs(ITalent talent)
        {
            Talent = talent;
        }
    }
}
