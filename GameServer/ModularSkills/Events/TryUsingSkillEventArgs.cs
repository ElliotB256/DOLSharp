using System;

namespace DOL.GS.ModularSkills
{
    /// <summary>
    /// Something attempts to use a skill
    /// </summary>
    public class TryUsingSkillEventArgs : EventArgs
    {
        public TryUsingSkillEventArgs(GameLiving invoker, ModularSkill skill)
        {
            Invoker = invoker;
            Skill = skill;
            ShouldContinue = true;
            StopReason = FailSkillUseReason.NoReason;
        }

        /// <summary>
        /// Invoker using the skill
        /// </summary>
        public GameLiving Invoker { get; private set; }

        /// <summary>
        /// Skill that is being used
        /// </summary>
        public ModularSkill Skill { get; private set; }

        /// <summary>
        /// Should the skill continue to be used after event?
        /// </summary>
        public bool ShouldContinue { get; private set; }

        public FailSkillUseReason StopReason { get; private set; }

        public void Stop(FailSkillUseReason reason)
        {
            ShouldContinue = false;
            StopReason = reason;
        }
    }
}
