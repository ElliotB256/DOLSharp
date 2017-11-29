using System.Collections.Generic;

namespace DOL.GS.ModularSkills
{
    /// <summary>
    /// Represents a skill composed of different modules
    /// </summary>
    public class ModularSkill
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Invocation method for the skill
        /// </summary> 
        public ISkillInvocation Invocation { get; set; }

        //Setter for Invocation should set IModularSkill Parent element to this, and register OnComplete delegate

        /// <summary>
        /// A component of a modular skill
        /// </summary>
        public class SkillComponent
        {
            /// <summary>
            /// Target selection method for the skill component
            /// </summary>
            public ITargetSelector TargetSelector { get; set; }

            /// <summary>
            /// Method by which effects are applied to target.
            /// </summary>
            public ISkillApplicator Applicator { get; set; }

            /// <summary>
            /// List of effects to apply to targets.
            /// Subsequent effects are applied if the preceding effect 'Apply'
            /// returns true.
            /// </summary>
            public ICollection<ISkillEffect> SkillEffectChain { get; set; }
        }

        ICollection<SkillComponent> Components { get; set; }

        public GameLiving Owner { get; set; }

        /// <summary>
        /// Owner of the skill tries to use it
        /// </summary>
        public void TryUse()
        {
            if (Invocation == null)
            {
                Log.Error(string.Format("Could not start skill - null Invocation. skill Owner={0}", Owner));
            }

            Invocation.Start(Owner.TargetObject);
        }

        /// <summary>
        /// Invoked when skill has successfully been cast.
        /// </summary>
        protected void OnInvoked(GameObject target)
        {
            foreach (SkillComponent sc in Components)
            {
                if (sc == null)
                    continue;

                ICollection<GameObject> targets = sc.TargetSelector.SelectTargets(Owner, target);

                foreach (GameObject o in targets)
                {
                    sc.Applicator.Start(o, sc);
                }
            }
        }
        public delegate void InvocationCompleteHandler(GameObject target);

        /// <summary>
        /// Invoked when skill effect chain is to be applied to target.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="sc"></param>
        protected void OnApplied(GameObject target, SkillComponent sc)
        {
            foreach (ISkillEffect effect in sc.SkillEffectChain)
            {
                if (effect == null)
                    continue;
                if (!effect.Apply(target))
                    break;
            }
        }
        public delegate void OnSkillAppliedHandler(GameObject target, SkillComponent sc);
    }
}