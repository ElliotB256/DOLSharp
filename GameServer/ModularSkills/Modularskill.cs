using System;
using System.Collections.Generic;

namespace DOL.GS.ModularSkills
{
    /// <summary>
    /// Represents a skill composed of different modules
    /// </summary>
    public class ModularSkill : IModularSkill
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private ISkillInvocation m_invocation;

        /// <summary>
        /// Invocation method for the skill
        /// </summary> 
        public ISkillInvocation Invocation
        {
            get { return m_invocation; }
            set
            {
                if (m_invocation != null)
                    m_invocation.Completed -= OnInvoked;
                m_invocation = value;
                m_invocation.Completed += OnInvoked;
            }
        }

        /// <summary>
        /// Target selector used for purposes of determining if the skill can be invoked.
        /// </summary>
        public ITargetSelector PrimaryTargetSelector
        {
            get
            {
                IList<SkillComponent> comps = Components;
                if (comps.Count > 1)
                    return comps[0].TargetSelector;
                return null;
            }
        }

        public IList<SkillComponent> Components { get; set; }

        public IModularSkillUser Owner { get; set; }

        /// <summary>
        /// Owner of the skill tries to use it
        /// </summary>
        public void TryUse()
        {
            if (Invocation == null)
            {
                Log.Error(string.Format("Could not start skill - null Invocation. skill Owner={0}", Owner));
                return;
            }
            
            Invocation.Start(Owner);
        }

        /// <summary>
        /// Invoked when skill has successfully been cast.
        /// </summary>
        protected void OnInvoked(GameObject target)
        {
            foreach (SkillComponent sc in Components)
            {
                if (sc == null)
                {
                    Log.Error(string.Format("SkillComponent was null in OnInvoked. Owner={0}", Owner));
                    continue;
                }

                ICollection<GameObject> targets = sc.TargetSelector.SelectTargets(Owner, target);

                foreach (GameObject o in targets)
                {
                    sc.Applicator.Start(o, sc);
                }
            }
        }

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