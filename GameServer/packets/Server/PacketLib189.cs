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
using System.Linq;
using log4net;
using DOL.GS.Quests;
using System.Reflection;
using DOL.Database;
using System.Collections;
using DOL.GS.Housing;
using System.Collections.Generic;
using DOL.GS.Representation;

namespace DOL.GS.PacketHandler
{
	[PacketLib(189, GameClient.eClientVersion.Version189)]
	public class PacketLib189 : PacketLib188
	{
		/// <summary>
		/// Defines a logger for this class.
		/// </summary>
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		/// <summary>
		/// Constructs a new PacketLib for Version 1.88 clients
		/// </summary>
		/// <param name="client">the gameclient this lib is associated with</param>
		public PacketLib189(GameClient client)
			: base(client)
		{

		}

		public override void SendLivingEquipmentUpdate(GameLiving living)
		{
			using (GSTCPPacketOut pak = new GSTCPPacketOut(GetPacketCode(eServerPackets.EquipmentUpdate)))
			{

				ICollection<InventoryItem> items = null;
				if (living.Inventory != null)
					items = living.Inventory.VisibleItems;
	
				pak.WriteShort((ushort)living.ObjectID);
				pak.WriteByte((byte)living.VisibleActiveWeaponSlots);
				pak.WriteByte((byte)living.CurrentSpeed); // new in 189b+, speed
				pak.WriteByte((byte)((living.IsCloakInvisible ? 0x01 : 0x00) | (living.IsHelmInvisible ? 0x02 : 0x00))); // new in 189b+, cloack/helm visibility
				pak.WriteByte((byte)((living.IsCloakHoodUp ? 0x01 : 0x00) | (int)living.ActiveQuiverSlot)); //bit0 is hood up bit4 to 7 is active quiver
	
				if (items != null)
				{
					pak.WriteByte((byte)items.Count);
					foreach (InventoryItem item in items)
					{
						ushort model = (ushort)(item.Model & 0x1FFF);
						int slot = item.SlotPosition;
						//model = GetModifiedModel(model);
						int texture = item.Emblem != 0 ? item.Emblem : item.Color;
						if (item.SlotPosition == Slot.LEFTHAND || item.SlotPosition == Slot.CLOAK) // for test only cloack and shield
							slot = slot | ((texture & 0x010000) >> 9); // slot & 0x80 if new emblem
						pak.WriteByte((byte)slot);
						if ((texture & ~0xFF) != 0)
							model |= 0x8000;
						else if ((texture & 0xFF) != 0)
							model |= 0x4000;
						if (item.Effect != 0)
							model |= 0x2000;
	
						pak.WriteShort(model);
	
						if (item.SlotPosition > Slot.RANGED || item.SlotPosition < Slot.RIGHTHAND)
							pak.WriteByte((byte)item.Extension);
	
						if ((texture & ~0xFF) != 0)
							pak.WriteShort((ushort)texture);
						else if ((texture & 0xFF) != 0)
							pak.WriteByte((byte)texture);
						if (item.Effect != 0)
							pak.WriteByte((byte)item.Effect);
					}
				}
				else
				{
					pak.WriteByte(0x00);
				}
				SendTCP(pak);
			}
		}

		/// <summary>
		/// New inventory update handler. This handler takes into account that
		/// a slot on the client isn't necessarily the same as a slot on the
		/// server, e.g. house vaults.
		/// </summary>
		/// <param name="updateItems"></param>
		/// <param name="windowType"></param>
		public override void SendInventoryItemsUpdate(IDictionary<int, IInventoryItemRepresentation> updateItems, eInventoryWindowType windowType)
		{
			if (m_gameClient.Player == null)
				return;
			if (updateItems == null)
				updateItems = new Dictionary<int, IInventoryItemRepresentation>();
			if (updateItems.Count <= ServerProperties.Properties.MAX_ITEMS_PER_PACKET)
			{
				SendInventoryItemsPartialUpdate(updateItems, windowType);
				return;
			}

			var items = new Dictionary<int, IInventoryItemRepresentation>(ServerProperties.Properties.MAX_ITEMS_PER_PACKET);
			foreach (var item in updateItems)
			{
				items.Add(item.Key, item.Value);
				if (items.Count >= ServerProperties.Properties.MAX_ITEMS_PER_PACKET)
				{
					SendInventoryItemsPartialUpdate(items, windowType);
					items.Clear();
					windowType = eInventoryWindowType.Update;
				}
			}

			if (items.Count > 0)
				SendInventoryItemsPartialUpdate(items, windowType);
		}

