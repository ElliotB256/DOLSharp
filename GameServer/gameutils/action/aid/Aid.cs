namespace DOL.GS
{
    /// <summary>
    /// A beneficial action is attempted on a target
    /// </summary>
    public abstract class Aid : ActionIntention
    {
        public Aid(GameLiving aider) :
            base(aider)
        { }
    }
}
