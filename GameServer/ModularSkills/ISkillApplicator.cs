namespace DOL.GS.ModularSkills
{
    /// <summary>
    /// Method by which skill effects are applied.
    /// </summary>
    public interface ISkillApplicator
    {
        /// <summary>
        /// Starts attempt to apply effect to target.
        /// Note that Starts to multiple targets may be made concurrently.
        /// </summary>
        void Start(GameObject target, ModularSkill.SkillComponent sc);

        /// <summary>
        /// function executed when the ISkillApplicator says effect should be
        /// applied to target, eg after LOS check, on tick or when bolt hits.
        /// </summary>
        event ModularSkill.OnSkillAppliedHandler Applied;
    }
}