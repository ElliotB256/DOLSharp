using System;
using System.Collections.Generic;

namespace DOL.Talents
{
    /// <summary>
    /// Represents a collection of ITalents owned by an ITalentOwner.
    /// </summary>
    public interface ITalentSet
    {
        /// <summary>
        /// Adds an ITalent to the ITalentSet
        /// </summary>
        /// <returns>true if added successfully</returns>
        bool Add(ITalent talent);

        /// <summary>
        /// Removes an ITalent from the ITalentSet
        /// </summary>
        bool Remove(ITalent talent);

        /// <summary>
        /// Returns a list containing all ITalent objects in the ITalentSet.
        /// </summary>
        /// <returns></returns>
        IEnumerable<ITalent> GetAllTalents();

        /// <summary>
        /// Owner of the ITalentSet
        /// </summary>
        ITalentOwner Owner { get; }

        /// <summary>
        /// A talent was added to the set
        /// </summary>
        event EventHandler<TalentAddedEventArgs> TalentAdded;

        /// <summary>
        /// A talent was removed from the set
        /// </summary>
        event EventHandler<TalentRemovedEventArgs> TalentRemoved;
    }
}
