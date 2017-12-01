using System;
using System.Collections.Generic;
using DOL.GS.ModularSkills.TargetSelector;

namespace DOL.GS.ModularSkills
{
    /// <summary>
    /// Selects a friendly target
    /// </summary>
    public class FriendlyTargetSelector : ITargetSelector, IRadial, IRanged
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

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

        public bool CheckPreconditionsForUse(GameObject target)
        {
            if (Skill == null || Skill.Owner == null)
            {
                Log.Error("null Skill or Skill.Owner.");
                return false;
            }

            if (Range > 0)
            {
                GameLiving user = Skill.Owner;
                int distance = user.GetDistanceTo(target);
                if (distance < 0)
                {
                    // TODO: Change messages like this to instead raise a bunch of events, eg TargetPreconditionsFailedEvent, Reasons.TargetTooFar.
                    // GamePlayer casters can listen to this event and send appropriate messages accordingly.
                    if (user is GamePlayer)
                        ((GamePlayer)user).Out.SendMessage("That target is too far!", PacketHandler.eChatType.CT_SpellResisted, PacketHandler.eChatLoc.CL_SystemWindow);
                    return false;
                }
            }

            return true;
        }

        public ICollection<GameObject> SelectTargets(GameLiving invoker, GameObject target)
        {
            throw new NotImplementedException();
        }
    }
}
