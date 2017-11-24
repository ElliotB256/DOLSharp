namespace DOL.Talents
{
    /// <summary>
    /// A talent represents any passive or active trait of the character.
    /// This may be the ability to make a melee attack, to take less fall
    /// damage, a passive bonus to constitution, a magic spell, etc etc...
    /// </summary>
    public interface ITalent
    {
        /// <summary>
        /// The talent is added to the object
        /// </summary>
        void Apply(ITalentOwner owner);

        /// <summary>
        /// The talent is removed from the object
        /// </summary>
        void Remove(ITalentOwner owner);

        /// <summary>
        /// Is the specified owner a suitable candidate for the ITalent
        /// </summary>
        /// <param name="owner"></param>
        bool IsValid(ITalentOwner owner);

        /// <summary>
        /// Client-side implementation and display of the talent.
        /// </summary>
        ITalentClientImplementation ClientImplementation { get; }
    }
}
