namespace DOL.GS
{
    /// <summary>
    /// A beneficial action is attempted on a target
    /// </summary>
    public abstract class AidOutcome : ActionOutcome
    {
        public AidOutcome(Aid original) : base(original)
        {
        }
    }
}
