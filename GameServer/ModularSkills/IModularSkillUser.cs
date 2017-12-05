using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOL.GS.ModularSkills
{
    /// <summary>
    /// A user of modular skills
    /// </summary>
    public interface IModularSkillUser
    {
        GameObject TargetObject { get; }

        IModularSkillEventHandlers ModularSkillEventHandlers { get; }
    }

    /// <summary>
    /// Helper class for holding event handlers relating to modular skills
    /// </summary>
    public interface IModularSkillEventHandlers
    {
        /// <summary>
        /// The skill user does not fulfill the conditions to use the skill.
        /// </summary>
        event EventHandler<FailSkillTargetRequirementsEventArgs> FailSkillTargetRequirements;
        //void OnFailSkillTargetRequirements(FailSkillTargetRequirementsEventArgs args);

        /// <summary>
        /// The skill user begins to invoke a skill with a finite deployment time.
        /// </summary>
        event EventHandler<InvokingSkillEventArgs> StartInvoking;
        //void OnStartInvoking(InvokingSkillEventArgs args);

        /// <summary>
        /// The skill user invokes a skill
        /// </summary>
        event EventHandler<SkillInvokedEventArgs> SkillInvoked;
        //void OnSkillInvoked(SkillInvokedEventArgs args);
    }
}
