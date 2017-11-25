using DOL.GS;

namespace DOL.Talents
{
    public class TestAbility : PassiveTalent
    {
        public TestAbility(string name) : base(name)
        { }

        public override void Apply(ITalentOwner owner)
        {
            GamePlayer p = owner as GamePlayer;
            if (p != null)
                p.Out.SendMessage("You have " + m_clientsideAbility.Name + "!", GS.PacketHandler.eChatType.CT_Spell, GS.PacketHandler.eChatLoc.CL_SystemWindow);
        }

        public override void Remove(ITalentOwner owner)
        {
            GamePlayer p = owner as GamePlayer;
            if (p != null)
                p.Out.SendMessage("You lose " + m_clientsideAbility.Name + "!", GS.PacketHandler.eChatType.CT_Spell, GS.PacketHandler.eChatLoc.CL_SystemWindow);
        }

        public override bool IsValid(ITalentOwner owner)
        {
            return true;
        }
    }
}
