namespace DOL.GS.ModularSkills
{
    /// <summary>
    /// A skill applicator is applied to the recipient
    /// </summary>
    public class SkillApplicatorAppliedEventArgs
    {
        public SkillApplicatorAppliedEventArgs(GameObject invoker, GameObject recipient, SkillComponent sc)
        {
            Invoker = invoker;
            Recipient = recipient;
            SkillComponent = sc;
        }

        public GameObject Invoker { get; private set; }

        public GameObject Recipient { get; private set; }

        public SkillComponent SkillComponent { get; private set; }
    }
}
