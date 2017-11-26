using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOL.GS.Representation
{
    /// <summary>
    /// Represents an inventory item in the player's inventory
    /// </summary>
    public interface IInventoryItemRepresentation
    {
        int ObjectType { get; }
        int TypeDamage { get; }
        int Count { get; }
        ushort Weight { get; }
        byte Condition { get; }
        byte Durability { get; }
        byte Quality { get; }
        byte Bonus { get; }
        byte BonusLevel { get; }
        byte HandFlag { get; }
        ushort Model { get; }
        byte ModelExtension { get; }
        byte Value1 { get; }
        byte Value2 { get; }
        bool IsSalvageable { get; }
        bool IsCraftable { get; }
        string Name { get; }
        byte Level { get; }
        ushort Emblem { get; }
        ushort Color { get; }
        ushort Effect { get; }

        ushort Spell1Icon { get; }
        string Spell1Name { get; }
        ushort Spell2Icon { get; }
        string Spell2Name { get; }
        int SlotPosition { get; }
    }
}