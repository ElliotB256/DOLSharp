namespace DOL.GS
{
    /// <summary>
    /// Represents an intention that something plans to do to a target.
    /// </summary>
    public abstract class ActionIntention
    {
        public ActionIntention(GameLiving actor)
        {
            Actor = actor;
        }

        /// <summary>
        /// Target of the intended action
        /// </summary>
        public GameLiving Target { get; set; }

        /// <summary>
        /// Do-er planning the intention
        /// </summary>
        public GameLiving Actor { get; private set; }

        /// <summary>
        /// Determine the outcome of the action
        /// </summary>
        /// <returns></returns>
        public abstract ActionOutcome DetermineResult();
    }
}
