using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOL.GS
{
    /// <summary>
    /// Outcome of an intention performed on a recipient
    /// </summary>
    public abstract class ActionOutcome
    {
        public ActionOutcome(ActionIntention intention)
        {
            Actor = intention.Actor;
            Recipient = intention.Target;
        }

        /// <summary>
        /// Actor who performed the intention
        /// </summary>
        public GameLiving Actor
        { get; private set; }

        public GameLiving Recipient
        { get; private set; }

        /// <summary>
        /// Enacts the outcome of the action
        /// </summary>
        public abstract void Enact();
    }
}