		/// <summary>
		/// Sends inventory items to the client.  If windowType is one of the client inventory windows then the client
		/// will display the window.  Once the window is displayed to the client all handling of items in the window
		/// is done in the move item request handlers
		/// </summary>
		/// <param name="items"></param>
		/// <param name="windowType"></param>
		protected override void SendInventoryItemsPartialUpdate(IDictionary<int, IInventoryItemRepresentation> items, eInventoryWindowType windowType)
		{
			using (GSTCPPacketOut pak = new GSTCPPacketOut(GetPacketCode(eServerPackets.InventoryUpdate)))
			{
				GameVault houseVault = m_gameClient.Player.ActiveInventoryObject as GameVault;
				pak.WriteByte((byte)(items.Count));
				pak.WriteByte(0x00); // new in 189b+, show shield in left hand
				pak.WriteByte((byte)((m_gameClient.Player.IsCloakInvisible ? 0x01 : 0x00) | (m_gameClient.Player.IsHelmInvisible ? 0x02 : 0x00))); // new in 189b+, cloack/helm visibility
				if (windowType == eInventoryWindowType.HouseVault && houseVault != null)
					pak.WriteByte((byte)(houseVault.Index + 1));	// Add the vault number to the window caption
				else
					pak.WriteByte((byte)((m_gameClient.Player.IsCloakHoodUp ? 0x01 : 0x00) | (int)m_gameClient.Player.ActiveQuiverSlot)); //bit0 is hood up bit4 to 7 is active quiver
				// ^ in 1.89b+, 0 bit - showing hooded cloack, if not hooded not show cloack at all ?
				pak.WriteByte(m_gameClient.Player.VisibleActiveWeaponSlots);
				pak.WriteByte((byte)windowType);
				foreach (var entry in items)
				{
					pak.WriteByte((byte)(entry.Key));
					WriteItemData(pak, entry.Value);
				}
				SendTCP(pak);
			}
		}

		/// <summary>
		/// Legacy inventory update. This handler silently
		/// assumes that a slot on the client matches a slot on the server.
		/// </summary>
		/// <param name="slots"></param>
		/// <param name="preAction"></param>
		protected override void SendInventorySlotsUpdateRange(ICollection<int> slots, eInventoryWindowType windowType)
		{
			using (GSTCPPacketOut pak = new GSTCPPacketOut(GetPacketCode(eServerPackets.InventoryUpdate)))
			{
				GameVault houseVault = m_gameClient.Player.ActiveInventoryObject as GameVault;
	
				pak.WriteByte((byte)(slots == null ? 0 : slots.Count));
				pak.WriteByte(0); // CurrentSpeed & 0xFF (not used for player, only for NPC)
				pak.WriteByte((byte)((m_gameClient.Player.IsCloakInvisible ? 0x01 : 0x00) | (m_gameClient.Player.IsHelmInvisible ? 0x02 : 0x00))); // new in 189b+, cloack/helm visibility
	
				if (windowType == eInventoryWindowType.HouseVault && houseVault != null)
				{
					pak.WriteByte((byte)(houseVault.Index + 1));	// Add the vault number to the window caption
				}
				else
				{
					pak.WriteByte((byte)((m_gameClient.Player.IsCloakHoodUp ? 0x01 : 0x00) | (int)m_gameClient.Player.ActiveQuiverSlot)); //bit0 is hood up bit4 to 7 is active quiver
				}
	
				pak.WriteByte((byte)m_gameClient.Player.VisibleActiveWeaponSlots);
				pak.WriteByte((byte)windowType);
	
				if (slots != null)
				{
					foreach (int updatedSlot in slots)
					{
						if (updatedSlot >= (int)eInventorySlot.Consignment_First && updatedSlot <= (int)eInventorySlot.Consignment_Last)
						{
							log.Error("PacketLib198:SendInventorySlotsUpdateBase - GameConsignmentMerchant inventory is no longer cached with player.  Use a Dictionary<int, InventoryItem> instead.");
							pak.WriteByte((byte)(updatedSlot - (int)eInventorySlot.Consignment_First + (int)eInventorySlot.HousingInventory_First));
						}
						else
						{
							pak.WriteByte((byte)(updatedSlot));
						}
	
						WriteItemData(pak, m_gameClient.Player.Inventory.GetItemRepresentation((eInventorySlot)(updatedSlot)));
					}
				}
	
				SendTCP(pak);
			}
		}

