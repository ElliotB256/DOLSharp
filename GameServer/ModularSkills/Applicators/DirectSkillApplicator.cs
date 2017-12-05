using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOL.GS.ModularSkills
{
    public class DirectSkillApplicator : ISkillApplicator
    {
        public event SkillAppliedHandler<GameObject> Applied;

        public void Start(GameObject target, SkillComponent sc)
        {
            // TODO: Add LOS check. Needs second GameObject reference.

            var eventhandler = Applied;
            eventhandler?.Invoke(target);
        }
    }
}
