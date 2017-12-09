using System;
using System.Collections.Generic;
using System.Linq;

namespace DOL.GS.ModularSkills
{
    /// <summary>
    /// Selects targets with support for radial area of effect around chosen target.
    /// </summary>
    public abstract class AOETargetSelector : ITargetSelector, IRadial, IRanged
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public event EventHandler<FailSkillUseEventArgs> FailSkillTargetRequirements;

        public AOETargetSelector(ModularSkill skill)
        {
            Skill = skill;
        }

        public ModularSkill Skill
        { get; private set; }

        /// <summary>
        /// Selects targets within radius
        /// </summary>
        public float Radius
        { get; set; }

        /// <summary>
        /// Range for valid selection
        /// </summary>
        public float Range
        { get; set; }

        public bool CheckRequirementsForUse(GameObject target)
        {
            if (Skill == null || Skill.Owner == null)
            {
                Log.Error("null Skill or Skill.Owner.");
                return false;
            }

            GameLiving oLiving = Skill.Owner as GameLiving;
            if (oLiving == null)
            {
                Log.Warn("Cannot determine preconditions for non-GameLiving Owner.");
                return false;
            }

            if (target == null)
                target = oLiving;

            // If range = 0, then the skill is always centered around the caster
            if (Range == 0)
                target = oLiving;
            else if (!IsValidTarget(oLiving, target))
            {
                FailSkillTargetRequirements(this,
                    new FailSkillUseEventArgs(FailSkillUseReason.InvalidTarget));
                return false;
            }

            if (Range > 0)
            {
                int distance = oLiving.GetDistanceTo(target);
                if (distance > Range)
                {
                    FailSkillTargetRequirements(this,
                        new FailSkillUseEventArgs(FailSkillUseReason.TargetTooFar));
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Checks if the target is valid for user.
        /// </summary>
        public virtual bool IsValidTarget(GameLiving user, GameObject target)
        {
            throw new NotImplementedException();
        }

        public ICollection<GameObject> SelectTargets(GameLiving invoker, GameObject target)
        {
            List<GameObject> list = new List<GameObject>();
            List<GameObject> potentials = new List<GameObject>();

            if (target == null)
                target = invoker;

            //If range is 0 use invoker to pick targets.
            if (Range == 0)
                target = invoker;

            if (Radius > 0)
            {
                if (target != null && invoker.GetDistanceTo(target) < Radius)
                {
                    //potentials.Add(target);
                    potentials.AddRange(target.GetNPCsInRadius((ushort)Radius, false).OfType<GameLiving>());
                    potentials.AddRange(target.GetPlayersInRadius((ushort)Radius, false).OfType<GameLiving>());
                }
            }
            else
            {
                if (invoker.GetDistanceTo(target) < Range)
                    potentials.Add(target);
            }


            list.AddRange(potentials.Where(o => IsValidTarget(invoker, o)));

            //todo: MaxTargetLimited. Select a number of entries at random from list into a new list. Guarantee target is returned if IsValidTarget(target).

            return list;
        }
    }
}
