using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DOL.Database;

namespace DOL.GS.Representation
{
    /// <summary>
    /// Represents an inventory item to the client
    /// </summary>
    public class InventoryItemRepresentation : IInventoryItemRepresentation
    {
        public InventoryItemRepresentation(InventoryItem item)
        {
            m_bonus = (byte)item.Bonus;
            m_objectType = item.Object_Type;
            m_typeDamage = item.Type_Damage;
            m_condition = (byte)item.ConditionPercent;
            m_count = item.Count;
            m_durability = (byte)item.DurabilityPercent;
            m_isSalvageable = item.SalvageYieldID != 0;
            m_model = (ushort)item.Model;
            m_modelExtension = item.Extension;
            m_quality = (byte)item.Quality;
            m_weight = (ushort)item.Weight;
            m_emblem = (ushort)item.Emblem;
            m_color = (ushort)item.Color;
            m_effect = (ushort)item.Effect;
            m_slotPosition = item.SlotPosition;

            switch (m_objectType)
            {
                case (int)eObjectType.GenericItem:
                    m_value1 = (byte)(item.Count & 0xFF);
                    m_value2 = (byte)((item.Count >> 8) & 0xFF);
                    break;
                case (int)eObjectType.Arrow:
                case (int)eObjectType.Bolt:
                case (int)eObjectType.Poison:
                    m_value1 = (byte)item.Count;
                    m_value2 = (byte)item.SPD_ABS;
                    break;
                case (int)eObjectType.Thrown:
                    m_value1 = (byte)item.DPS_AF;
                    m_value2 = (byte)item.Count;
                    break;
                case (int)eObjectType.Instrument:
                    m_value1 = (byte)(item.DPS_AF == 2 ? 0 : item.DPS_AF);
                    m_value2 = 0;
                    break; // unused
                case (int)eObjectType.Shield:
                    m_value1 = (byte)item.Type_Damage;
                    m_value2 = (byte)item.DPS_AF;
                    break;
                case (int)eObjectType.AlchemyTincture:
                case (int)eObjectType.SpellcraftGem:
                    m_value1 = 0;
                    m_value2 = 0;
                    break;
                case (int)eObjectType.HouseWallObject:
                case (int)eObjectType.HouseFloorObject:
                case (int)eObjectType.GardenObject:
                    m_value1 = 0;
                    m_value2 = (byte)item.SPD_ABS;
                    break;
                default:
                    m_value1 = (byte)item.DPS_AF;
                    m_value2 = (byte)item.SPD_ABS;
                    break;
            }

            if (item.Object_Type == (int)eObjectType.GardenObject)
                m_handFlag = ((byte)(item.DPS_AF));
            else
                m_handFlag = ((byte)(item.Hand << 6));

            m_name = item.Name;
            if (item.Count > 1)
                m_name = item.Count + " " + m_name;
            if (item.SellPrice > 0)
                m_name = m_name + " [" + Money.GetShortString(item.SellPrice) + "]";

            // Spells
            if (item.Object_Type != (int)eObjectType.AlchemyTincture)
            {
                SpellLine chargeEffectsLine = SkillBase.GetSpellLine(GlobalSpellsLines.Item_Effects);
                Spell spell1 = SkillBase.FindSpell(item.SpellID, chargeEffectsLine);
                Spell spell2 = SkillBase.FindSpell(item.SpellID, chargeEffectsLine);
                if (spell1 != null)
                {
                    m_spell1Icon = spell1.Icon;
                    m_spell1Name = spell1.Name;
                }
                if (spell2 != null)
                {
                    m_spell2Icon = spell2.Icon;
                    m_spell2Name = spell2.Name;
                }
            }

            m_isCraftable = false;
        }

        protected byte m_handFlag;
        public byte HandFlag
        { get { return m_handFlag; } }

        protected byte m_bonus;
        public byte Bonus
        { get { return m_bonus; } }

        protected int m_objectType;
        public int ObjectType
        { get { return m_objectType; } }

        protected byte m_value1;
        public byte Value1
        { get { return m_value1; } }

        protected byte m_value2;
        public byte Value2 { get { return m_value2; } }

        protected int m_typeDamage;
        public int TypeDamage
        { get { return m_typeDamage; } }

        protected byte m_condition;
        public byte Condition
        { get { return m_condition; } }

        protected int m_count;
        public int Count
        { get { return m_count; } }

        protected byte m_durability;
        public byte Durability
        { get { return m_durability; } }

        protected bool m_isCraftable = false;
        public bool IsCraftable
        { get { return m_isCraftable; } }

        protected bool m_isSalvageable = false;
        public bool IsSalvageable
        { get { return m_isSalvageable; } }

        protected ushort m_model;
        public ushort Model
        { get { return m_model; } }

        protected byte m_modelExtension;
        public byte ModelExtension
        { get { return m_modelExtension; } }

        protected string m_name;
        public string Name
        { get { return m_name; } }

        protected byte m_quality;
        public byte Quality
        { get { return m_quality; } }

        protected ushort m_spell1Icon;
        public ushort Spell1Icon
        { get { return m_spell1Icon; } }

        protected string m_spell1Name;
        public string Spell1Name
        { get { return m_spell1Name; } }

        protected ushort m_spell2Icon;
        public ushort Spell2Icon
        { get { return m_spell2Icon; } }

        protected string m_spell2Name;
        public string Spell2Name
        { get { return m_spell2Name; } }

        protected ushort m_weight;
        public ushort Weight
        { get { return m_weight; } }

        protected byte m_level;
        public byte Level
        { get { return m_level; } }

        protected ushort m_emblem;
        public ushort Emblem
        { get { return m_emblem; } }

        protected ushort m_color;
        public ushort Color
        { get { return m_color; } }

        protected ushort m_effect;
        public ushort Effect
        { get { return m_effect; } }

        protected byte m_bonusLevel;
        public byte BonusLevel
        { get { return m_bonusLevel; } }

        public static IDictionary<int, IInventoryItemRepresentation> CreateFrom(IDictionary<int, InventoryItem> list)
        {
            Dictionary<int, IInventoryItemRepresentation> reps = new Dictionary<int, IInventoryItemRepresentation>();
            list.ForEach(p => reps.Add(p.Key, new InventoryItemRepresentation(p.Value)));
            return reps;
        }

        public static ICollection<IInventoryItemRepresentation> CreateFrom(ICollection<InventoryItem> itemsToUpdate)
        {
            ICollection<IInventoryItemRepresentation> reps = new List<IInventoryItemRepresentation>();
            itemsToUpdate.ForEach(p => reps.Add(new InventoryItemRepresentation(p)));
            return reps;
        }

        protected int m_slotPosition;
        public int SlotPosition { get { return m_slotPosition; } }
    }
}
