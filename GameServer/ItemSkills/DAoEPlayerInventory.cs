using System;
using System.Collections.Generic;
using System.Linq;

using DOL.Database;
using DOL.Events;
using DOL.GS.PacketHandler;
using DOL.GS.Representation;
using DOL.ItemSocket;

namespace DOL.GS
{
    /// <summary>
    /// Player inventory in DAoE.
    /// Provides additional utility for socketing IPluggables into equipment
    /// </summary>
    public class DAoEPlayerInventory : GamePlayerInventory
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public DAoEPlayerInventory(GamePlayer player) : base(player)
        {
        }

        public const string UI_INDENT_STRING = "   ";

        /// <summary>
        /// Check if the slot is valid in this inventory
        /// </summary>
        /// <param name="slot">SlotPosition to check</param>
        /// <returns>the slot if it's valid or eInventorySlot.Invalid if not</returns>
        protected override eInventorySlot GetValidInventorySlot(eInventorySlot slot)
        {
            // Any requests for backpack inventory items are redirected to player vault
            if (slot == eInventorySlot.FirstEmptyBackpack)
                slot = eInventorySlot.FirstEmptyVault;

            if (slot == eInventorySlot.LastEmptyBackpack)
                slot = eInventorySlot.LastEmptyVault;

            // Some inventory slots are reserved for showing icons of currently equipped items.
            // These are never valid inventory slots.

            return base.GetValidInventorySlot(slot);
        }

