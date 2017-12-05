using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOL.GS.ModularSkills
{
    /// <summary>
    /// Occurs when the use of a skill fails the precondition test
    /// </summary>
    public class FailSkillTargetRequirementsEventArgs
    {
        public FailSkillTargetRequirementsEventArgs(eReason reason)
        {
            Reason = reason;
        }

        public enum eReason
        {
            TargetTooFar,
            InvalidTarget,
            CasterDead,
        }

        public eReason Reason { get; private set; }
    }
}
