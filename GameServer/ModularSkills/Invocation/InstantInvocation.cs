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
    public class InstantInvocation : ISkillInvocation
    {
        public event EventHandler<SkillInvokedEventArgs> Completed;

        public ModularSkill Skill { get; private set; }

        public InstantInvocation(ModularSkill skill)
        {
            Skill = skill;
        }

        public void Start(GameLiving invoker)
        {
            GameObject target = null;
            if (invoker != null)
                target = invoker.TargetObject;

            ITargetSelector ts = Skill.PrimaryTargetSelector;
            if (!ts.CheckRequirementsForUse(target))
                return;

            //Invocation successful
            Completed(this, new SkillInvokedEventArgs(invoker, target));
        }
    }
}
