using System;
using DOL.Talents.Clientside;

namespace DOL.Talents
{
    /// <summary>
    /// Base class for passive talents.
    /// </summary>
    public abstract class PassiveTalent : ITalent
    {
        protected ClientAbilityImplementation m_clientsideAbility;

        public PassiveTalent(String name)
        {
            m_clientsideAbility = new ClientAbilityImplementation();
            m_clientsideAbility.Name = name;
        }

        /// <summary>
        /// Client side implementation of the passive talent
        /// </summary>
        public ITalentClientImplementation ClientImplementation
        {
            get { return m_clientsideAbility; }
        }

        public virtual void Apply(ITalentOwner owner)
        {
            throw new NotImplementedException();
        }

        public virtual bool IsValid(ITalentOwner owner)
        {
            throw new NotImplementedException();
        }

        public virtual void Remove(ITalentOwner owner)
        {
            throw new NotImplementedException();
        }
    }
}
