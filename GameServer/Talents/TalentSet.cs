using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using DOL.Util;

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
        }

        private ThreadSafeList<ITalent> m_talents;

        /// <summary>
        /// Adds an ITalent to the TalentSet.
        /// The ITalent may fail to be added if it is already contained in the list.
        /// </summary>
        /// <returns>True if successfully added</returns>
        public virtual bool Add(ITalent talent)
        {
            if (m_talents.Contains(talent))
                return false;
            m_talents.Add(talent);
            return true;
        }

        public virtual bool Remove(ITalent talent)
        {
            return m_talents.Remove(talent);
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
