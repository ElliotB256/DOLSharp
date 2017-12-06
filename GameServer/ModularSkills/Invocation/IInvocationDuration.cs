namespace DOL.GS.ModularSkills
{
    /// <summary>
    /// Something that takes a finite time to invoke
    /// </summary>
    public interface IInvocationDuration
    {
        /// <summary>
        /// Base duration to invoke the skill
        /// </summary>
        float Duration { get; }
    }
}
