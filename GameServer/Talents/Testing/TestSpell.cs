using DOL.Talents.Clientside;
using DOL.GS;

namespace DOL.Talents
{
    public class TestSpell : ITalent
    {
        public TestSpell(SkillGroupTalent group, string name, ushort icon)
        {
            m_clientSpell = new Clientside.ClientSpellImplementation();
            m_clientSpell.Icon = icon;
            m_clientSpell.Name = name;
            m_clientSpell.SkillGroup = group;
        }

        protected ClientSpellImplementation m_clientSpell;

        public ITalentClientImplementation ClientImplementation
        { get { return m_clientSpell; } }

        public void Apply(ITalentOwner owner)
        {
            GamePlayer p = owner as GamePlayer;
            if (p != null)
                p.Out.SendMessage("You gain spell " + m_clientSpell.Name + "!", GS.PacketHandler.eChatType.CT_Spell, GS.PacketHandler.eChatLoc.CL_SystemWindow);
        }

        public bool IsValid(ITalentOwner owner)
        {
            return true;
        }

        public void Remove(ITalentOwner owner)
        {
            GamePlayer p = owner as GamePlayer;
            if (p != null)
                p.Out.SendMessage("You lose spell " + m_clientSpell.Name + "!", GS.PacketHandler.eChatType.CT_Spell, GS.PacketHandler.eChatLoc.CL_SystemWindow);
        }
    }
}
