using System;

namespace DOL.GS.ModularSkills
{
    public interface ISkillInvocation
    {
        /// <summary>
        /// Invoker starts to use specified skill
        /// </summary>
        void Start(GameLiving invoker);

        /// <summary>
        /// Executed when the skill is successfully invoked upon target gameobject
        /// </summary>
        event EventHandler<SkillInvokedEventArgs> Completed;
    }
}