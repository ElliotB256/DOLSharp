using System.Collections.Generic;

namespace DOL.GS.ModularSkills
{
    /// <summary>
    /// A component of a modular skill
    /// </summary>
    public class SkillComponent
    {
        /// <summary>
        /// Target selection method for the skill component
        /// </summary>
        public ITargetSelector TargetSelector { get; set; }

        /// <summary>
        /// Method by which effects are applied to target.
        /// </summary>
        public ISkillApplicator Applicator { get; set; }

        /// <summary>
        /// List of effects to apply to targets.
        /// Subsequent effects are applied if the preceding effect 'Apply'
        /// returns true.
        /// </summary>
        public IList<ISkillEffect> SkillEffectChain { get; set; }
    }
}