		protected static int MAX_NAME_LENGTH = 55;

		protected virtual void WriteItemData(GSTCPPacketOut pak, IInventoryItemRepresentation item)
		{
			if (item == null)
			{
				pak.Fill(0x00, 19);
				return;
			}

			pak.WriteByte(item.Level);
			pak.WriteByte(item.Value1);
			pak.WriteByte(item.Value2);
	        pak.WriteByte(item.HandFlag);

			pak.WriteByte((byte)((item.TypeDamage > 3 ? 0 : item.TypeDamage << 6) | item.ObjectType));
			pak.WriteShort(item.Weight);
			pak.WriteByte(item.Condition);
			pak.WriteByte(item.Durability);
			pak.WriteByte(item.Quality);
			pak.WriteByte(item.Bonus);
			pak.WriteShort(item.Model);
			pak.WriteByte(item.ModelExtension);

			int flag = 0;
			if (item.Emblem != 0)
			{
				pak.WriteShort(item.Emblem);
				flag |= (item.Emblem & 0x010000) >> 16; // = 1 for newGuildEmblem
			}
			else
			{
				pak.WriteShort(item.Color);
			}

            if (item.IsSalvageable)
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

            string name = item.Name;
			if (name.Length > MAX_NAME_LENGTH)
				name = name.Substring(0, MAX_NAME_LENGTH);

			pak.WritePascalString(name);
		}


		public override void SendHouse(House house)
		{
			using (GSTCPPacketOut pak = new GSTCPPacketOut(GetPacketCode(eServerPackets.HouseCreate)))
			{
				pak.WriteShort((ushort)house.HouseNumber);
				pak.WriteShort((ushort)house.Z);
				pak.WriteInt((uint)house.X);
				pak.WriteInt((uint)house.Y);
				pak.WriteShort((ushort)house.Heading);
				pak.WriteShort((ushort)house.PorchRoofColor);
				int flagPorchAndGuildEmblem = (house.Emblem & 0x010000) >> 13;//new Guild Emblem
				if (house.Porch)
					flagPorchAndGuildEmblem |= 1;
				if (house.OutdoorGuildBanner)
					flagPorchAndGuildEmblem |= 2;
				if (house.OutdoorGuildShield)
					flagPorchAndGuildEmblem |= 4;
				pak.WriteShort((ushort)flagPorchAndGuildEmblem);
				pak.WriteShort((ushort)house.Emblem);
				pak.WriteShort(0); // new in 1.89b+ (scheduled for resposession XXX hourses ago)
				pak.WriteByte((byte)house.Model);
				pak.WriteByte((byte)house.RoofMaterial);
				pak.WriteByte((byte)house.WallMaterial);
				pak.WriteByte((byte)house.DoorMaterial);
				pak.WriteByte((byte)house.TrussMaterial);
				pak.WriteByte((byte)house.PorchMaterial);
				pak.WriteByte((byte)house.WindowMaterial);
				pak.WriteByte(0);
				pak.WriteShort(0); // new in 1.89b+
				pak.WritePascalString(house.Name);
	
				SendTCP(pak);
			}
			
			// Update cache
			m_gameClient.HouseUpdateArray[new Tuple<ushort, ushort>(house.RegionID, (ushort)house.HouseNumber)] = GameTimer.GetTickCount();
		}

