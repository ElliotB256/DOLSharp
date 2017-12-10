namespace DOL.GS
{
    /// <summary>
    /// The outcome/result of an attack
    /// </summary>
    public class DamageAttackOutcome : AttackOutcome
    {
        public DamageAttackOutcome(DamageAttack original) : base(original)
        {
            Damage = original.Damage;
            DamageType = original.DamageType;
        }

        /// <summary>
        /// Get the damage from the attack
        /// </summary>
        public virtual int Damage { get; set; }

        /// <summary>
        /// Damage type of the attack
        /// </summary>
        public eDamageType DamageType { get; set; }

        public override void Enact()
        {
            base.Enact();
            Recipient.ChangeHealth(-Damage);
        }
    }
}
