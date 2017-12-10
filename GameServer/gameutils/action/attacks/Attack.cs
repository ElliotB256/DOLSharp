namespace DOL.GS
{
    /// <summary>
    /// Represents the intention for an Attacker to attack a Target.
    /// </summary>
    public class Attack : ActionIntention
    {
        public Attack(GameLiving attacker) : base(attacker) { }

        /// <summary>
        /// Determine the outcome of the attack.
        /// </summary>
        /// <returns></returns>
        public override ActionOutcome DetermineResult()
        {
            return new AttackOutcome(this);
        }
    }
}
