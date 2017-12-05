namespace DOL.GS.ModularSkills
{
    public interface ISkillEffect
    {
        /// <summary>
        /// Applies the effect to the recipient.
        /// </summary>
        /// <returns>true if effect is successfuly applied.</returns>
        bool Apply(GameObject recipient);

        /// <summary>
        /// Remove the effect from recipient.
        /// Only invoked for duration effects which expire.
        /// </summary>
        void Expire(GameObject recipient);
    }
}