namespace DOL.GS.ModularSkills
{
    /// <summary>
    /// Selects a friendly target
    /// </summary>
    public class EnemyTargetSelector : AOETargetSelector, IRadial, IRanged
    {
        public EnemyTargetSelector(ModularSkill skill) : base(skill)
        {
        }

        /// <summary>
        /// Checks if the target is valid for user.
        /// </summary>
        public override bool IsValidTarget(GameLiving user, GameObject target)
        {
            var defender = target as GameLiving;
            if (defender != null)
                return GameServer.ServerRules.IsAllowedToAttack(user, defender, true);
            return false;
        }
    }
}