		public override void SendGarden(House house)
		{
			using (GSTCPPacketOut pak = new GSTCPPacketOut(GetPacketCode(eServerPackets.HouseChangeGarden)))
			{
				pak.WriteShort((ushort)house.HouseNumber);
				pak.WriteShort(0); // sheduled for repossession (in hours) new in 1.89b+
				pak.WriteByte((byte)house.OutdoorItems.Count);
				pak.WriteByte(0x80);
	
				foreach (var entry in house.OutdoorItems.OrderBy(entry => entry.Key))
				{
					OutdoorItem item = entry.Value;
					pak.WriteByte((byte)entry.Key);
					pak.WriteShort((ushort)item.Model);
					pak.WriteByte((byte)item.Position);
					pak.WriteByte((byte)item.Rotation);
				}
	
				SendTCP(pak);
			}
			
			// Update cache
			m_gameClient.HouseUpdateArray.UpdateIfExists(new Tuple<ushort, ushort>(house.RegionID, (ushort)house.HouseNumber), GameTimer.GetTickCount());
		}

		public override void SendGarden(House house, int i)
		{
			using (GSTCPPacketOut pak = new GSTCPPacketOut(GetPacketCode(eServerPackets.HouseChangeGarden)))
			{
				pak.WriteShort((ushort)house.HouseNumber);
				pak.WriteShort(0); // sheduled for repossession (in hours) new in 1.89b+
				pak.WriteByte(0x01);
				pak.WriteByte(0x00); // update
				OutdoorItem item = (OutdoorItem)house.OutdoorItems[i];
				pak.WriteByte((byte)i);
				pak.WriteShort((ushort)item.Model);
				pak.WriteByte((byte)item.Position);
				pak.WriteByte((byte)item.Rotation);
				SendTCP(pak);
			}
			
			// Update cache
			m_gameClient.HouseUpdateArray.UpdateIfExists(new Tuple<ushort, ushort>(house.RegionID, (ushort)house.HouseNumber), GameTimer.GetTickCount());
		}
              

		public override void SendHouseOccupied(House house, bool flagHouseOccuped)
		{
			using (GSTCPPacketOut pak = new GSTCPPacketOut(GetPacketCode(eServerPackets.HouseChangeGarden)))
			{
				pak.WriteShort((ushort)house.HouseNumber);
				pak.WriteShort(0); // sheduled for repossession (in hours) new in 1.89b+
				pak.WriteByte(0x00);
				pak.WriteByte((byte)(flagHouseOccuped ? 1 : 0));
	
				SendTCP(pak);
			}
			
			// Update cache
			m_gameClient.HouseUpdateArray.UpdateIfExists(new Tuple<ushort, ushort>(house.RegionID, (ushort)house.HouseNumber), GameTimer.GetTickCount());
		}

		public override void SendEnterHouse(House house)
		{
			using (GSTCPPacketOut pak = new GSTCPPacketOut(GetPacketCode(eServerPackets.HouseEnter)))
			{
				pak.WriteShort((ushort)house.HouseNumber);
				pak.WriteShort((ushort)25000);         //constant!
				pak.WriteInt((uint)house.X);
				pak.WriteInt((uint)house.Y);
				pak.WriteShort((ushort)house.Heading); //useless/ignored by client.
				pak.WriteByte(0x00);
				pak.WriteByte((byte)(house.GetGuildEmblemFlags() | (house.Emblem & 0x010000) >> 14));//new Guild Emblem
				pak.WriteShort((ushort)house.Emblem);	//emblem
				pak.WriteByte(0x00);
				pak.WriteByte(0x00);
				pak.WriteByte((byte)house.Model);
				pak.WriteByte(0x00);
				pak.WriteByte(0x00);
				pak.WriteByte(0x00);
				pak.WriteByte((byte)house.Rug1Color);
				pak.WriteByte((byte)house.Rug2Color);
				pak.WriteByte((byte)house.Rug3Color);
				pak.WriteByte((byte)house.Rug4Color);
				pak.WriteByte(0x00);
				pak.WriteByte(0x00); // houses codemned ?
				pak.WriteShort(0); // 0xFFBF = condemned door model
				pak.WriteByte(0x00);
	
				SendTCP(pak);
			}
		}
	}
}
