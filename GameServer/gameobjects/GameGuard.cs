using DOL.AI.Brain;
using DOL.Language;
using System.Collections;
using DOL.GS.PacketHandler;

namespace DOL.GS
{
    public class GameGuard : GameNPC
    {
        public GameGuard()
            : base()
        {
            m_ownBrain = new GuardBrain();
            m_ownBrain.Body = this;
        }

        public override IList GetExamineMessages(GamePlayer player)
        {
            IList list = new ArrayList(4);
            list.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameGuard.GetExamineMessages.Examine", 
                                                GetName(0, true, player.Client.Account.Language, this), GetPronoun(0, true, player.Client.Account.Language),
                                                GetAggroLevelString(player, false)));
            return list;
        }
    }
}