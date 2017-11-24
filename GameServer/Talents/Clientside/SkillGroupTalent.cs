using System;

namespace DOL.Talents.Clientside
{
    /// <summary>
    /// This talent represents a `skill group', under which other talents can be assembled.
    /// This line does not hold a reference to the collection of ITalents that are assembled underneath it. 
    /// </summary>
    public class SkillGroupTalent : ITalent
    {
        /// <summary>
        /// Creates a new skill group
        /// </summary>
        public SkillGroupTalent(String name)
        {
            Name = name;
        }

        /// <summary>
        /// No implementation required client side for spell line
        /// </summary>
        public ITalentClientImplementation ClientImplementation
        { get { return null; } }

        public virtual void Apply(ITalentOwner owner)
        {
        }

        public virtual bool IsValid(ITalentOwner owner)
        {
            return true;
        }

        public virtual void Remove(ITalentOwner owner)
        {
        }

        /// <summary>
        /// Name of the spell line as displayed to the client.
        /// </summary>
        public string Name
        { get; private set; }
    }
}
