using System;
using System.Linq;

namespace DOL.GS.ModularSkills
{
    public class GesturedInvocation : DelayedInvocation
    {
        public GesturedInvocation(ModularSkill skill) : base(skill)
        {
        }

        public ushort SpellAnimation { get; set; }

        protected override void BroadcastInterruptAnimation(GameLiving invoker)
        {
            invoker.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE).OfType<GamePlayer>()
                .ForEach(p => p.Out.SendInterruptAnimation(invoker));
        }

        protected override void SendCastingAnimation(GameLiving invoker)
        {
            ushort gameTime = (ushort)(Duration * 10); // client units
            invoker.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE).OfType<GamePlayer>()
                .ForEach(p => p.Out.SendSpellCastAnimation(invoker, SpellAnimation, gameTime));
        }

        public override void HandleUseOtherSkill(object sender, TryUsingSkillEventArgs e)
        {
            base.HandleUseOtherSkill(sender, e);

            if (IsBeingInvoked() && e.Skill.Invocation is GesturedInvocation)
                e.Stop(FailSkillUseReason.AlreadyUsingAnotherSkill);
        }
    }
}
