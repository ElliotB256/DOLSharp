namespace DOL.GS.ModularSkills
{
    /// <summary>
    /// Selects a friendly target
    /// </summary>
    public class FriendlyTargetSelector : AOETargetSelector, IRadial, IRanged
    {
        public FriendlyTargetSelector(ModularSkill skill) : base(skill)
        {
        }
        
        /// <summary>
        /// Checks if the target is valid for user.
        /// </summary>
        public override bool IsValidTarget(GameLiving user, GameObject target)
        {
            return GameServer.ServerRules.IsFriendly(user, target);
        }
    }
}
