namespace DOL.GS
{
    public class HealingAidOutcome : AidOutcome
    {
        public HealingAidOutcome(HealingAid aid)
            :base(aid)
        {
            Health = aid.Health;
        }

        /// <summary>
        /// The amount of health healed
        /// </summary>
        public int Health { get; private set; }

        public override void Enact()
        {
            Recipient.ChangeHealth(Health);
        }
    }
}
