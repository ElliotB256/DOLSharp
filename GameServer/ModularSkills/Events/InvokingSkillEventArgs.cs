using System;

namespace DOL.GS.ModularSkills
{
    /// <summary>
    /// A skill user begins to use a skill with a finite time to deploy.
    /// Instant-use skills will instead directly fire the Invoked event.
    /// </summary>
    public class InvokingSkillEventArgs : EventArgs
    {
        public InvokingSkillEventArgs(GameObject target)
        {
            Target = target;
        }

        public GameObject Target { get; private set; }
    }
}
