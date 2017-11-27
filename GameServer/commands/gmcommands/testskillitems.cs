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
using DOL.GS.PacketHandler;
using DOL.Language;
using DOL.GS;
using DOL.Database;
using DOL.ItemSkills;

namespace DOL.GS.Commands
{
    [CmdAttribute(
        "&testskillitems",
        ePrivLevel.GM,
        "Creates a set of socketable items for testing purposes",
        "/testskillitems",
        "/testskillitems help")]
    public class TestSkillItemsCommandHandler : AbstractCommandHandler, ICommandHandler
    {
        public void OnCommand(GameClient client, string[] args)
        {
            if (client == null || client.Player == null)
            {
                return;
            }

            if (args.Length > 1 || (args.Length > 1 && args[0] == "help"))
            {
                DisplaySyntax(client);
                return;
            }

            for (int i = 0; i < 6; i++)
                client.Player.Inventory.AddItem(eInventorySlot.FirstEmptyVault, CreateRandomSkillGem());

            GameInventoryItem helm = new GameInventoryItem();
            helm.Id_nb = InventoryItem.BLANK_ITEM;
            helm.Model = 1280;
            helm.Name = "Dinberg's Mighty Hat";
            helm.Object_Type = (int)eObjectType.Cloth;
            helm.Item_Type = (int)eEquipmentItems.HEAD;
            client.Player.Inventory.AddItem(eInventorySlot.FirstEmptyVault, helm);

            GameInventoryItem torso = new GameInventoryItem();
            torso.Id_nb = InventoryItem.BLANK_ITEM;
            torso.Model = 799;
            torso.Name = "Magical Shirt";
            torso.Object_Type = (int)eObjectType.Cloth;
            torso.Item_Type = (int)eEquipmentItems.TORSO;
            client.Player.Inventory.AddItem(eInventorySlot.FirstEmptyVault, torso);

            GameInventoryItem legs = new GameInventoryItem();
            legs.Id_nb = InventoryItem.BLANK_ITEM;
            legs.Model = 800;
            legs.Name = "Magical Trousers";
            legs.Object_Type = (int)eObjectType.Cloth;
            legs.Item_Type = (int)eEquipmentItems.LEGS;
            client.Player.Inventory.AddItem(eInventorySlot.FirstEmptyVault, legs);

            GameInventoryItem arms = new GameInventoryItem();
            arms.Id_nb = InventoryItem.BLANK_ITEM;
            arms.Model = 801;
            arms.Name = "Magical Sleeves";
            arms.Object_Type = (int)eObjectType.Cloth;
            arms.Item_Type = (int)eEquipmentItems.ARMS;
            client.Player.Inventory.AddItem(eInventorySlot.FirstEmptyVault, arms);

            GameInventoryItem gloves = new GameInventoryItem();
            gloves.Id_nb = InventoryItem.BLANK_ITEM;
            gloves.Model = 802;
            gloves.Name = "Snuggly Gloves";
            gloves.Object_Type = (int)eObjectType.Cloth;
            gloves.Item_Type = (int)eEquipmentItems.HAND;
            client.Player.Inventory.AddItem(eInventorySlot.FirstEmptyVault, gloves);

            GameInventoryItem shoes = new GameInventoryItem();
            shoes.Id_nb = InventoryItem.BLANK_ITEM;
            shoes.Model = 803;
            shoes.Name = "Lovely Shoes";
            shoes.Object_Type = (int)eObjectType.Cloth;
            shoes.Item_Type = (int)eEquipmentItems.FEET;
            client.Player.Inventory.AddItem(eInventorySlot.FirstEmptyVault, shoes);

            client.Out.SendMessage("You receive magical items!", eChatType.CT_Spell, eChatLoc.CL_SystemWindow);
        }

        /// <summary>
        /// Create a random skillgem for test purposes.
        /// </summary>
        /// <returns></returns>
        public SkillGem CreateRandomSkillGem()
        {
            SkillGem sg = new SkillGem();
            sg.Id_nb = InventoryItem.BLANK_ITEM;
            sg.Name = "Skill Gem";
            switch (Util.Random(4))
            {
                default:
                    sg.Model = 117;
                    break;
                case 1:
                    sg.Model = 116;
                    break;
                case 2:
                    sg.Model = 115;
                    break;
                case 3:
                    sg.Model = 110;
                    break;
                case 4:
                    sg.Model = 112;
                    break;
            }
            sg.Object_Type = (int)eObjectType.Magical;
            return sg;
        }
    }
}