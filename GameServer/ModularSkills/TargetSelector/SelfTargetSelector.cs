using System;
using System.Collections.Generic;

namespace DOL.GS.ModularSkills
{
    /// <summary>
    /// Selects user of skill
    /// </summary>
    public abstract class SelfTargetSelector : ITargetSelector
    {
        public ModularSkill Skill { get; private set; }

        public SelfTargetSelector(ModularSkill ms)
        {
            Skill = ms;
        }

        public bool CheckPreconditionsForUse(GameObject target)
        {
            return true;
        }

        public ICollection<GameObject> SelectTargets(GameLiving invoker, GameObject target)
        {
            ICollection<GameObject> list = new List<GameObject>();
            list.Add(Skill.Owner);
            return list;
        }
    }
}
