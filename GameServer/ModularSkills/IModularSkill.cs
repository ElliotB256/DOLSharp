using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOL.GS.ModularSkills
{
    public interface IModularSkill
    {
        /// <summary>
        /// Invocation method for the skill
        /// </summary> 
        ISkillInvocation Invocation { get; set; }
        IModularSkillUser Owner { get; }
        /// <summary>
        /// Target selector used for purposes of determining if the skill can be invoked.
        /// </summary>
        ITargetSelector PrimaryTargetSelector { get; }

    }
}
