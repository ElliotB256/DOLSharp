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
using System;
using System.Linq;
using System.Collections.Generic;
using DOL.GS.Representation;
using DOL.Database;

namespace DOL.GS.PacketHandler
{
	[PacketLib(1119, GameClient.eClientVersion.Version1119)]
	public class PacketLib1119 : PacketLib1118
	{
		/// <summary>
		/// Constructs a new PacketLib for Client Version 1.119
		/// </summary>
		/// <param name="client">the gameclient this lib is associated with</param>
		public PacketLib1119(GameClient client)
			: base(client)
		{
		}
		
		/// <summary>
		/// New item data packet for 1.119
		/// </summary>		
		protected override void WriteItemData(GSTCPPacketOut pak, IInventoryItemRepresentation item)
		{
			if (item == null)
			{
				pak.Fill(0x00, 24); // +1 byte: item.Effect changed to short
				return;
			}
			pak.WriteShort(0); // item uniqueID
			pak.WriteByte(item.Level);
			pak.WriteByte(item.Value1);
			pak.WriteByte(item.Value2);
            pak.WriteByte(item.HandFlag);
			pak.WriteByte((byte)((item.TypeDamage > 3 ? 0 : item.TypeDamage << 6) | item.ObjectType));
			pak.WriteByte(0x00); //unk 1.112
			pak.WriteShort(item.Weight);
			pak.WriteByte(item.Condition); // % of con
			pak.WriteByte(item.Durability); // % of dur
			pak.WriteByte(item.Quality); // % of qua
			pak.WriteByte(item.Bonus); // % bonus
			pak.WriteByte(item.BonusLevel); // 1.109
			pak.WriteShort(item.Model);
			pak.WriteByte(item.ModelExtension);
			int flag = 0;
			if (item.Emblem != 0)
			{
				pak.WriteShort((ushort)item.Emblem);
				flag |= (item.Emblem & 0x010000) >> 16; // = 1 for newGuildEmblem
			}
			else
			{
				pak.WriteShort((ushort)item.Color);
			}
            if (item.IsSalvageable)
			flag |= 0x02; // enable salvage button
            if (item.IsCraftable)
				flag |= 0x04; // enable craft button

            //spells
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
            pak.WriteShort(item.Effect);
			string name = item.Name;
			if (name == null) name = "";
			if (name.Length > 55)
				name = name.Substring(0, 55);
			pak.WritePascalString(name);
		}
	}
}
