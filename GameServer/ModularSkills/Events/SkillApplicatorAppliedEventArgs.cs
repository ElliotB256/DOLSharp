namespace DOL.GS.ModularSkills
{
    /// <summary>
    /// A skill applicator is applied to the recipient
    /// </summary>
    public class SkillApplicatorAppliedEventArgs
    {
        public SkillApplicatorAppliedEventArgs(GameObject recipient, SkillComponent sc)
        {
            Recipient = recipient;
            SkillComponent = sc;
        }

        public GameObject Recipient { get; private set; }

        public SkillComponent SkillComponent { get; private set; }
    }
}
