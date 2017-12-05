namespace DOL.GS.ModularSkills
{
    public interface ISkillInvocation
    {
        /// <summary>
        /// Invoker starts to use specified skill
        /// </summary>
        void Start(IModularSkillUser invoker);

        /// <summary>
        /// Executed when the skill is successfully invoked upon target gameobject
        /// </summary>
        event SkillInvocationHandler<GameObject> Completed;
    }

    public delegate void SkillInvocationHandler<TTarget>(TTarget target) where TTarget : GameObject;
}