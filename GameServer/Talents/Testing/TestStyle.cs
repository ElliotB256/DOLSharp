using DOL.Talents.Clientside;
using DOL.GS;

namespace DOL.Talents
{
    public class TestStyle : ITalent
    {
        public TestStyle(SkillGroupTalent group, string name, ushort icon)
        {
            m_clientStyle = new Clientside.ClientStyleImplementation();
            m_clientStyle.Icon = icon;
            m_clientStyle.Name = name;
            m_clientStyle.SkillGroup = group;
        }

        protected ClientStyleImplementation m_clientStyle;

        public ITalentClientImplementation ClientImplementation
        { get { return m_clientStyle; } }

        public void Apply(ITalentOwner owner)
        {
            GamePlayer p = owner as GamePlayer;
            if (p != null)
                p.Out.SendMessage("You gain style " + m_clientStyle.Name + "!", GS.PacketHandler.eChatType.CT_Spell, GS.PacketHandler.eChatLoc.CL_SystemWindow);
        }

        public bool IsValid(ITalentOwner owner)
        {
            return true;
        }

        public void Remove(ITalentOwner owner)
        {
            GamePlayer p = owner as GamePlayer;
            if (p != null)
                p.Out.SendMessage("You lose style " + m_clientStyle.Name + "!", GS.PacketHandler.eChatType.CT_Spell, GS.PacketHandler.eChatLoc.CL_SystemWindow);
        }
    }
}
