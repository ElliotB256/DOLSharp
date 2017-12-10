using System;

namespace DOL.GS
{
    /// <summary>
    /// The outcome/result of an attack
    /// </summary>
    public class AttackOutcome : EventArgs
    {
        public AttackOutcome(Attack original)
        {
            Originator = original;
        }

        /// <summary>
        /// The attack that lead to this outcome
        /// </summary>
        public Attack Originator { get; private set; }

        /// <summary>
        /// Living that performs the attack
        /// </summary>
        public GameLiving Attacker { get; set; }

        /// <summary>
        /// Recipient of the attack.
        /// </summary>
        public GameLiving Recipient { get; set; }

    }
}
