using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOL.GS.ModularSkills
{
    public class DirectSkillApplicator : ISkillApplicator
    {
        public event EventHandler<SkillApplicatorAppliedEventArgs> Applied;

        public void Start(GameObject invoker, GameObject target, SkillComponent sc)
        {
            // TODO: Add LOS check. None for self target!

            var eventhandler = Applied;
            eventhandler?.Invoke(this, new SkillApplicatorAppliedEventArgs(invoker, target, sc));
        }
    }
}
