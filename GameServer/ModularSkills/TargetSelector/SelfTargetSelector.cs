using System;
using System.Collections.Generic;

namespace DOL.GS.ModularSkills
{
    /// <summary>
    /// Selects user of skill
    /// </summary>
    public class SelfTargetSelector : ITargetSelector
    { 
        public SelfTargetSelector()
        {
        }

        public event EventHandler<FailSkillUseEventArgs> FailSkillTargetRequirements;

        public bool CheckRequirementsForUse(GameObject target)
        {
            return true;
        }

        public ICollection<GameObject> SelectTargets(GameLiving invoker, GameObject target)
        {
            ICollection<GameObject> list = new List<GameObject>();
            if (invoker != null)
                list.Add(invoker);
            return list;
        }
    }
}
