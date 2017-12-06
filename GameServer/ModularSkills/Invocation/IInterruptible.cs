namespace DOL.GS.ModularSkills
{
    /// <summary>
    /// An invocation method which can be configured to be interrupted
    /// </summary>
    public interface IInterruptible
    {
        /// <summary>
        /// The skill can be interrupted when the invoker is attacked
        /// </summary>
        bool CanInterruptWhenAttacked { get; }

        /// <summary>
        /// The skill can be interrupted when the invoker moves
        /// </summary>
        bool CanInterruptWhenMoving { get; }
    }
}
