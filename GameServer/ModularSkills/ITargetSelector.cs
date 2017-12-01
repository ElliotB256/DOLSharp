using System.Collections.Generic;

namespace DOL.GS.ModularSkills
{
    public interface ITargetSelector
    {
        /// <summary>
        /// Gather a list of eligible targets for the skill
        /// </summary>
        ICollection<GameObject> SelectTargets(GameLiving invoker, GameObject target);

        /// <summary>
        /// Checks whether a skill can be used against the target object
        /// </summary>
        /// <param name="target"></param>
        /// <returns>true if skill can be used</returns>
        bool CheckPreconditionsForUse(GameObject target)
    }
}
