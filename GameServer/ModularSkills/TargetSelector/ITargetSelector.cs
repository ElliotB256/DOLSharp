using System.Collections.Generic;

namespace DOL.GS.ModularSkills
{
    public interface ITargetSelector
    {
        /// <summary>
        /// Gather a list of eligible targets for the skill.
        /// SelectTargets should assume that all checks between invoker and target have already been performed (eg LOS, distance, valid target etc)
        /// </summary>
        ICollection<GameObject> SelectTargets(IModularSkillUser invoker, GameObject target);

        /// <summary>
        /// Checks whether a skill can be used against the target object.
        /// Raises CheckPreconditionsFailedEvent if the skill cannot be used.
        /// </summary>
        /// <param name="target"></param>
        /// <returns>true if skill can be used</returns>
        bool CheckRequirementsForUse(GameObject target);
    }
}
