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
using DOL.GS.PacketHandler;
using DOL.Talents;

namespace DOL.GS.Commands
{
    [CmdAttribute(
        "&talent",
        ePrivLevel.GM,
        "Adds or removes talents from target.",
        "/talent passive <name>",
        "/talent spellline <name>",
        "/talent spell <icon> <name>",
        "/talent style <icon> <name>")]
    public class TalentCommandHandler : ICommandHandler
    {
        public void OnCommand(GameClient client, string[] args)
        {
            GamePlayer player = client.Player;

            // If an argument is given (an Int value is expected)
            if (args.Length > 1)
            {
                switch (args[1])
                {
                    case "passive":

                        if (args.Length == 2)
                            return;

                        string name = string.Join(" ", args, 2, args.Length - 2);
                        ITalent pt = new TestAbility(name);
                        player.Talents.Add(pt);
                        player.Out.SendUpdatePlayerSkills();
                        player.Out.SendMessage("Passive ability added.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                        break;
                    case "spell":

                        if (args.Length < 4)
                            return;

                        ushort icon = 0;
                        if (!ushort.TryParse(args[2], out icon))
                        {
                            player.Out.SendMessage("Could not parse icon id.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                            return;
                        }
                        string spellname = string.Join(" ", args, 3, args.Length - 3);

                        TestSpell ts = new TestSpell(null, spellname, icon);
                        player.Talents.Add(ts);
                        player.Out.SendUpdatePlayerSkills();
                        player.Out.SendMessage("Spell added!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                        break;
                    case "style":

                        if (args.Length < 4)
                            return;

                        ushort styleIcon = 0;
                        if (!ushort.TryParse(args[2], out styleIcon))
                        {
                            player.Out.SendMessage("Could not parse icon id.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                            return;
                        }
                        string styleName = string.Join(" ", args, 3, args.Length - 3);

                        // Find skill line for player
                        Talents.Clientside.SkillGroupTalent group = null;
                        foreach (ITalent talent in player.Talents.GetAllTalents())
                        {
                            if (talent is Talents.Clientside.SkillGroupTalent)
                            {
                                group = talent as Talents.Clientside.SkillGroupTalent;
                                break;
                            }
                        }
                        if (group == null)
                        {
                            group = new Talents.Clientside.SkillGroupTalent("Two Handed");
                            player.Talents.Add(group);
                        }

                        TestStyle testStyle = new TestStyle(group, styleName, styleIcon);
                        player.Talents.Add(testStyle);
                        player.Out.SendUpdatePlayerSkills();
                        player.Out.SendMessage("Style added!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                        break;
                    case "clear":
                        foreach (ITalent it in player.Talents.GetAllTalents())
                            player.Talents.Remove(it);

                        break;
                }
            }
        }
    }
}