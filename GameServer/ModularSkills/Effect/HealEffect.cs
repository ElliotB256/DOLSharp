using System;
using System.Linq;

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
            foreach (GamePlayer p in recipient.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE).OfType<GamePlayer>())
            {
                p.Out.SendSpellEffectAnimation(recipient, recipient, 2065, 0, false, 1);
            }

            return true;
        }

        public void BroadcastEffectAnimation(GameObject recipient)
        {

        }

        public void Expire(GameObject recipient)
        {
            // Heals are instant.
        }
    }
}
