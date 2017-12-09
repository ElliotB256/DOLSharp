using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOL.GS.ModularSkills
{
    /// <summary>
    /// Occurs when use of a skill fails
    /// </summary>
    public class FailSkillUseEventArgs
    {
        public FailSkillUseEventArgs(FailSkillUseReason reason)
        {
            Reason = reason;
        }

        public FailSkillUseReason Reason { get; private set; }
    }

    /// <summary>
    /// Reasons for failing to use a skill
    /// </summary>
    public enum FailSkillUseReason
    {
        NoReason,
        TargetTooFar,
        InvalidTarget,
        CasterDead,
        AlreadyUsingAnotherSkill,
        InterruptedByMoving,
        InterruptedByAttack,
    }
}
