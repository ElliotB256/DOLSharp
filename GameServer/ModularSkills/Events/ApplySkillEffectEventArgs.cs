namespace DOL.GS.ModularSkills
{
    /// <summary>
    /// A skill effect should be applied to a recipient
    /// </summary>
    public class ApplySkillEffectEventArgs
    {
        public ApplySkillEffectEventArgs(GameObject invoker, GameObject recipient, ISkillEffect effect)
        {
            Invoker = invoker;
            Recipient = recipient;
            Effect = effect;
        }

        public GameObject Invoker { get; private set; }

        public GameObject Recipient { get; private set; }

        public ISkillEffect Effect { get; private set; }
    }
}
