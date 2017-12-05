using System;
using System.Collections.Generic;
using System.Linq;
using DOL.GS.ModularSkills.TargetSelector;
using DOL.GS;

namespace DOL.GS.ModularSkills
{
    /// <summary>
    /// Selects a friendly target
    /// </summary>
    public class FriendlyTargetSelector : ITargetSelector, IRadial, IRanged
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public event EventHandler<FailSkillTargetRequirementsEventArgs> FailSkillTargetRequirements;

        public FriendlyTargetSelector(ModularSkill skill)
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

            if (!IsValidTarget(oLiving, target))
            {
                FailSkillTargetRequirements(this,
                    new FailSkillTargetRequirementsEventArgs(FailSkillTargetRequirementsEventArgs.eReason.InvalidTarget));
                return false;
            }

            if (Range > 0)
            {
                int distance = oLiving.GetDistanceTo(target);
                if (distance > Range)
                {
                    FailSkillTargetRequirements(this,
                        new FailSkillTargetRequirementsEventArgs(FailSkillTargetRequirementsEventArgs.eReason.TargetTooFar));
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Checks if the target is valid for user.
        /// </summary>
        public bool IsValidTarget(GameLiving user, GameObject target)
        {
            return GameServer.ServerRules.IsFriendly(user, target);
        }

        public ICollection<GameObject> SelectTargets(IModularSkillUser invoker, GameObject target)
        {
            List<GameObject> list = new List<GameObject>();
            List<GameObject> potentials = new List<GameObject>();

            if (Radius > 0)
            {
                potentials.AddRange(target.GetNPCsInRadius((ushort)Radius, false).OfType<GameLiving>());
                potentials.AddRange(target.GetPlayersInRadius((ushort)Radius, false).OfType<GameLiving>());
            }

            return list;
        }
    }
}
