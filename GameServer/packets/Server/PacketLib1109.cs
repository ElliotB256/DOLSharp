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
using System.Reflection;
using DOL.Database;
using DOL.GS.Representation;
using System.Collections;
using System.Collections.Generic;
using log4net;


namespace DOL.GS.PacketHandler
{
    [PacketLib(1109, GameClient.eClientVersion.Version1109)]
    public class PacketLib1109 : PacketLib1108
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Constructs a new PacketLib for Client Version 1.109
        /// </summary>
        /// <param name="client">the gameclient this lib is associated with</param>
        public PacketLib1109(GameClient client)
            : base(client)
        {

        }

		public override void SendTradeWindow()
		{
			if (m_gameClient.Player == null)
				return;
			if (m_gameClient.Player.TradeWindow == null)
				return;

			using (GSTCPPacketOut pak = new GSTCPPacketOut(GetPacketCode(eServerPackets.TradeWindow)))
			{
				lock (m_gameClient.Player.TradeWindow.Sync)
				{
					foreach (InventoryItem item in m_gameClient.Player.TradeWindow.TradeItems)
					{
						pak.WriteByte((byte)item.SlotPosition);
					}
					pak.Fill(0x00, 10 - m_gameClient.Player.TradeWindow.TradeItems.Count);
	
					pak.WriteShort(0x0000);
					pak.WriteShort((ushort)Money.GetMithril(m_gameClient.Player.TradeWindow.TradeMoney));
					pak.WriteShort((ushort)Money.GetPlatinum(m_gameClient.Player.TradeWindow.TradeMoney));
					pak.WriteShort((ushort)Money.GetGold(m_gameClient.Player.TradeWindow.TradeMoney));
					pak.WriteShort((ushort)Money.GetSilver(m_gameClient.Player.TradeWindow.TradeMoney));
					pak.WriteShort((ushort)Money.GetCopper(m_gameClient.Player.TradeWindow.TradeMoney));
	
					pak.WriteShort(0x0000);
					pak.WriteShort((ushort)Money.GetMithril(m_gameClient.Player.TradeWindow.PartnerTradeMoney));
					pak.WriteShort((ushort)Money.GetPlatinum(m_gameClient.Player.TradeWindow.PartnerTradeMoney));
					pak.WriteShort((ushort)Money.GetGold(m_gameClient.Player.TradeWindow.PartnerTradeMoney));
					pak.WriteShort((ushort)Money.GetSilver(m_gameClient.Player.TradeWindow.PartnerTradeMoney));
					pak.WriteShort((ushort)Money.GetCopper(m_gameClient.Player.TradeWindow.PartnerTradeMoney));
	
					pak.WriteShort(0x0000);
					ArrayList items = m_gameClient.Player.TradeWindow.PartnerTradeItems;
					if (items != null)
					{
						pak.WriteByte((byte)items.Count);
						pak.WriteByte(0x01);
					}
					else
					{
						pak.WriteShort(0x0000);
					}
					pak.WriteByte((byte)(m_gameClient.Player.TradeWindow.Repairing ? 0x01 : 0x00));
					pak.WriteByte((byte)(m_gameClient.Player.TradeWindow.Combine ? 0x01 : 0x00));
					if (items != null)
					{
						foreach (InventoryItem item in items)
						{
							pak.WriteByte((byte)item.SlotPosition);
							WriteItemData(pak, new InventoryItemRepresentation(item));
						}
					}
					if (m_gameClient.Player.TradeWindow.Partner != null)
						pak.WritePascalString("Trading with " + m_gameClient.Player.GetName(m_gameClient.Player.TradeWindow.Partner)); // transaction with ...
					else
						pak.WritePascalString("Selfcrafting"); // transaction with ...
					SendTCP(pak);
				}
			}
		}

