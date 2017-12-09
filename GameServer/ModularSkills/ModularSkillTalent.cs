using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DOL.Talents;
using DOL.Talents.Clientside;

namespace DOL.GS.ModularSkills
{
    public class ModularSkillTalent : ModularSkill, IUseableTalent
    {
        public ModularSkillTalent()
        {
            m_spell = new ClientSpellImplementation();
            m_spell.Icon = 10;
            m_spell.Name = "A Modular Skill";
        }

        private ClientSpellImplementation m_spell;
        public ITalentClientImplementation ClientImplementation { get { return m_spell;  } }

        public void Apply(ITalentOwner owner)
        {
            GamePlayer p = owner as GamePlayer;
            if (p != null)
            {
                p.Out.SendMessage("You gain the modular skill " + m_spell.Name + "!", GS.PacketHandler.eChatType.CT_Spell, GS.PacketHandler.eChatLoc.CL_SystemWindow);
            }

            GameLiving l = owner as GameLiving;
            if (l != null) Owner = l;
        }

        public bool IsValid(ITalentOwner owner)
        {
            return true;
        }

        public void Remove(ITalentOwner owner)
        {
        }

        public void Use()
        {
            if (Owner != null)
                TryUse();
        }
    }
}
