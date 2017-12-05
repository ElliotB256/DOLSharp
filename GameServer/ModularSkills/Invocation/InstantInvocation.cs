using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOL.GS.ModularSkills
{
    /// <summary>
    /// Checks various prerequisites of the skill to be triggered
    /// </summary>
    public abstract class InstantInvocation : ISkillInvocation
    {
        public event SkillInvocationHandler<GameObject> Completed;

        public ModularSkill Skill { get; private set; }

        public InstantInvocation(ModularSkill skill)
        {
            Skill = skill;
        }

        public void Start(IModularSkillUser invoker)
        {
            GameLiving living = invoker as GameLiving;
            GameObject target = null;
            if (living != null)
                target = living.TargetObject;

            ITargetSelector ts = Skill.PrimaryTargetSelector;
            if (!ts.CheckRequirementsForUse(target))
                return;

            //Invocation successful
            invoker.ModularSkillEventHandlers.OnSkillInvoked(new SkillInvokedEventArgs(Skill));
            Completed(target);
        }
    }
}
