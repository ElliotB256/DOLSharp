using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DOL.GS;
using DOL.GS.Representation;

namespace DOL.ItemSocket
{
    /// <summary>
    /// Represents an empty equipment socket.
    /// </summary>
    public class EmptySocket : IInventoryItemRepresentation
    {
        public EmptySocket(int slot)
        {
            m_slotPosition = slot;
        }
        private int m_slotPosition;
        public byte Bonus { get { return 0; } }
        public byte BonusLevel { get { return 1; } }
        public ushort Color { get { return 0; } }
        public byte Condition { get { return 100; } }
        public int Count { get { return 1; } }
        public byte Durability { get { return 100; } }
        public ushort Effect { get { return 0; } }
        public ushort Emblem { get { return 0; } }
        public byte HandFlag { get { return 0; } }
        public bool IsCraftable { get { return false; } }
        public bool IsSalvageable { get { return false; } }
        public byte Level { get { return 1; } }
        public ushort Model { get { return 513; } }
        public byte ModelExtension { get { return 0; } }
        public string Name { get { return DAoEPlayerInventory.UI_INDENT_STRING+"Empty slot"; } }
        public int ObjectType { get { return (int)eObjectType.GenericItem; } }
        public byte Quality { get { return 100; } }
        public int SlotPosition { get { return m_slotPosition; } }
        public ushort Spell1Icon { get { return 0; } }
        public string Spell1Name { get { return ""; } }
        public ushort Spell2Icon { get { return 0; } }
        public string Spell2Name { get { return ""; } }
        public int TypeDamage { get { return 0; } }
        public byte Value1 { get { return (byte)(1 & 0xFF); } }
        public byte Value2 { get { return (byte)((1 >> 8) & 0xFF); } }
        public ushort Weight { get { return 0; } }
    }
}
