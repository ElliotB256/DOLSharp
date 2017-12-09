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
using DOL.GS.ModularSkills;
using DOL.GS.PacketHandler;
using DOL.Talents;
using System.Collections.Generic;
using System.Linq;

namespace DOL.GS.Commands
{
    [CmdAttribute(
        "&ms",
        ePrivLevel.GM,
        "Adds a test modular skill to the user.",
        "/ms heal",
        "/ms castheal",
        "/ms pulseheal")]
    public class ModularSkillCommandHandler : ICommandHandler
    {
        public void OnCommand(GameClient client, string[] args)
        {
            GamePlayer player = client.Player;

            ModularSkillTalent testMS = null;
            SkillComponent sc = null;

            if (args.Length > 1)
            {
                switch (args[1])
                {
                    case "heal":

                        testMS = new ModularSkillTalent();
                        testMS.Invocation = new InstantInvocation(testMS);
                        sc = new SkillComponent();
                        sc.Applicator = new DirectSkillApplicator();
                        sc.TargetSelector = new SelfTargetSelector();
                        sc.SkillEffectChain = new List<ISkillEffect>();
                        sc.SkillEffectChain.Add( new HealEffect() );
                        testMS.Components.Add(sc);

                        player.Talents.Add(testMS);
                        player.Out.SendUpdatePlayerSkills();
                        player.Out.SendMessage("Passive ability added.", eChatType.CT_System, eChatLoc.CL_SystemWindow);

                        break;
                    case "castheal":

                        testMS = new ModularSkillTalent();
                        var gi = new GesturedInvocation(testMS);
                        gi.SpellAnimation = 2065;
                        gi.Duration = 3f;
                        testMS.Invocation = gi;
                        sc = new SkillComponent();
                        sc.Applicator = new DirectSkillApplicator();
                        sc.TargetSelector = new SelfTargetSelector();
                        sc.SkillEffectChain = new List<ISkillEffect>();
                        sc.SkillEffectChain.Add(new HealEffect());
                        testMS.Components.Add(sc);

                        player.Talents.Add(testMS);
                        player.Out.SendUpdatePlayerSkills();
                        player.Out.SendMessage("Passive ability added.", eChatType.CT_System, eChatLoc.CL_SystemWindow);

                        break;

                    case "pulseheal":

                        testMS = new ModularSkillTalent();
                        var pi = new PulsedInvocation(testMS);
                        testMS.Invocation = pi;
                        sc = new SkillComponent();
                        sc.Applicator = new DirectSkillApplicator();
                        sc.TargetSelector = new SelfTargetSelector();
                        sc.SkillEffectChain = new List<ISkillEffect>();
                        sc.SkillEffectChain.Add(new HealEffect());
                        testMS.Components.Add(sc);
                        player.Talents.Add(testMS);
                        player.Out.SendUpdatePlayerSkills();
                        player.Out.SendMessage("Passive ability added.", eChatType.CT_System, eChatLoc.CL_SystemWindow);

                        break;
                }
            }
        }
    }
}