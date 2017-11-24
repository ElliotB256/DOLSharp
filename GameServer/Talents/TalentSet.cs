using System;
using System.Collections.Generic;

using DOL.Concurrent;

namespace DOL.Talents
{
    /// <summary>
    /// A set of Talents
    /// </summary>
    public class TalentSet : ITalentSet
    {
        public TalentSet(ITalentOwner owner)
        {
            m_talents = new ThreadSafeList<ITalent>();
            m_owner = owner;
        }

        protected ITalentOwner m_owner;
        private ThreadSafeList<ITalent> m_talents;

        /// <summary>
        /// Owner of this TalentSet
        /// </summary>
        public ITalentOwner Owner
        { get { return m_owner; } }

        /// <summary>
        /// Adds an ITalent to the TalentSet.
        /// The ITalent may fail to be added if it is already contained in the list or is not valid for Owner.
        /// </summary>
        /// <returns>True if successfully added</returns>
        public virtual bool Add(ITalent talent)
        {
            if (m_talents.Contains(talent))
                return false;
            if (!talent.IsValid(Owner))
                return false;

            m_talents.Add(talent);
            talent.Apply(Owner);
            return true;
        }

        public virtual bool Remove(ITalent talent)
        {
            if (m_talents.Remove(talent))
            {
                talent.Remove(Owner);
                return true;
            }
            return false;                    
        }

        /// <summary>
        /// Get a list of all talents currently in this set.
        /// </summary>
        /// <returns>A clone of the list that is thread safe to enumerate.<returns>
        public IEnumerable<ITalent> GetAllTalents()
        {
            return m_talents.Clone();
        }
    }
}
