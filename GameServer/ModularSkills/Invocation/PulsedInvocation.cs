using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DOL.Events;

namespace DOL.GS.ModularSkills
{
    /// <summary>
    /// Skill invoked each time the spell pulses
    /// </summary>
    public class PulsedInvocation : ISkillInvocation, IFrequency
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public event EventHandler<SkillInvokedEventArgs> Completed;
        public event EventHandler<FailSkillUseEventArgs> FailInvocation;

        public ModularSkill Skill { get; private set; }

        public PulsedInvocation(ModularSkill skill)
        {
            Skill = skill;
            Frequency = 1 / 5f;
        }

        public void Start(GameLiving invoker)
        {
            GameObject target = null;
            if (invoker != null)
                target = invoker.TargetObject;

            //If we are already pulsing, using the skill cancels it.
            if (IsPulsing())
            {
                StopPulsing();
                return;
            }
            else
            {
                StartPulsing(invoker);
            }
        }

        protected RegionTimerAction<GameLiving> m_pulseTimer;

        /// <summary>
        /// Starts the invocation pulsing from target invoker
        /// </summary>
        /// <param name="invoker"></param>
        protected void StartPulsing(GameLiving invoker)
        {
            m_pulseTimer = new RegionTimerAction<GameLiving>(invoker, p => OnPulse(p));
            m_pulseTimer.Interval = GetInterval(invoker);
            m_pulseTimer.Start(m_pulseTimer.Interval);
        }

        /// <summary>
        /// Stops the invocation from pulsing
        /// </summary>
        /// <param name="invoker"></param>
        protected void StopPulsing()
        {
            m_pulseTimer?.Stop();
        }

        protected int OnPulse(GameLiving invoker)
        {
            Completed(this, new SkillInvokedEventArgs(invoker, invoker.TargetObject));
            return GetInterval(invoker);
        }

        /// <summary>
        /// Is the invocation currently pulsing?
        /// </summary>
        public bool IsPulsing() {
            var pt = m_pulseTimer;
            return pt != null && pt.IsAlive;
        }

        public float Frequency
        { get; protected set; }

        /// <summary>
        /// Get interval between ticks
        /// </summary>
        /// <returns></returns>
        protected int GetInterval(GameLiving invoker)
        {
            return (int)(1000 / Frequency);
        }

        public virtual void HandleUseOtherSkill(object sender, TryUsingSkillEventArgs e)
        {
            // If other skill is a pulsing skill, cancel current skill. Can only have one pulsing skill active at a time!
            var pulseI = e.Skill.Invocation as PulsedInvocation;
            if (pulseI != null && IsPulsing() && pulseI != this)
            {
                StopPulsing();
            }
        }
    }
}
