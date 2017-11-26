/*
 * DAWN OF LIGHT - The first free open source DAoC server emulator
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; either version 2
 * of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
 *
 */
#define NOENCRYPTION
using System;
using log4net;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using DOL.GS.Representation;

namespace DOL.GS.PacketHandler
{
	[PacketLib(182, GameClient.eClientVersion.Version182)]
	public class PacketLib182 : PacketLib181
	{
		/// <summary>
		/// Defines a logger for this class.
		/// </summary>
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		/// <summary>
		/// Constructs a new PacketLib for Version 1.82 clients
		/// </summary>
		/// <param name="client">the gameclient this lib is associated with</param>
		public PacketLib182(GameClient client)
			: base(client)
		{
		}

		protected override void SendInventorySlotsUpdateRange(ICollection<int> slots, eInventoryWindowType windowType)
		{
			using (GSTCPPacketOut pak = new GSTCPPacketOut(GetPacketCode(eServerPackets.InventoryUpdate)))
			{
				pak.WriteByte((byte)(slots == null ? 0 : slots.Count));
				pak.WriteByte((byte)((m_gameClient.Player.IsCloakHoodUp ? 0x01 : 0x00) | (int)m_gameClient.Player.ActiveQuiverSlot)); //bit0 is hood up bit4 to 7 is active quiver
				pak.WriteByte((byte)m_gameClient.Player.VisibleActiveWeaponSlots);
				pak.WriteByte((byte)windowType);
				if (slots != null)
				{
					foreach (int updatedSlot in slots)
					{
						if (updatedSlot >= (int)eInventorySlot.Consignment_First && updatedSlot <= (int)eInventorySlot.Consignment_Last)
							pak.WriteByte((byte)(updatedSlot - (int)eInventorySlot.Consignment_First + (int)eInventorySlot.HousingInventory_First));
						else
							pak.WriteByte((byte)(updatedSlot));

                        IInventoryItemRepresentation item = m_gameClient.Player.Inventory.GetItemRepresentation((eInventorySlot)updatedSlot);

                        if (item == null)
						{
							pak.Fill(0x00, 19);
							continue;
						}
						pak.WriteByte(item.Level);
						pak.WriteByte(item.Value1);
						pak.WriteByte(item.Value2);
                        pak.WriteByte(item.HandFlag);
						pak.WriteByte((byte)((item.TypeDamage > 3 ? 0 : item.TypeDamage << 6) | item.ObjectType));
						pak.WriteShort(item.Weight);
						pak.WriteByte(item.Condition); // % of con
						pak.WriteByte(item.Durability); // % of dur
						pak.WriteByte(item.Quality); // % of qua
						pak.WriteByte(item.Bonus); // % bonus
						pak.WriteShort(item.Model);
						pak.WriteByte(item.ModelExtension);
						int flag = 0;
						if (item.Emblem != 0)
						{
							pak.WriteShort(item.Emblem);
							flag |= (item.Emblem & 0x010000) >> 16; // = 1 for newGuildEmblem
						}
						else
							pak.WriteShort((ushort)item.Color);
						if (item.IsCraftable)
							flag |= 0x02; // enable salvage button
						if (item.IsCraftable)
							flag |= 0x04; // enable craft button

                        if (item.Spell1Icon != 0)
                            flag |= 0x08;

                        if (item.Spell2Icon != 0)
                            flag |= 0x10;

                        pak.WriteByte((byte)flag);
                        if ((flag & 0x08) == 0x08)
                        {
                            pak.WriteShort(item.Spell1Icon);
                            pak.WritePascalString(item.Spell1Name);
                        }

                        if ((flag & 0x10) == 0x10)
                        {
                            pak.WriteShort(item.Spell2Icon);
                            pak.WritePascalString(item.Spell2Name);
                        }

                        pak.WriteByte((byte)item.Effect);
						pak.WritePascalString(item.Name);
					}
				}
				SendTCP(pak);
			}
		}
	}
}
