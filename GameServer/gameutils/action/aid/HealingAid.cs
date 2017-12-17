namespace DOL.GS
{
    public class HealingAid : Aid
    {
        public HealingAid(GameLiving actor, eHealingType type)
            : base(actor) { }

        /// <summary>
        /// The amount of health to heal
        /// </summary>
        public int Health { get; set; }

        public eHealingType HealingType
        { get; set; }

        public override ActionOutcome DetermineResult()
        {
            return new HealingAidOutcome(this);
        }
    }

    public enum eHealingType
    {
        Regeneration,
        Magical,
        Potion,
    }
}
