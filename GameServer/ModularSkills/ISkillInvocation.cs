namespace DOL.GS.ModularSkills
{
    public interface ISkillInvocation
    {
        /// <summary>
        /// Invoker starts to use specified skill
        /// </summary>
        void Start(GameObject invoker);

        /// <summary>
        /// Executed when the skill is invoked
        /// </summary>
        event ModularSkill.InvocationCompleteHandler Completed;
    }
}