		/// <summary>
		/// 1.109 items have an additional byte prior to item.Weight
		/// </summary>
		/// <param name="pak"></param>
		/// <param name="item"></param>
		protected override void WriteItemData(GSTCPPacketOut pak, IInventoryItemRepresentation item)
		{
			if (item == null)
			{
				pak.Fill(0x00, 20); // 1.109 +1 byte
				return;
			}

			pak.WriteByte(item.Level);
			pak.WriteByte(item.Value1);
			pak.WriteByte(item.Value2);
            pak.WriteByte(item.HandFlag);
			pak.WriteByte((byte)((item.TypeDamage > 3 ? 0 : item.TypeDamage << 6) | item.ObjectType));
			pak.Fill(0x00, 1); // 1.109, +1 byte, no clue what this is  - Tolakram
			pak.WriteShort(item.Weight);
			pak.WriteByte(item.Condition); // % of con
			pak.WriteByte(item.Durability); // % of dur
			pak.WriteByte(item.Quality); // % of qua
			pak.WriteByte(item.Bonus); // % bonus
			pak.WriteShort(item.Model);
			pak.WriteByte((byte)item.ModelExtension);
			int flag = 0;
			if (item.Emblem != 0)
			{
				pak.WriteShort((ushort)item.Emblem);
				flag |= (item.Emblem & 0x010000) >> 16; // = 1 for newGuildEmblem
			}
			else
				pak.WriteShort((ushort)item.Color);
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

		protected override void WriteTemplateData(GSTCPPacketOut pak, ItemTemplate template, int count)
		{
			if (template == null)
			{
				pak.Fill(0x00, 20);  // 1.109 +1 byte
				return;
			}
			
			pak.WriteByte((byte)template.Level);

			int value1;
			int value2;

			switch (template.Object_Type)
			{
				case (int)eObjectType.Arrow:
				case (int)eObjectType.Bolt:
				case (int)eObjectType.Poison:
				case (int)eObjectType.GenericItem:
					value1 = count; // Count
					value2 = template.SPD_ABS;
					break;
				case (int)eObjectType.Thrown:
					value1 = template.DPS_AF;
					value2 = count; // Count
					break;
				case (int)eObjectType.Instrument:
					value1 = (template.DPS_AF == 2 ? 0 : template.DPS_AF);
					value2 = 0;
					break;
				case (int)eObjectType.Shield:
					value1 = template.Type_Damage;
					value2 = template.DPS_AF;
					break;
				case (int)eObjectType.AlchemyTincture:
				case (int)eObjectType.SpellcraftGem:
					value1 = 0;
					value2 = 0;
					/*
					must contain the quality of gem for spell craft and think same for tincture
					*/
					break;
				case (int)eObjectType.GardenObject:
					value1 = 0;
					value2 = template.SPD_ABS;
					/*
					Value2 byte sets the width, only lower 4 bits 'seem' to be used (so 1-15 only)

					The byte used for "Hand" (IE: Mini-delve showing a weapon as Left-Hand
					usabe/TwoHanded), the lower 4 bits store the height (1-15 only)
					*/
					break;

				default:
					value1 = template.DPS_AF;
					value2 = template.SPD_ABS;
					break;
			}
			pak.WriteByte((byte)value1);
			pak.WriteByte((byte)value2);

			if (template.Object_Type == (int)eObjectType.GardenObject)
				pak.WriteByte((byte)(template.DPS_AF));
			else
				pak.WriteByte((byte)(template.Hand << 6));
			pak.WriteByte((byte)((template.Type_Damage > 3
				? 0
				: template.Type_Damage << 6) | template.Object_Type));
			pak.Fill(0x00, 1); // 1.109, +1 byte, no clue what this is  - Tolakram
			pak.WriteShort((ushort)template.Weight);
			pak.WriteByte(template.BaseConditionPercent);
			pak.WriteByte(template.BaseDurabilityPercent);
			pak.WriteByte((byte)template.Quality);
			pak.WriteByte((byte)template.Bonus);
			pak.WriteShort((ushort)template.Model);
			pak.WriteByte((byte)template.Extension);
			if (template.Emblem != 0)
				pak.WriteShort((ushort)template.Emblem);
			else
				pak.WriteShort((ushort)template.Color);
			pak.WriteByte((byte)0); // Flag
			pak.WriteByte((byte)template.Effect);
			if (count > 1)
				pak.WritePascalString(String.Format("{0} {1}", count, template.Name));
			else
				pak.WritePascalString(template.Name);
		}

	}
}
