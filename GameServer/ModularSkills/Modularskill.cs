﻿using System;
using System.Collections.Generic;

namespace DOL.GS.ModularSkills
{
    /// <summary>
    /// Represents a skill composed of different modules
    /// </summary>
    public class ModularSkill
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public ModularSkill()
        {
            Components = new List<SkillComponent>();
        }

        /// <summary>
        /// Target selector used for purposes of determining if the skill can be invoked.
        /// </summary>
        public ITargetSelector PrimaryTargetSelector
        {
            get
            {
                IList<SkillComponent> comps = Components;
                if (comps.Count > 0)
                    return comps[0].TargetSelector;
                return null;
            }
        }

        public IList<SkillComponent> Components { get; private set; }

        public GameLiving Owner { get; set; }

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

            // Connect up required event chains
            Components.ForEach(sc => sc.Applicator.Applied += OnApplicatorApplied);
            Log.Debug(string.Format("TryUse() of ModularSkill, user={0}", Owner));
            Invocation.Start(Owner);
        }


        #region Invocation

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
        /// The Modular skill is successfully invoked.
        /// </summary>
        public event EventHandler<SkillInvokedEventArgs> Invoked;

        /// <summary>
        /// The Modular skill could not be invoked because target requirements were not met
        /// </summary>
        public event EventHandler<FailSkillTargetRequirementsEventArgs> FailSkillTargetRequirement;

        /// <summary>
        /// Invoked when skill has successfully been cast.
        /// </summary>
        protected void OnInvoked(object sender, SkillInvokedEventArgs e)
        {
            GameObject target = e.Target;
            GameObject invoker = e.Invoker;

            Log.Debug("Invocation of a modular skill has completed.");

            foreach (SkillComponent sc in Components)
            {
                if (sc == null)
                {
                    Log.Error(string.Format("SkillComponent was null in OnInvoked. Invoker={0}", Owner));
                    continue;
                }

                ICollection<GameObject> targets = sc.TargetSelector.SelectTargets(Owner, target);                

                foreach (GameObject o in targets)
                {
                    Log.Debug(string.Format("Start to apply Applicator to target={0}", o));
                    sc.Applicator.Start(invoker, o, sc);
                }
            }
            var eh = Invoked;
            eh?.Invoke(this, e);
        }

        #endregion

        /// <summary>
        /// The skill applicator has been applied to target.
        /// Now it is time to apply the SkillComponent's SkillEffectChain
        /// </summary>
        /// <param name="target"></param>
        /// <param name="sc"></param>
        protected void OnApplicatorApplied(object sender, SkillApplicatorAppliedEventArgs e)
        {
            Log.Debug(string.Format("ISkillApplicator applied! Applying skill effect chain to recipient={0}.", e.Recipient));
            foreach (ISkillEffect effect in e.SkillComponent.SkillEffectChain)
            {
                if (effect == null)
                    continue;
                if (!effect.Apply(e.Recipient))
                    break;
            }
        }
    }
}