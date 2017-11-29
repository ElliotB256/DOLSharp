using System.Collections.Generic;

namespace DOL.GS.ModularSkills
{
    public interface ITargetSelector
    {
        /// <summary>
        /// Gather a list of eligible targets for the skill
        /// </summary>
        ICollection<GameObject> SelectTargets(GameLiving invoker, GameObject target);
    }
}
