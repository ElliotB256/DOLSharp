namespace DOL.GS.ModularSkills
{
    /// <summary>
    /// The skill can target only a maximum number of targets.
    /// </summary>
    public interface IMaxTargetLimited
    {
        /// <summary>
        /// Number of targets the skill can target
        /// </summary>
        int MaxTargetNumber { get; }

        /// <summary>
        /// Should the skill obey the target number cap
        /// </summary>
        bool IsTargetNumberCapped { get; }
    }
}
