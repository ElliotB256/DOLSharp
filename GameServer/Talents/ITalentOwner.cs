namespace DOL.Talents
{
    /// <summary>
    /// Something that owns a talent.
    /// </summary>
    public interface ITalentOwner
    {
        ITalentSet Talents { get; }
    }
}
