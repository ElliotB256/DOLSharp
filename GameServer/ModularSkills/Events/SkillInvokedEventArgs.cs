namespace DOL.GS.ModularSkills
{
    /// <summary>
    /// A skill user invokes a modular skill.
    /// </summary>
    public class SkillInvokedEventArgs
    {
        public SkillInvokedEventArgs(GameObject invoker, GameObject target)
        {
            Target = target;
            Invoker = invoker;
        }

        public GameObject Target { get; private set; }

        public GameObject Invoker { get; private set; }
    }
}
