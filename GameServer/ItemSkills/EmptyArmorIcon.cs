using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DOL.GS;
using DOL.GS.Representation;

namespace DOL.ItemSocket
{
    /// <summary>
    /// Represents an icon for an empty armor piece
    /// </summary>
    public class EmptyArmorIcon : IInventoryItemRepresentation
    {
        public EmptyArmorIcon(int slot, eInventorySlot iconFor)
        {
            m_slotPosition = slot;

            switch (iconFor)
            {
                case eInventorySlot.HeadArmor:
                    m_name = "Empty Helmet";
                    m_model = 1296;
                    break;
                case eInventorySlot.TorsoArmor:
                    m_name = "Empty Torso";
                    m_model = 161;
                    break;
                case eInventorySlot.ArmsArmor:
                    m_name = "Empty Arms";
                    m_model = 163;
                    break;
                case eInventorySlot.LegsArmor:
                    m_name = "Empty Leggings";
                    m_model = 162;
                    break;
                case eInventorySlot.HandsArmor:
                    m_name = "Empty Gloves";
                    m_model = 164;
                    break;
                case eInventorySlot.FeetArmor:
                    m_name = "Empty Boots";
                    m_model = 165;
                    break;
            }
        }
        private int m_slotPosition;
        private string m_name;
        private ushort m_model;

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
        public ushort Model { get { return m_model; } }
        public byte ModelExtension { get { return 0; } }
        public string Name { get { return m_name; } }
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
