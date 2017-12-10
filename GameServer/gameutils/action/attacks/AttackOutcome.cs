namespace DOL.GS
{
    /// <summary>
    /// The outcome/result of an attack
    /// </summary>
    public class AttackOutcome : ActionOutcome
    {
        public AttackOutcome(Attack original) : base(original)
        {
        }

        public override void Enact()
        {
        }
    }
}
