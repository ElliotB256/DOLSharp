using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DOL.GS;
using DOL.Database;
using DOL.GS.Representation;

namespace DOL.ItemSocket
{
    /// <summary>
    /// Represents a socketed skill gem
    /// </summary>
    public class SocketedSkillGemRepresentation : InventoryItemRepresentation
    {
        public SocketedSkillGemRepresentation(InventoryItem item) : base(item)
        {
            m_name = DAoEPlayerInventory.UI_INDENT_STRING + m_name;   
        }
    }
}
