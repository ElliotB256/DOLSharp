using System;

namespace DOL.GS
{
    /// <summary>
    /// Represents the intention for an Attacker to attack a Target.
    /// </summary>
    public class Attack : EventArgs
    {
        //Automatically set when the living attacks.
        /// <summary>
        /// Living that performs the attack
        /// </summary>
        public GameLiving Attacker { get; set; }
        /// <summary>
        /// Intended recipient of the attack.
        /// </summary>
        public GameLiving Target { get; set; }

        /// <summary>
        /// Determine the outcome of the attack.
        /// </summary>
        /// <returns></returns>
        public virtual AttackOutcome DetermineResult()
        {
            var outcome = new AttackOutcome(this);
            outcome.Attacker = Attacker;
            outcome.Recipient = Target;
            return outcome;
        }
    }
}
