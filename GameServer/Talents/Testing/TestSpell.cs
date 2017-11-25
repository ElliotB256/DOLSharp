using DOL.Talents.Clientside;
using DOL.GS;
using System;

namespace DOL.Talents
{
    public class TestSpell : IUseableTalent
    {
        public TestSpell(SkillGroupTalent group, string name, ushort icon)
        {
            m_clientSpell = new Clientside.ClientSpellImplementation();
            m_clientSpell.Icon = icon;
            m_clientSpell.Name = name;
            m_clientSpell.SkillGroup = group;
        }

        protected ClientSpellImplementation m_clientSpell;

        protected GamePlayer m_playerOwner;

        public ITalentClientImplementation ClientImplementation
        { get { return m_clientSpell; } }

        public void Apply(ITalentOwner owner)
        {
            GamePlayer p = owner as GamePlayer;
            if (p != null)
            {
                p.Out.SendMessage("You gain spell " + m_clientSpell.Name + "!", GS.PacketHandler.eChatType.CT_Spell, GS.PacketHandler.eChatLoc.CL_SystemWindow);
                m_playerOwner = p;
            }
        }

        public bool IsValid(ITalentOwner owner)
        {
            return true;
        }

        public void Remove(ITalentOwner owner)
        {
            GamePlayer p = owner as GamePlayer;
            if (p != null)
            {
                p.Out.SendMessage("You lose spell " + m_clientSpell.Name + "!", GS.PacketHandler.eChatType.CT_Spell, GS.PacketHandler.eChatLoc.CL_SystemWindow);
                m_playerOwner = null;
            }
        }

        public void Use()
        {
            if (m_playerOwner != null)
            {
                m_playerOwner.Out.SendMessage("You used " + m_clientSpell.Name + "!", GS.PacketHandler.eChatType.CT_Spell, GS.PacketHandler.eChatLoc.CL_SystemWindow);
            }
        }
    }
}
