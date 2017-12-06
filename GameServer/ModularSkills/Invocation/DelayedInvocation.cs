using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DOL.Events;

namespace DOL.GS.ModularSkills
{
    /// <summary>
    /// Skill invoked with a finite non-zero cast time
    /// </summary>
    public abstract class DelayedInvocation : ISkillInvocation, IInvocationDuration, IInterruptible
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public event EventHandler<SkillInvokedEventArgs> Completed;

        public ModularSkill Skill { get; private set; }

        /// <summary>
        /// Duration to invoke the skill, seconds
        /// </summary>
        public float Duration { get; set; }

        /// <summary>
        /// The skill can be interrupted when the invoker is attacked
        /// </summary>
        public bool CanInterruptWhenAttacked { get; set; }

        /// <summary>
        /// The skill can be interrupted when the invoker moves
        /// </summary>
        public bool CanInterruptWhenMoving { get; set; }

        public DelayedInvocation(ModularSkill skill)
        {
            Skill = skill;
            CanInterruptWhenAttacked = true;
            CanInterruptWhenMoving = true;
            Duration = 3f;
        }

        public void Start(GameLiving invoker)
        {
            GameObject target = null;
            if (invoker != null)
                target = invoker.TargetObject;

            if (DelayedAction != null && DelayedAction.IsAlive)
            {
                //interrupt old skill (for now)
                Interrupt(DelayedAction.Invoker);
            }

            ITargetSelector ts = Skill.PrimaryTargetSelector;
            if (!ts.CheckRequirementsForUse(target))
                return;

            DelayedAction = new DelayedInvocationAction(this, invoker, target);
            DelayedAction.Start((int)(Duration * 1000));
            SendCastingAnimation(invoker);

            RegisterHandlers(invoker);
        }

        protected abstract void SendCastingAnimation(GameLiving invoker);

        protected void OnComplete(GameLiving invoker, GameObject target)
        {
            UnregisterHandlers(invoker);
            Completed(this, new SkillInvokedEventArgs(invoker, target));
        }

        /// <summary>
        /// Completes the invocation after a period of time if uninterrupted.
        /// </summary>
        protected class DelayedInvocationAction : RegionAction
        {
            public DelayedInvocationAction(DelayedInvocation di, GameLiving invoker, GameObject target)
                : base(invoker)
            {
                Invoker = invoker;
                Target = target;
                DelayedInvocation = di;
            }

            public GameLiving Invoker { get; private set; }
            public GameObject Target { get; private set; }
            public DelayedInvocation DelayedInvocation { get; private set; }

            protected override void OnTick()
            {
                DelayedInvocation.OnComplete(Invoker, Target);
            }
        }

        /// <summary>
        /// RegionTimerActions that completes the invocation.
        /// </summary>
        protected DelayedInvocationAction DelayedAction { get; private set; }

        protected virtual void RegisterHandlers(GameLiving invoker)
        {
            if (CanInterruptWhenMoving)
                GameEventMgr.AddHandler(invoker, GameLivingEvent.Moving, new DOLEventHandler(OnInterrupted));
            if (CanInterruptWhenAttacked)
                GameEventMgr.AddHandler(invoker, GameLivingEvent.AttackedByEnemy, new DOLEventHandler(OnInterrupted));
        }

        protected virtual void UnregisterHandlers(GameLiving invoker)
        {
            GameEventMgr.RemoveHandler(invoker, GameLivingEvent.Moving, new DOLEventHandler(OnInterrupted));
            GameEventMgr.RemoveHandler(invoker, GameLivingEvent.AttackedByEnemy, new DOLEventHandler(OnInterrupted));
        }

        /// <summary>
        /// The invocation fails because it is interrupted
        /// </summary>
        public void OnInterrupted(DOLEvent e, object sender, EventArgs args)
        {
            GameLiving invoker = sender as GameLiving;
            if (invoker == null)
            {
                Log.Warn("OnInterrupted with null sender.");
                return;
            }

            Interrupt(invoker);
        }

        protected abstract void BroadcastInterruptAnimation(GameLiving invoker);

        public void Interrupt(GameLiving invoker)
        {
            UnregisterHandlers(invoker);
            BroadcastInterruptAnimation(invoker);

            if (DelayedAction != null && DelayedAction.IsAlive)
            {
                DelayedAction.Stop();
                DelayedAction = null;
            }
        }      

    }
}