        public override bool MoveItem(eInventorySlot fromSlot, eInventorySlot toSlot, int itemCount)
        {
            //Some slots are reserved for showing images of equipped items. If these slots are used, redirect to appropriate slot.
            eInventorySlot originalFrom = fromSlot;
            eInventorySlot originalTo = toSlot;
            eInventorySlot from_equipmentCopySlot = GetEquipmentSlotForIcon(fromSlot);
            eInventorySlot to_equipmentCopySlot = GetEquipmentSlotForIcon(toSlot);
            if (from_equipmentCopySlot != eInventorySlot.Invalid)
            {
                Log.Debug(string.Format("MoveItem: player {0} selects fromSlot corresponding to {1}", m_player.Name, from_equipmentCopySlot));
                fromSlot = from_equipmentCopySlot;
            }
            if (to_equipmentCopySlot != eInventorySlot.Invalid)
            {
                Log.Debug(string.Format("MoveItem: player {0} selects toSlot corresponding to {1}", m_player.Name, to_equipmentCopySlot));
                toSlot = to_equipmentCopySlot;
            }

            // player moves icon to its equipment slot, or vice versa. Do nothing.
            if (from_equipmentCopySlot != eInventorySlot.Invalid && from_equipmentCopySlot == to_equipmentCopySlot)
            {
                m_player.Out.SendInventorySlotsUpdate(new int[] { (int)originalFrom });
                return false;
            }

            ICollection<eInventorySlot> socketSlots = GetAllSocketSlots();
            InventoryItem fromItem = GetItem(fromSlot);
            InventoryItem toItem = GetItem(toSlot);
            if (fromItem == null)
            {
                if (socketSlots.Contains(fromSlot))
                    m_player.Out.SendMessage("That socket is empty.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                if (GetEquipmentSlotsSupportingSockets().Contains(fromSlot))
                    m_player.Out.SendMessage("That equipment slot is empty.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                m_player.Out.SendInventorySlotsUpdate(new int[] { (int)originalFrom });
                return false;
            }

            //We are attempting to move item into a socket slot. Check the item can be socketed.
            if (socketSlots.Contains(toSlot) && !(fromItem is ISocketable))
            {
                m_player.Out.SendMessage(String.Format("You cannot place {0} into an equipment socket.", fromItem.Name), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                m_player.Out.SendInventorySlotsUpdate(new int[] { (int)originalFrom });
                return false;
            }
            if (socketSlots.Contains(fromSlot) && toItem != null && !(toItem is ISocketable))
            {
                m_player.Out.SendMessage(String.Format("You cannot place {0} into an equipment socket.", toItem.Name), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                m_player.Out.SendInventorySlotsUpdate(new int[] { (int)originalFrom });
                return false;
            }

            bool moved = base.MoveItem(fromSlot, toSlot, itemCount);

            if (moved)
            {
                //If we have moved a piece of armor, we must also send updates also for the duplicated sockets used as icons.
                eInventorySlot fromIcon = GetIconSlotForEquipmentSlot(fromSlot);
                eInventorySlot toIcon = GetIconSlotForEquipmentSlot(toSlot);

                if (fromIcon != eInventorySlot.Invalid)
                    m_player.Out.SendInventorySlotsUpdate(new int[] { (int)fromIcon });

                if (toIcon != eInventorySlot.Invalid)
                    m_player.Out.SendInventorySlotsUpdate(new int[] { (int)toIcon });
            }

            return moved;
        }

        /// <summary>
        /// Maps the given equipment slot to a slot in inventory used as an icon for skill building.
        /// The client sees equipment in both to 
        /// </summary>
        /// <param name="equipment"></param>
        /// <returns></returns>
        eInventorySlot GetIconSlotForEquipmentSlot(eInventorySlot equipment)
        {
            switch (equipment)
            {
                case eInventorySlot.HeadArmor:
                    return eInventorySlot.FirstBackpack;
                case eInventorySlot.TorsoArmor:
                    return eInventorySlot.FirstBackpack + 8;
                case eInventorySlot.ArmsArmor:
                    return eInventorySlot.FirstBackpack + 16;
                case eInventorySlot.LegsArmor:
                    return eInventorySlot.FirstBackpack + 24;
                case eInventorySlot.HandsArmor:
                    return eInventorySlot.FirstBackpack + 32;
                case eInventorySlot.FeetArmor:
                    return eInventorySlot.FirstBackpack + 36;
                default:
                    return eInventorySlot.Invalid;
            }
        }

        /// <summary>
        /// Gets equipment slot for given icon slot. Inverse of GetCorrespondingIconSlot
        /// </summary>
        eInventorySlot GetEquipmentSlotForIcon(eInventorySlot iconSlot)
        {
            foreach (eInventorySlot s in GetEquipmentSlotsSupportingSockets())
            {
                if (iconSlot == GetIconSlotForEquipmentSlot(s))
                    return s;
            }
            return eInventorySlot.Invalid;
        }

        /// <summary>
        /// Return a collection of all equipment slots that support sockets.
        /// </summary>
        /// <returns></returns>
        ICollection<eInventorySlot> GetEquipmentSlotsSupportingSockets()
        {
            ICollection<eInventorySlot> slots = new List<eInventorySlot>();
            slots.Add(eInventorySlot.HeadArmor);
            slots.Add(eInventorySlot.TorsoArmor);
            slots.Add(eInventorySlot.ArmsArmor);
            slots.Add(eInventorySlot.LegsArmor);
            slots.Add(eInventorySlot.HandsArmor);
            slots.Add(eInventorySlot.FeetArmor);
            return slots;
        }

        /// <summary>
        /// Get all inventory slots corresponding to sockets.
        /// </summary>
        /// <returns></returns>
        ICollection<eInventorySlot> GetAllSocketSlots()
        {
            List<eInventorySlot> list = new List<eInventorySlot>();
            ICollection<eInventorySlot> equips = GetEquipmentSlotsSupportingSockets();
            equips.ForEach(p => list.AddRange(GetLinkedSlotsForEquipment(p)));
            return list;
        }

        /// <summary>
        /// Returns the slots linked to given equipment.
        /// </summary>
        /// <param name="equipment"></param>
        /// <returns></returns>
        ICollection<eInventorySlot> GetLinkedSlotsForEquipment(eInventorySlot equipment)
        {
            List<eInventorySlot> slots = new List<eInventorySlot>();
            int numberOfSlots = 7;
            switch (equipment)
            {
                case eInventorySlot.HandsArmor:
                case eInventorySlot.FeetArmor:
                    numberOfSlots = 3;
                    break;
            }

            eInventorySlot slot = GetIconSlotForEquipmentSlot(equipment);
            if (slot != eInventorySlot.Invalid)
            {
                for (int i = 0; i < numberOfSlots; i++)
                    slots.Add(slot + i + 1);
            }
            return slots;
        }

        public override IInventoryItemRepresentation GetItemRepresentation(eInventorySlot slot)
        {
            // For slots used for items, we send an 'Empty Socket' item if the slot is empty.
            if (GetAllSocketSlots().Contains(slot))
            {
                if (GetItem(slot) == null)
                    return new EmptySocket((int)slot);
            }

            // For slots corresponding to icons, instead show the item in equip slot
            eInventorySlot realSlot = GetEquipmentSlotForIcon(slot);
            if (realSlot != eInventorySlot.Invalid)
            {
                InventoryItem i = GetItem(realSlot);
                if (i == null)
                {
                    return new EmptyArmorIcon((int)slot, realSlot);
                }
                return new InventoryItemRepresentation(i);
            }

            return base.GetItemRepresentation(slot);
        }

        ///// <summary>
        ///// Return all IPluggables associated with item in given slot
        ///// </summary>
        ///// <param name="slot"></param>
        ///// <returns></returns>
        //List<IPluggable> GetPlugsForSlot(eInventorySlot slot)
        //{

        //}
    }
}