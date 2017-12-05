using System;

namespace DOL.GS.ModularSkills
{
    public class HealEffect : ISkillEffect, IEfficacious
    {
        public int Value { get; private set; }
        public int Effectiveness { get; private set; }

        int CalculateHeal() => Value * Effectiveness;

        public bool Apply(GameObject recipient)
        {
            recipient.Health += CalculateHeal();

            // TODO: broadcast heal spell effect. The effect used should be chosen according to the percentage of health healed.

            return true;
        }

        public void Expire(GameObject recipient)
        {
            // Heals are instant.
        }
    }
}
