namespace DOL.GS.ModularSkills
{
    /// <summary>
    /// A skill user invokes a modular skill.
    /// </summary>
    public class SkillInvokedEventArgs
    {
        public SkillInvokedEventArgs(ModularSkill ms)
        {
            Skill = ms;
        }

        public ModularSkill Skill { get; private set; }
    }
}
