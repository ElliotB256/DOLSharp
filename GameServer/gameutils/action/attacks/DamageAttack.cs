namespace DOL.GS
{
    /// <summary>
    /// Represents the intention for an Attacker to attack a Target, which results in target damage.
    /// </summary>
    public class DamageAttack : Attack
    {
        public DamageAttack(GameLiving attacker) : base(attacker)
        {
            Damage = 0;
            DamageType = eDamageType.Natural;
        }

        public DamageAttack(GameLiving attacker, int damage, eDamageType damageType) : base(attacker)
        {
            Damage = damage;
            DamageType = damageType;
        }

        /// <summary>
        /// Damage of the attack
        /// </summary>
        public virtual int Damage { get; set; }

        public virtual eDamageType DamageType { get; set; }

        /// <summary>
        /// Determine the outcome of the attack.
        /// </summary>
        /// <returns></returns>
        public override ActionOutcome DetermineResult()
        {
            return new DamageAttackOutcome(this);
        }
    }
}
