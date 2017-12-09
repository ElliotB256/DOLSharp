namespace DOL.GS.ModularSkills
{
    /// <summary>
    /// Target selector has a maximum range for target selection
    /// </summary>
    public interface IRanged
    {
        /// <summary>
        /// Range targets must be within to be valid
        /// </summary>
        float Range { get; set; }
    }
}