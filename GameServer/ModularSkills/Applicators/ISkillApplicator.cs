using System;

namespace DOL.GS.ModularSkills
{
    /// <summary>
    /// Method by which skill effects are applied.
    /// </summary>
    public interface ISkillApplicator
    {
        /// <summary>
        /// Starts attempt to apply effect to target.
        /// </summary>
        void Start(GameObject invoker, GameObject target, SkillComponent sc);

        /// <summary>
        /// Event raised when ISkillApplicator says effect should be
        /// applied to target, eg after LOS checks, on tick or when bolt hits.
        /// </summary>
        event EventHandler<SkillApplicatorAppliedEventArgs> Applied;
    }
}