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
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

using DOL.AI.Brain;
using DOL.Database;
using DOL.Events;
using DOL.GS.Housing;
using DOL.GS.Keeps;
using DOL.GS.PacketHandler;
using DOL.GS.ServerProperties;
using DOL.Language;
using log4net;

namespace DOL.GS.ServerRules
{
	public abstract class AbstractServerRules : IServerRules
	{
		/// <summary>
		/// Defines a logger for this class.
		/// </summary>
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		/// <summary>
		/// This is called after the rules are created to do any event binding or other tasks
		/// </summary>
		/// <param name="e"></param>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		public virtual void Initialize()
		{
			GameEventMgr.AddHandler(GamePlayerEvent.GameEntered, new DOLEventHandler(OnGameEntered));
			GameEventMgr.AddHandler(GamePlayerEvent.RegionChanged, new DOLEventHandler(OnRegionChanged));
			GameEventMgr.AddHandler(GamePlayerEvent.Released, new DOLEventHandler(OnReleased));
			m_invExpiredCallback = new GamePlayer.InvulnerabilityExpiredCallback(ImmunityExpiredCallback);
		}

		/// <summary>
		/// Allows or denies a client from connecting to the server ...
		/// NOTE: The client has not been fully initialized when this method is called.
		/// For example, no account or character data has been loaded yet.
		/// </summary>
		/// <param name="client">The client that sent the login request</param>
		/// <param name="username">The username of the client wanting to connect</param>
		/// <returns>true if connection allowed, false if connection should be terminated</returns>
		/// <remarks>You can only send ONE packet to the client and this is the
		/// LoginDenied packet before returning false. Trying to send any other packet
		/// might result in unexpected behaviour on server and client!</remarks>
		public virtual bool IsAllowedToConnect(GameClient client, string username)
		{
			if (!client.Socket.Connected)
				return false;

			// Ban account
			IList<DBBannedAccount> objs;
			objs = GameServer.Database.SelectObjects<DBBannedAccount>("(`Type` = @TypeA OR `Type` = @TypeB) AND `Account` = @Account", new[] { new QueryParameter("@TypeA", "A"), new QueryParameter("@TypeB", "B"), new QueryParameter("@Account", username) });
			if (objs.Count > 0)
			{
				client.IsConnected = false;
				client.Out.SendLoginDenied(eLoginError.AccountIsBannedFromThisServerType);
				log.Debug("IsAllowedToConnect deny access to username " + username);
				return false;
			}

			// Ban IP Address or range (example: 5.5.5.%)
			string accip = GameServer.Database.Escape(client.TcpEndpointAddress);
			objs = GameServer.Database.SelectObjects<DBBannedAccount>("(`Type` = @TypeI OR `Type` = @TypeB) AND @Ip LIKE `Ip`", new[] { new QueryParameter("@TypeI", "I"), new QueryParameter("@TypeB", "B"), new QueryParameter("@Ip", accip) });
			if (objs.Count > 0)
			{
				client.IsConnected = false;
				client.Out.SendLoginDenied(eLoginError.AccountIsBannedFromThisServerType);
				log.Debug("IsAllowedToConnect deny access to IP " + accip);
				return false;
			}

			GameClient.eClientVersion min = (GameClient.eClientVersion)Properties.CLIENT_VERSION_MIN;
			if (min != GameClient.eClientVersion.VersionNotChecked && client.Version < min)
			{
				client.IsConnected = false;
				client.Out.SendLoginDenied(eLoginError.ClientVersionTooLow);
				log.Debug("IsAllowedToConnect deny access to client version (too low) " + client.Version);
				return false;
			}

			GameClient.eClientVersion max = (GameClient.eClientVersion)Properties.CLIENT_VERSION_MAX;
			if (max != GameClient.eClientVersion.VersionNotChecked && client.Version > max)
			{
				client.IsConnected = false;
				client.Out.SendLoginDenied(eLoginError.NotAuthorizedToUseExpansionVersion);
				log.Debug("IsAllowedToConnect deny access to client version (too high) " + client.Version);
				return false;
			}

			if (Properties.CLIENT_TYPE_MAX > -1)
			{
				GameClient.eClientType type = (GameClient.eClientType)Properties.CLIENT_TYPE_MAX;
				if ((int)client.ClientType > (int)type)
				{
					client.IsConnected = false;
					client.Out.SendLoginDenied(eLoginError.ExpansionPacketNotAllowed);
					log.Debug("IsAllowedToConnect deny access to expansion pack.");
					return false;
				}
			}

			/* Example to limit the connections from a certain IP range!
			if(client.Socket.RemoteEndPoint.ToString().StartsWith("192.168.0."))
			{
				client.Out.SendLoginDenied(eLoginError.AccountNoAccessAnyGame);
				return false;
			}
			 */


			/* Example to deny new connections on saturdays
			if(DateTime.Now.DayOfWeek == DayOfWeek.Saturday)
			{
				client.Out.SendLoginDenied(eLoginError.GameCurrentlyClosed);
				return false;
			}
			 */

			/* Example to deny new connections between 10am and 12am
			if(DateTime.Now.Hour >= 10 && DateTime.Now.Hour <= 12)
			{
				client.Out.SendLoginDenied(eLoginError.GameCurrentlyClosed);
				return false;
			}
			 */

			Account account = GameServer.Database.FindObjectByKey<Account>(username);

			if (Properties.MAX_PLAYERS > 0)
			{
				if (WorldMgr.GetAllClients().Count >= Properties.MAX_PLAYERS)
				{
					// GMs are still allowed to enter server
					if (account == null || (account.PrivLevel == 1 && account.Status <= 0))
					{
						// Normal Players will not be allowed over the max
						client.IsConnected = false;
						client.Out.SendLoginDenied(eLoginError.TooManyPlayersLoggedIn);
						log.Debug("IsAllowedToConnect deny access due to too many players.");
						return false;
					}

				}
			}

			if (Properties.STAFF_LOGIN)
			{
				if (account == null || account.PrivLevel == 1)
				{
					// GMs are still allowed to enter server
					// Normal Players will not be allowed to Log in
					client.IsConnected = false;
					client.Out.SendLoginDenied(eLoginError.GameCurrentlyClosed);
					log.Debug("IsAllowedToConnect deny access; staff only login");
					return false;
				}
			}

			if (!Properties.ALLOW_DUAL_LOGINS)
			{
				if ((account == null || account.PrivLevel == 1) && client.TcpEndpointAddress != "not connected")
				{
					foreach (GameClient cln in WorldMgr.GetAllClients())
					{
						if (cln == null || client == cln) continue;
						if (cln.TcpEndpointAddress == client.TcpEndpointAddress)
						{
							if (cln.Account != null && cln.Account.PrivLevel > 1)
							{
								break;
							}
							client.IsConnected = false;
							client.Out.SendLoginDenied(eLoginError.AccountAlreadyLoggedIntoOtherServer);
							log.Debug("IsAllowedToConnect deny access; dual login not allowed");
							return false;
						}
					}
				}
			}

			return true;
		}


		/// <summary>
		/// Called when player enters the game for first time
		/// </summary>
		/// <param name="e">event</param>
		/// <param name="sender">GamePlayer object that has entered the game</param>
		/// <param name="args"></param>
		public virtual void OnGameEntered(DOLEvent e, object sender, EventArgs args)
		{
			StartImmunityTimer((GamePlayer)sender, ServerProperties.Properties.TIMER_GAME_ENTERED * 1000);
		}

		/// <summary>
		/// Called when player has changed the region
		/// </summary>
		/// <param name="e">event</param>
		/// <param name="sender">GamePlayer object that has changed the region</param>
		/// <param name="args"></param>
		public virtual void OnRegionChanged(DOLEvent e, object sender, EventArgs args)
		{
			StartImmunityTimer((GamePlayer)sender, ServerProperties.Properties.TIMER_REGION_CHANGED * 1000);
		}

		/// <summary>
		/// Called after player has released
		/// </summary>
		/// <param name="e">event</param>
		/// <param name="sender">GamePlayer that has released</param>
		/// <param name="args"></param>
		public virtual void OnReleased(DOLEvent e, object sender, EventArgs args)
		{
			GamePlayer player = (GamePlayer)sender;
			StartImmunityTimer(player, ServerProperties.Properties.TIMER_KILLED_BY_MOB * 1000);//When Killed by a Mob
		}

		/// <summary>
		/// Should be called whenever a player teleports to a new location
		/// </summary>
		/// <param name="player"></param>
		/// <param name="source"></param>
		/// <param name="destination"></param>
		public virtual void OnPlayerTeleport(GamePlayer player, GameLocation source, Teleport destination)
		{
			// override this in order to do something, like set immunity, when a player teleports
		}

		/// <summary>
		/// Starts the immunity timer for a player
		/// </summary>
		/// <param name="player">player that gets immunity</param>
		/// <param name="duration">amount of milliseconds when immunity ends</param>
		public virtual void StartImmunityTimer(GamePlayer player, int duration)
		{
			if (duration > 0)
			{
				player.StartInvulnerabilityTimer(duration, m_invExpiredCallback);
			}
		}

		/// <summary>
		/// Holds the delegate called when PvP invulnerability is expired
		/// </summary>
		protected GamePlayer.InvulnerabilityExpiredCallback m_invExpiredCallback;

		/// <summary>
		/// Removes immunity from the players
		/// </summary>
		/// <player></player>
		public virtual void ImmunityExpiredCallback(GamePlayer player)
		{
			if (player.ObjectState != GameObject.eObjectState.Active) return;
			if (player.Client.IsPlaying == false) return;

			player.Out.SendMessage("Your temporary invulnerability timer has expired.", eChatType.CT_System, eChatLoc.CL_SystemWindow);

			return;
		}


		public abstract bool IsSameRealm(GameLiving source, GameLiving target, bool quiet);
		public abstract bool IsAllowedCharsInAllRealms(GameClient client);
		public abstract bool IsAllowedToGroup(GamePlayer source, GamePlayer target, bool quiet);
		public abstract bool IsAllowedToJoinGuild(GamePlayer source, Guild guild);
		public abstract bool IsAllowedToTrade(GameLiving source, GameLiving target, bool quiet);
		public abstract bool IsAllowedToUnderstand(GameLiving source, GamePlayer target);
		public abstract string RulesDescription();

		public virtual bool IsAllowedToMoveToBind(GamePlayer player)
		{
			return true;
		}

		public virtual bool CountsTowardsSlashLevel(DOLCharacters player)
		{
			return true;
		}

		/// <summary>
		/// Is attacker allowed to attack defender.
		/// </summary>
		/// <param name="attacker">living that makes attack</param>
		/// <param name="defender">attacker's target</param>
		/// <param name="quiet">should messages be sent</param>
		/// <returns>true if attack is allowed</returns>
		public virtual bool IsAllowedToAttack(GameLiving attacker, GameLiving defender, bool quiet)
		{
			if (attacker == null || defender == null)
				return false;

			//dead things can't attack
			if (!defender.IsAlive || !attacker.IsAlive)
				return false;

			GamePlayer playerAttacker = attacker as GamePlayer;
			GamePlayer playerDefender = defender as GamePlayer;

			// if Pet, let's define the controller once
			if (defender is GameNPC)
				if ((defender as GameNPC).Brain is IControlledBrain)
					playerDefender = ((defender as GameNPC).Brain as IControlledBrain).GetPlayerOwner();

			if (attacker is GameNPC)
				if ((attacker as GameNPC).Brain is IControlledBrain)
					playerAttacker = ((attacker as GameNPC).Brain as IControlledBrain).GetPlayerOwner();

			if (playerDefender != null && (playerDefender.Client.ClientState == GameClient.eClientState.WorldEnter || playerDefender.IsInvulnerableToAttack))
			{
				if (!quiet)
					MessageToLiving(attacker, defender.Name + " is entering the game and is temporarily immune to PvP attacks!");
				return false;
			}

			if (playerAttacker != null && playerDefender != null)
			{
				// Attacker immunity
				if (playerAttacker.IsInvulnerableToAttack)
				{
					if (quiet == false) MessageToLiving(attacker, "You can't attack players until your PvP invulnerability timer wears off!");
					return false;
				}

				// Defender immunity
				if (playerDefender.IsInvulnerableToAttack)
				{
					if (quiet == false) MessageToLiving(attacker, defender.Name + " is temporarily immune to PvP attacks!");
					return false;
				}
			}

			// PEACE NPCs can't be attacked/attack
			if (attacker is GameNPC)
				if ((((GameNPC)attacker).Flags & GameNPC.eFlags.PEACE) != 0)
					return false;
			if (defender is GameNPC)
				if ((((GameNPC)defender).Flags & GameNPC.eFlags.PEACE) != 0)
					return false;
			// Players can't attack mobs while they have immunity
			if (playerAttacker != null && defender != null)
			{
				if ((defender is GameNPC) && (playerAttacker.IsInvulnerableToAttack))
				{
					if (quiet == false) MessageToLiving(attacker, "You can't attack until your PvP invulnerability timer wears off!");
					return false;
				}
			}
			// Your pet can only attack stealthed players you have selected
			if (defender.IsStealthed && attacker is GameNPC)
				if (((attacker as GameNPC).Brain is IControlledBrain) &&
					defender is GamePlayer &&
					playerAttacker.TargetObject != defender)
					return false;

			// GMs can't be attacked
			if (playerDefender != null && playerDefender.Client.Account.PrivLevel > 1)
				return false;

			// Safe area support for defender
			foreach (AbstractArea area in defender.CurrentAreas)
			{
				if (!area.IsSafeArea)
					continue;

				if (defender is GamePlayer)
				{
					if (quiet == false) MessageToLiving(attacker, "You can't attack someone in a safe area!");
					return false;
				}
			}

			//safe area support for attacker
			foreach (AbstractArea area in attacker.CurrentAreas)
			{
				if ((area.IsSafeArea) && (defender is GamePlayer) && (attacker is GamePlayer))
				{
					if (quiet == false) MessageToLiving(attacker, "You can't attack someone in a safe area!");
					return false;
				}
			}

			//I don't want mobs attacking guards
			if (defender is GameKeepGuard && attacker is GameNPC && attacker.Realm == 0)
				return false;

			return true;
		}

		/// <summary>
		/// Is caster allowed to cast a spell
		/// </summary>
		/// <param name="caster"></param>
		/// <param name="target"></param>
		/// <param name="spell"></param>
		/// <param name="spellLine"></param>
		/// <returns>true if allowed</returns>
		public virtual bool IsAllowedToCastSpell(GameLiving caster, GameLiving target, Spell spell, SpellLine spellLine)
		{
			//we only allow certain spell targets to be cast when targeting a keep component
			//tolakram - live allows most damage spells to be cast on doors. This should be handled in spell handlers
			if (target is GameKeepComponent || target is GameKeepDoor)
			{
				bool isAllowed = false;

				switch (spell.Target.ToLower())
				{
					case "self":
					case "group":
					case "pet":
					case "controlled":
					case "realm":
					case "area":
						isAllowed = true;
						break;

					case "enemy":

						if (spell.Radius == 0)
						{
							switch (spell.SpellType.ToLower())
							{
								case "archery":
								case "bolt":
								case "bomber":
								case "damagespeeddecrease":
								case "directdamage":
								case "magicalstrike":
								case "siegearrow":
								case "summontheurgistpet":
								case "directdamagewithdebuff":
									isAllowed = true;
									break;
							}
						}

						// pbaoe
						if (spell.Radius > 0 && spell.Range == 0)
						{
							isAllowed = true;
						}

						break;
				}

				if (!isAllowed && caster is GamePlayer)
					(caster as GamePlayer).Client.Out.SendMessage("You can't cast this spell on the " + target.Name, eChatType.CT_System, eChatLoc.CL_SystemWindow);

				return isAllowed;
			}



			return true;
		}

		public virtual bool IsAllowedToSpeak(GamePlayer source, string communicationType)
		{
			if (source.IsAlive == false)
			{
				MessageToLiving(source, "Hmmmm...you can't " + communicationType + " while dead!");
				return false;
			}
			return true;
		}

		/// <summary>
		/// Is player allowed to bind
		/// </summary>
		/// <param name="player"></param>
		/// <param name="point"></param>
		/// <returns></returns>
		public virtual bool IsAllowedToBind(GamePlayer player, BindPoint point)
		{
			return true;
		}

		/// <summary>
		/// Is player allowed to make the item
		/// </summary>
		/// <param name="player"></param>
		/// <param name="item"></param>
		/// <returns></returns>
		public virtual bool IsAllowedToCraft(GamePlayer player, ItemTemplate item)
		{
			return true;
		}

		/// <summary>
		/// Is player allowed to claim in this region
		/// </summary>
		/// <param name="player"></param>
		/// <param name="region"></param>
		/// <returns></returns>
		public virtual bool IsAllowedToClaim(GamePlayer player, Region region)
		{
			if (region.IsInstance)
			{
				return false;
			}

			return true;
		}

		public virtual bool IsAllowedToZone(GamePlayer player, Region region)
		{
			return true;
		}

		/// <summary>
		/// is player allowed to ride his personal mount ?
		/// </summary>
		/// <param name="player"></param>
		/// <returns>string representing why player is not allowed to mount, else empty string</returns>
		public virtual string ReasonForDisallowMounting(GamePlayer player)
		{
			// pre conditions
			if (!player.IsAlive) return "GamePlayer.UseSlot.CantMountWhileDead";
			if (player.Steed != null) return "GamePlayer.UseSlot.MustDismountBefore";

			// gm/admin overrides the other checks
			if (player.Client.Account.PrivLevel != (uint)ePrivLevel.Player) return string.Empty;

			// player restrictions
			if (player.IsMoving) return "GamePlayer.UseSlot.CantMountMoving";
			if (player.InCombat) return "GamePlayer.UseSlot.CantMountCombat";
			if (player.IsSitting) return "GamePlayer.UseSlot.CantCallMountSeated";
			if (player.IsStealthed) return "GamePlayer.UseSlot.CantMountStealthed";

			// You are carrying a relic ? You can't use a mount !
			if (GameRelic.IsPlayerCarryingRelic(player))
				return "GamePlayer.UseSlot.CantMountRelicCarrier";

			// zones checks:
			// white list: always allows
			string currentRegion = player.CurrentRegion.ID.ToString();
			if (ServerProperties.Properties.ALLOW_PERSONNAL_MOUNT_IN_REGIONS.Contains(currentRegion))
			{
				var regions = ServerProperties.Properties.ALLOW_PERSONNAL_MOUNT_IN_REGIONS.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
				foreach (var region in regions)
					if (region == currentRegion)
						return string.Empty;
			}

			// restrictions: dungeons, instances, capitals, rvr horses
			if (player.CurrentRegion.IsDungeon ||
				player.CurrentRegion.IsInstance ||
				player.CurrentRegion.IsCapitalCity)
				return "GamePlayer.UseSlot.CantMountHere";
			// perhaps need to be tweaked for PvPServerRules
			if (player.CurrentRegion.IsRvR && !player.ActiveHorse.IsSummonRvR)
				return "GamePlayer.UseSlot.CantSummonRvR";

			// sounds good !
			return string.Empty;

		}

		public virtual bool CanTakeFallDamage(GamePlayer player)
		{
			if (player.Client.Account.PrivLevel > 1)
				return false;

			if (player.CurrentRegion.IsHousing)
				return false; // Workaround: falling from houses should not produce damage

			return true;
		}

		public virtual long GetExperienceForLiving(int level)
		{
			level = (level < 0) ? 0 : level;

			// use exp table
			if (level < GameLiving.XPForLiving.Length)
				return GameLiving.XPForLiving[level];

			// use formula if level is not in exp table
			// long can hold values up to level 238
			if (level > 238)
				level = 238;

			double k1, k1_inc, k1_lvl;

			// noret: using these rules i was able to reproduce table from
			// http://www.daocweave.com/daoc/general/experience_table.htm
			if (level >= 35)
			{
				k1_lvl = 35;
				k1_inc = 0.2;
				k1 = 20;
			}
			else if (level >= 20)
			{
				k1_lvl = 20;
				k1_inc = 0.3334;
				k1 = 15;
			}
			else if (level >= 10)
			{
				k1_lvl = 10;
				k1_inc = 0.5;
				k1 = 10;
			}
			else
			{
				k1_lvl = 0;
				k1_inc = 1;
				k1 = 0;
			}

			long exp = (long)(Math.Pow(2, k1 + (level - k1_lvl) * k1_inc) * 5);
			if (exp < 0)
			{
				exp = 0;
			}

			return exp;
		}

		// Can a character use this item?
		public virtual bool CheckAbilityToUseItem(GameLiving living, ItemTemplate item)
		{
			if (living == null || item == null)
				return false;

			GamePlayer player = living as GamePlayer;

			// GMs can equip everything
			if (player != null && player.Client.Account.PrivLevel > (uint)ePrivLevel.Player)
				return true;

			// allow usage of all house items
			if ((item.Object_Type == 0 || item.Object_Type >= (int)eObjectType._FirstHouse) && item.Object_Type <= (int)eObjectType._LastHouse)
				return true;

			// on some servers we may wish for dropped items to be used by all realms regardless of what is set in the db
			if (!ServerProperties.Properties.ALLOW_CROSS_REALM_ITEMS)
			{
				if (item.Realm != 0 && item.Realm != (int)living.Realm)
					return false;
			}

			// classes restriction. 0 means every class
			if (player != null && !Util.IsEmpty(item.AllowedClasses, true))
			{
				if (!item.AllowedClasses.SplitCSV(true).Contains(player.CharacterClass.ID.ToString()))
					return false;
			}

			//armor
			if (item.Object_Type >= (int)eObjectType._FirstArmor && item.Object_Type <= (int)eObjectType._LastArmor)
			{
				int armorAbility = -1;

				if (ServerProperties.Properties.ALLOW_CROSS_REALM_ITEMS && item.Item_Type != (int)eEquipmentItems.HEAD)
				{
					switch (player.Realm) // Choose based on player rather than item region
					{
						case eRealm.Albion: armorAbility = living.GetAbilityLevel(Abilities.AlbArmor); break;
						case eRealm.Hibernia: armorAbility = living.GetAbilityLevel(Abilities.HibArmor); break;
						case eRealm.Midgard: armorAbility =  living.GetAbilityLevel(Abilities.MidArmor); break;
						default: break;
					}
				}
				else
				{
					switch ((eRealm)item.Realm)
					{
						case eRealm.Albion: armorAbility = living.GetAbilityLevel(Abilities.AlbArmor); break;
						case eRealm.Hibernia: armorAbility = living.GetAbilityLevel(Abilities.HibArmor); break;
						case eRealm.Midgard: armorAbility = living.GetAbilityLevel(Abilities.MidArmor); break;
						default: // use old system
							armorAbility = Math.Max(armorAbility, living.GetAbilityLevel(Abilities.AlbArmor));
							armorAbility = Math.Max(armorAbility, living.GetAbilityLevel(Abilities.HibArmor));
							armorAbility = Math.Max(armorAbility, living.GetAbilityLevel(Abilities.MidArmor));
							break;
					}
				}
				switch ((eObjectType)item.Object_Type)
				{
					case eObjectType.GenericArmor: return armorAbility >= ArmorLevel.GenericArmor;
					case eObjectType.Cloth: return armorAbility >= ArmorLevel.Cloth;
					case eObjectType.Leather: return armorAbility >= ArmorLevel.Leather;
					case eObjectType.Reinforced:
					case eObjectType.Studded: return armorAbility >= ArmorLevel.Studded;
					case eObjectType.Scale:
					case eObjectType.Chain: return armorAbility >= ArmorLevel.Chain;
					case eObjectType.Plate: return armorAbility >= ArmorLevel.Plate;
					default: return false;
				}
			}

			// non-armors
			string abilityCheck = null;
			string[] otherCheck = new string[0];

			//http://dol.kitchenhost.de/files/dol/Info/itemtable.txt
			switch ((eObjectType)item.Object_Type)
			{
				case eObjectType.GenericItem: return true;
				case eObjectType.GenericArmor: return true;
				case eObjectType.GenericWeapon: return true;
				case eObjectType.Staff: abilityCheck = Abilities.Weapon_Staves; break;
				case eObjectType.Fired: abilityCheck = Abilities.Weapon_Shortbows; break;
				case eObjectType.FistWraps: abilityCheck = Abilities.Weapon_FistWraps; break;
				case eObjectType.MaulerStaff: abilityCheck = Abilities.Weapon_MaulerStaff; break;

				//alb
				case eObjectType.CrushingWeapon:
					if (ServerProperties.Properties.ALLOW_CROSS_REALM_ITEMS)
						switch (living.Realm)
						{
							case eRealm.Albion: abilityCheck = Abilities.Weapon_Crushing; break;
							case eRealm.Hibernia: abilityCheck = Abilities.Weapon_Blunt; break;
							case eRealm.Midgard: abilityCheck = Abilities.Weapon_Hammers; break;
							default: break;
						} 
					else abilityCheck = Abilities.Weapon_Crushing;
					break;
				case eObjectType.SlashingWeapon:
					if (ServerProperties.Properties.ALLOW_CROSS_REALM_ITEMS)
						switch (living.Realm)
						{
							case eRealm.Albion: abilityCheck = Abilities.Weapon_Slashing; break;
							case eRealm.Hibernia: abilityCheck = Abilities.Weapon_Blades; break;
							case eRealm.Midgard: abilityCheck = Abilities.Weapon_Swords; break;
							default: break;
						}
					else abilityCheck = Abilities.Weapon_Slashing;
					break;
				case eObjectType.ThrustWeapon:
					if (ServerProperties.Properties.ALLOW_CROSS_REALM_ITEMS && living.Realm == eRealm.Hibernia)
						abilityCheck = Abilities.Weapon_Piercing;
					else
						abilityCheck = Abilities.Weapon_Thrusting;
					break;
				case eObjectType.TwoHandedWeapon:
					if (ServerProperties.Properties.ALLOW_CROSS_REALM_ITEMS && living.Realm == eRealm.Hibernia)
						abilityCheck = Abilities.Weapon_LargeWeapons;
					else abilityCheck = Abilities.Weapon_TwoHanded;
					break;
				case eObjectType.PolearmWeapon:
					if (ServerProperties.Properties.ALLOW_CROSS_REALM_ITEMS)
						switch (living.Realm)
						{
							case eRealm.Albion: abilityCheck = Abilities.Weapon_Polearms; break;
							case eRealm.Hibernia: abilityCheck = Abilities.Weapon_CelticSpear; break;
							case eRealm.Midgard: abilityCheck = Abilities.Weapon_Spears; break;
							default: break;
						}
					else abilityCheck = Abilities.Weapon_Polearms;
					break;
				case eObjectType.Longbow:
					otherCheck = new string[] { Abilities.Weapon_Longbows, Abilities.Weapon_Archery };
					break;
				case eObjectType.Crossbow: abilityCheck = Abilities.Weapon_Crossbow; break;
				case eObjectType.Flexible: abilityCheck = Abilities.Weapon_Flexible; break;
				//TODO: case 5: abilityCheck = Abilities.Weapon_Thrown;break;

				//mid
				case eObjectType.Sword:
					if (ServerProperties.Properties.ALLOW_CROSS_REALM_ITEMS)
						switch (living.Realm)
						{
							case eRealm.Albion: abilityCheck = Abilities.Weapon_Slashing; break;
							case eRealm.Hibernia: abilityCheck = Abilities.Weapon_Blades; break;
							case eRealm.Midgard: abilityCheck = Abilities.Weapon_Swords; break;
							default: break;
						}
					else abilityCheck = Abilities.Weapon_Swords; 
					break;
				case eObjectType.Hammer:
					if (ServerProperties.Properties.ALLOW_CROSS_REALM_ITEMS)
						switch (living.Realm)
						{
							case eRealm.Albion: abilityCheck = Abilities.Weapon_Crushing; break;
							case eRealm.Midgard: abilityCheck = Abilities.Weapon_Hammers; break;
							case eRealm.Hibernia: abilityCheck = Abilities.Weapon_Blunt; break;
							default: break;
						}
					else abilityCheck = Abilities.Weapon_Hammers; 
					break;
				case eObjectType.LeftAxe:
				case eObjectType.Axe:
					if (ServerProperties.Properties.ALLOW_CROSS_REALM_ITEMS)
						switch (living.Realm)
						{
							case eRealm.Albion: abilityCheck = Abilities.Weapon_Slashing; break;
							case eRealm.Hibernia: abilityCheck = Abilities.Weapon_Blades; break;
							case eRealm.Midgard: abilityCheck = Abilities.Weapon_Axes; break;
							default: break;
						}
					else abilityCheck = Abilities.Weapon_Axes; 
					break;
				case eObjectType.Spear:
					if (ServerProperties.Properties.ALLOW_CROSS_REALM_ITEMS)
						switch (living.Realm)
						{
							case eRealm.Albion: abilityCheck = Abilities.Weapon_Polearms; break;
							case eRealm.Hibernia: abilityCheck = Abilities.Weapon_CelticSpear; break;
							case eRealm.Midgard: abilityCheck = Abilities.Weapon_Spears; break;
							default: break;
						}
					else abilityCheck = Abilities.Weapon_Spears; 
					break;
				case eObjectType.CompositeBow:
					otherCheck = new string[] { Abilities.Weapon_CompositeBows, Abilities.Weapon_Archery };
					break;
				case eObjectType.Thrown: abilityCheck = Abilities.Weapon_Thrown; break;
				case eObjectType.HandToHand: abilityCheck = Abilities.Weapon_HandToHand; break;

				//hib
				case eObjectType.RecurvedBow:
					otherCheck = new string[] { Abilities.Weapon_RecurvedBows, Abilities.Weapon_Archery };
					break;
				case eObjectType.Blades:
					if (ServerProperties.Properties.ALLOW_CROSS_REALM_ITEMS)
						switch (living.Realm)
						{
							case eRealm.Albion: abilityCheck = Abilities.Weapon_Slashing; break;
							case eRealm.Hibernia: abilityCheck = Abilities.Weapon_Blades; break;
							case eRealm.Midgard: abilityCheck = Abilities.Weapon_Swords; break;
							default: break;
						}
					else abilityCheck = Abilities.Weapon_Blades; 
					break;
				case eObjectType.Blunt:
					if (ServerProperties.Properties.ALLOW_CROSS_REALM_ITEMS)
						switch (living.Realm)
						{
							case eRealm.Albion: abilityCheck = Abilities.Weapon_Crushing; break;
							case eRealm.Hibernia: abilityCheck = Abilities.Weapon_Blunt; break;
							case eRealm.Midgard: abilityCheck = Abilities.Weapon_Hammers; break;
							default: break;
						}
					else abilityCheck = Abilities.Weapon_Blunt;
					break;
				case eObjectType.Piercing:
					if (ServerProperties.Properties.ALLOW_CROSS_REALM_ITEMS && living.Realm == eRealm.Albion)
						abilityCheck = Abilities.Weapon_Thrusting;
					else abilityCheck = Abilities.Weapon_Piercing;
					break;
				case eObjectType.LargeWeapons:
					if (ServerProperties.Properties.ALLOW_CROSS_REALM_ITEMS && living.Realm == eRealm.Albion)
						abilityCheck = Abilities.Weapon_TwoHanded;
					else abilityCheck = Abilities.Weapon_LargeWeapons; break;
				case eObjectType.CelticSpear:
					if (ServerProperties.Properties.ALLOW_CROSS_REALM_ITEMS)
						switch (living.Realm)
						{
							case eRealm.Albion: abilityCheck = Abilities.Weapon_Polearms; break;
							case eRealm.Hibernia: abilityCheck = Abilities.Weapon_CelticSpear; break;
							case eRealm.Midgard: abilityCheck = Abilities.Weapon_Spears; break;
							default: break;
						}
					else abilityCheck = Abilities.Weapon_CelticSpear;
					break;
				case eObjectType.Scythe: abilityCheck = Abilities.Weapon_Scythe; break;

				//misc
				case eObjectType.Magical: return true;
				case eObjectType.Shield: return living.GetAbilityLevel(Abilities.Shield) >= item.Type_Damage;
				case eObjectType.Bolt: abilityCheck = Abilities.Weapon_Crossbow; break;
				case eObjectType.Arrow: otherCheck = new string[] { Abilities.Weapon_CompositeBows, Abilities.Weapon_Longbows, Abilities.Weapon_RecurvedBows, Abilities.Weapon_Shortbows }; break;
				case eObjectType.Poison: return living.GetModifiedSpecLevel(Specs.Envenom) > 0;
				case eObjectType.Instrument: return living.HasAbility(Abilities.Weapon_Instruments);
					//TODO: different shield sizes
			}

			if (abilityCheck != null && living.HasAbility(abilityCheck))
				return true;

			foreach (string str in otherCheck)
				if (living.HasAbility(str))
					return true;

			return false;
		}

		/// <summary>
		/// Get object specialization level based on server type
		/// </summary>
		/// <param name="player">player whom specializations are checked</param>
		/// <param name="objectType">object type</param>
		/// <returns>specialization in object or 0</returns>
		public virtual int GetObjectSpecLevel(GamePlayer player, eObjectType objectType)
		{
			int res = 0;

			foreach (eObjectType obj in GetCompatibleObjectTypes(objectType))
			{
				int spec = player.GetModifiedSpecLevel(SkillBase.ObjectTypeToSpec(obj));
				if (res < spec)
					res = spec;
			}
			return res;
		}

		/// <summary>
		/// Get object specialization level based on server type
		/// </summary>
		/// <param name="player">player whom specializations are checked</param>
		/// <param name="objectType">object type</param>
		/// <returns>specialization in object or 0</returns>
		public virtual int GetBaseObjectSpecLevel(GamePlayer player, eObjectType objectType)
		{
			int res = 0;

			foreach (eObjectType obj in GetCompatibleObjectTypes(objectType))
			{
				int spec = player.GetBaseSpecLevel(SkillBase.ObjectTypeToSpec(obj));
				if (res < spec)
					res = spec;
			}
			return res;
		}

		/// <summary>
		/// Checks whether one object type is equal to another
		/// based on server type
		/// </summary>
		/// <param name="type1"></param>
		/// <param name="type2"></param>
		/// <returns>true if equals</returns>
		public virtual bool IsObjectTypesEqual(eObjectType type1, eObjectType type2)
		{
			foreach (eObjectType obj in GetCompatibleObjectTypes(type1))
			{
				if (obj == type2)
					return true;
			}
			return false;
		}

		#region GetCompatibleObjectTypes

		/// <summary>
		/// Holds arrays of compatible object types
		/// </summary>
		protected Hashtable m_compatibleObjectTypes = null;

		/// <summary>
		/// Translates object type to compatible object types based on server type
		/// </summary>
		/// <param name="objectType">The object type</param>
		/// <returns>An array of compatible object types</returns>
		protected virtual eObjectType[] GetCompatibleObjectTypes(eObjectType objectType)
		{
			if (m_compatibleObjectTypes == null)
			{
				m_compatibleObjectTypes = new Hashtable();
				m_compatibleObjectTypes[(int)eObjectType.Staff] = new eObjectType[] { eObjectType.Staff };
				m_compatibleObjectTypes[(int)eObjectType.Fired] = new eObjectType[] { eObjectType.Fired };

				m_compatibleObjectTypes[(int)eObjectType.FistWraps] = new eObjectType[] { eObjectType.FistWraps };
				m_compatibleObjectTypes[(int)eObjectType.MaulerStaff] = new eObjectType[] { eObjectType.MaulerStaff };

				//alb
				m_compatibleObjectTypes[(int)eObjectType.CrushingWeapon] = new eObjectType[] { eObjectType.CrushingWeapon, eObjectType.Blunt, eObjectType.Hammer };
				m_compatibleObjectTypes[(int)eObjectType.SlashingWeapon] = new eObjectType[] { eObjectType.SlashingWeapon, eObjectType.Blades, eObjectType.Sword, eObjectType.Axe };
				m_compatibleObjectTypes[(int)eObjectType.ThrustWeapon] = new eObjectType[] { eObjectType.ThrustWeapon, eObjectType.Piercing };
				m_compatibleObjectTypes[(int)eObjectType.TwoHandedWeapon] = new eObjectType[] { eObjectType.TwoHandedWeapon, eObjectType.LargeWeapons };
				m_compatibleObjectTypes[(int)eObjectType.PolearmWeapon] = new eObjectType[] { eObjectType.PolearmWeapon, eObjectType.CelticSpear, eObjectType.Spear };
				m_compatibleObjectTypes[(int)eObjectType.Flexible] = new eObjectType[] { eObjectType.Flexible };
				m_compatibleObjectTypes[(int)eObjectType.Longbow] = new eObjectType[] { eObjectType.Longbow };
				m_compatibleObjectTypes[(int)eObjectType.Crossbow] = new eObjectType[] { eObjectType.Crossbow };
				//TODO: case 5: abilityCheck = Abilities.Weapon_Thrown; break;

				//mid
				m_compatibleObjectTypes[(int)eObjectType.Hammer] = new eObjectType[] { eObjectType.Hammer, eObjectType.CrushingWeapon, eObjectType.Blunt };
				m_compatibleObjectTypes[(int)eObjectType.Sword] = new eObjectType[] { eObjectType.Sword, eObjectType.SlashingWeapon, eObjectType.Blades };
				m_compatibleObjectTypes[(int)eObjectType.LeftAxe] = new eObjectType[] { eObjectType.LeftAxe };
				m_compatibleObjectTypes[(int)eObjectType.Axe] = new eObjectType[] { eObjectType.Axe, eObjectType.SlashingWeapon, eObjectType.Blades, eObjectType.LeftAxe };
				m_compatibleObjectTypes[(int)eObjectType.HandToHand] = new eObjectType[] { eObjectType.HandToHand };
				m_compatibleObjectTypes[(int)eObjectType.Spear] = new eObjectType[] { eObjectType.Spear, eObjectType.CelticSpear, eObjectType.PolearmWeapon };
				m_compatibleObjectTypes[(int)eObjectType.CompositeBow] = new eObjectType[] { eObjectType.CompositeBow };
				m_compatibleObjectTypes[(int)eObjectType.Thrown] = new eObjectType[] { eObjectType.Thrown };

				//hib
				m_compatibleObjectTypes[(int)eObjectType.Blunt] = new eObjectType[] { eObjectType.Blunt, eObjectType.CrushingWeapon, eObjectType.Hammer };
				m_compatibleObjectTypes[(int)eObjectType.Blades] = new eObjectType[] { eObjectType.Blades, eObjectType.SlashingWeapon, eObjectType.Sword, eObjectType.Axe };
				m_compatibleObjectTypes[(int)eObjectType.Piercing] = new eObjectType[] { eObjectType.Piercing, eObjectType.ThrustWeapon };
				m_compatibleObjectTypes[(int)eObjectType.LargeWeapons] = new eObjectType[] { eObjectType.LargeWeapons, eObjectType.TwoHandedWeapon };
				m_compatibleObjectTypes[(int)eObjectType.CelticSpear] = new eObjectType[] { eObjectType.CelticSpear, eObjectType.Spear, eObjectType.PolearmWeapon };
				m_compatibleObjectTypes[(int)eObjectType.Scythe] = new eObjectType[] { eObjectType.Scythe };
				m_compatibleObjectTypes[(int)eObjectType.RecurvedBow] = new eObjectType[] { eObjectType.RecurvedBow };

				m_compatibleObjectTypes[(int)eObjectType.Shield] = new eObjectType[] { eObjectType.Shield };
				m_compatibleObjectTypes[(int)eObjectType.Poison] = new eObjectType[] { eObjectType.Poison };
				//TODO: case 45: abilityCheck = Abilities.instruments; break;
			}

			eObjectType[] res = (eObjectType[])m_compatibleObjectTypes[(int)objectType];
			if (res == null)
				return new eObjectType[0];
			return res;
		}

		#endregion

		/// <summary>
		/// Invoked on NPC death and deals out
		/// experience/realm points if needed
		/// </summary>
		/// <param name="killedNPC">npc that died</param>
		/// <param name="killer">killer</param>
		public virtual void OnNPCKilled(GameNPC killedNPC, GameObject killer)
		{
		}

		/// <summary>
		/// Called on living death that is not gameplayer or gamenpc
		/// </summary>
		/// <param name="killedLiving">The living object</param>
		public virtual void OnLivingDied(GameLiving killedLiving)
		{
		}

		/// <summary>
		/// Invoked on Player death and deals out
		/// experience/realm points if needed
		/// </summary>
		/// <param name="killedPlayer">player that died</param>
		/// <param name="killer">killer</param>
		public virtual void OnPlayerKilled(GamePlayer killedPlayer, GameObject killer)
		{
			if (ServerProperties.Properties.ENABLE_WARMAPMGR && killer is GamePlayer && killer.CurrentRegion.ID == 163)
				WarMapMgr.AddFight((byte)killer.CurrentZone.ID, killer.X, killer.Y, (byte)killer.Realm, (byte)killedPlayer.Realm);

			killedPlayer.LastDeathRealmPoints = 0;
		}

		/// <summary>
		/// Gets the Realm of an living for name text coloring
		/// </summary>
		/// <param name="player"></param>
		/// <param name="target"></param>
		/// <returns>byte code of realm</returns>
		public virtual byte GetLivingRealm(GamePlayer player, GameLiving target)
		{
			if (player == null || target == null) return 0;

			// clients with priv level > 1 are considered friendly by anyone
			GamePlayer playerTarget = target as GamePlayer;
			if (playerTarget != null && playerTarget.Client.Account.PrivLevel > 1) return (byte)player.Realm;

			return (byte)target.Realm;
		}

		/// <summary>
		/// Gets the player name based on server type
		/// </summary>
		/// <param name="source">The "looking" player</param>
		/// <param name="target">The considered player</param>
		/// <returns>The name of the target</returns>
		public virtual string GetPlayerName(GamePlayer source, GamePlayer target)
		{
			return target.Name;
		}

		/// <summary>
		/// Gets the player Realmrank 12 or 13 title
		/// </summary>
		/// <param name="source">The "looking" player</param>
		/// <param name="target">The considered player</param>
		/// <returns>The Realmranktitle of the target</returns>
		public virtual string GetPlayerPrefixName(GamePlayer source, GamePlayer target)
		{
			string language;

			try
			{
				language = source.Client.Account.Language;
			}
			catch
			{
				language = LanguageMgr.DefaultLanguage;
			}

			if (IsSameRealm(source, target, true) && target.RealmLevel >= 110)
				return target.RealmRankTitle(language);

			return string.Empty;
		}

		/// <summary>
		/// Gets the player last name based on server type
		/// </summary>
		/// <param name="source">The "looking" player</param>
		/// <param name="target">The considered player</param>
		/// <returns>The last name of the target</returns>
		public virtual string GetPlayerLastName(GamePlayer source, GamePlayer target)
		{
			return target.LastName;
		}

		/// <summary>
		/// Gets the player guild name based on server type
		/// </summary>
		/// <param name="source">The "looking" player</param>
		/// <param name="target">The considered player</param>
		/// <returns>The guild name of the target</returns>
		public virtual string GetPlayerGuildName(GamePlayer source, GamePlayer target)
		{
			return target.GuildName;
		}

		/// <summary>
		/// Gets the player's custom title based on server type
		/// </summary>
		/// <param name="source">The "looking" player</param>
		/// <param name="target">The considered player</param>
		/// <returns>The custom title of the target</returns>
		public virtual string GetPlayerTitle(GamePlayer source, GamePlayer target)
		{
			return target.CurrentTitle.GetValue(source, target);
		}

		/// <summary>
		/// Gets the player's Total Amount of Realm Points Based on Level, Realm Level of other constraints.
		/// </summary>
		/// <param name="source">The player</param>
		/// <param name="target"></param>
		/// <returns>The total pool of realm points !</returns>
		public virtual int GetPlayerRealmPointsTotal(GamePlayer source)
		{
			return source.Level > 19 ? Math.Max(1, source.RealmLevel) : source.RealmLevel;
		}

		/// <summary>
		/// Gets the server type color handling scheme
		///
		/// ColorHandling: this byte tells the client how to handle color for PC and NPC names (over the head)
		/// 0: standard way, other realm PC appear red, our realm NPC appear light green
		/// 1: standard PvP way, all PC appear red, all NPC appear with their level color
		/// 2: Same realm livings are friendly, other realm livings are enemy; nearest friend/enemy buttons work
		/// 3: standard PvE way, all PC friendly, realm 0 NPC enemy rest NPC appear light green
		/// 4: All NPC are enemy, all players are friendly; nearest friend button selects self, nearest enemy don't work at all
		/// </summary>
		/// <param name="client">The client asking for color handling</param>
		/// <returns>The color handling</returns>
		public virtual byte GetColorHandling(GameClient client)
		{
			return 0;
		}

		/// <summary>
		/// Formats player statistics.
		/// </summary>
		/// <param name="player">The player to read statistics from.</param>
		/// <returns>List of strings.</returns>
		public virtual IList<string> FormatPlayerStatistics(GamePlayer player)
		{
			List<string> stat = new List<string>();

			int total = 0;
			#region Players Killed
			//only show if there is a kill [by Suncheck]
			if ((player.KillsAlbionPlayers + player.KillsMidgardPlayers + player.KillsHiberniaPlayers) > 0)
			{
				stat.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "PlayerStatistic.Kill.Title"));
				switch ((eRealm)player.Realm)
				{
					case eRealm.Albion:
						if (player.KillsMidgardPlayers > 0) stat.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "PlayerStatistic.Kill.MidgardPlayer") + ": " + player.KillsMidgardPlayers.ToString("N0"));
						if (player.KillsHiberniaPlayers > 0) stat.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "PlayerStatistic.Kill.HiberniaPlayer") + ": " + player.KillsHiberniaPlayers.ToString("N0"));
						total = player.KillsMidgardPlayers + player.KillsHiberniaPlayers;
						break;
					case eRealm.Midgard:
						if (player.KillsAlbionPlayers > 0) stat.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "PlayerStatistic.Kill.AlbionPlayer") + ": " + player.KillsAlbionPlayers.ToString("N0"));
						if (player.KillsHiberniaPlayers > 0) stat.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "PlayerStatistic.Kill.HiberniaPlayer") + ": " + player.KillsHiberniaPlayers.ToString("N0"));
						total = player.KillsAlbionPlayers + player.KillsHiberniaPlayers;
						break;
					case eRealm.Hibernia:
						if (player.KillsAlbionPlayers > 0) stat.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "PlayerStatistic.Kill.AlbionPlayer") + ": " + player.KillsAlbionPlayers.ToString("N0"));
						if (player.KillsMidgardPlayers > 0) stat.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "PlayerStatistic.Kill.MidgardPlayer") + ": " + player.KillsMidgardPlayers.ToString("N0"));
						total = player.KillsMidgardPlayers + player.KillsAlbionPlayers;
						break;
				}
				stat.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "PlayerStatistic.Kill.TotalPlayers") + ": " + total.ToString("N0"));
			}
			#endregion
			stat.Add(" ");
			#region Players Deathblows
			//only show if there is a kill [by Suncheck]
			if ((player.KillsAlbionDeathBlows + player.KillsMidgardDeathBlows + player.KillsHiberniaDeathBlows) > 0)
			{
				total = 0;
				switch ((eRealm)player.Realm)
				{
					case eRealm.Albion:
						if (player.KillsMidgardDeathBlows > 0) stat.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "PlayerStatistic.Deathblows.MidgardPlayer") + ": " + player.KillsMidgardDeathBlows.ToString("N0"));
						if (player.KillsHiberniaDeathBlows > 0) stat.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "PlayerStatistic.Deathblows.HiberniaPlayer") + ": " + player.KillsHiberniaDeathBlows.ToString("N0"));
						total = player.KillsMidgardDeathBlows + player.KillsHiberniaDeathBlows;
						break;
					case eRealm.Midgard:
						if (player.KillsAlbionDeathBlows > 0) stat.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "PlayerStatistic.Deathblows.AlbionPlayer") + ": " + player.KillsAlbionDeathBlows.ToString("N0"));
						if (player.KillsHiberniaDeathBlows > 0) stat.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "PlayerStatistic.Deathblows.HiberniaPlayer") + ": " + player.KillsHiberniaDeathBlows.ToString("N0"));
						total = player.KillsAlbionDeathBlows + player.KillsHiberniaDeathBlows;
						break;
					case eRealm.Hibernia:
						if (player.KillsAlbionDeathBlows > 0) stat.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "PlayerStatistic.Deathblows.AlbionPlayer") + ": " + player.KillsAlbionDeathBlows.ToString("N0"));
						if (player.KillsMidgardDeathBlows > 0) stat.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "PlayerStatistic.Deathblows.MidgardPlayer") + ": " + player.KillsMidgardDeathBlows.ToString("N0"));
						total = player.KillsMidgardDeathBlows + player.KillsAlbionDeathBlows;
						break;
				}
				stat.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "PlayerStatistic.Deathblows.TotalPlayers") + ": " + total.ToString("N0"));
			}
			#endregion
			stat.Add(" ");
			#region Players Solo Kills
			//only show if there is a kill [by Suncheck]
			if ((player.KillsAlbionSolo + player.KillsMidgardSolo + player.KillsHiberniaSolo) > 0)
			{
				total = 0;
				switch ((eRealm)player.Realm)
				{
					case eRealm.Albion:
						if (player.KillsMidgardSolo > 0) stat.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "PlayerStatistic.Solo.MidgardPlayer") + ": " + player.KillsMidgardSolo.ToString("N0"));
						if (player.KillsHiberniaSolo > 0) stat.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "PlayerStatistic.Solo.HiberniaPlayer") + ": " + player.KillsHiberniaSolo.ToString("N0"));
						total = player.KillsMidgardSolo + player.KillsHiberniaSolo;
						break;
					case eRealm.Midgard:
						if (player.KillsAlbionSolo > 0) stat.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "PlayerStatistic.Solo.AlbionPlayer") + ": " + player.KillsAlbionSolo.ToString("N0"));
						if (player.KillsHiberniaSolo > 0) stat.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "PlayerStatistic.Solo.HiberniaPlayer") + ": " + player.KillsHiberniaSolo.ToString("N0"));
						total = player.KillsAlbionSolo + player.KillsHiberniaSolo;
						break;
					case eRealm.Hibernia:
						if (player.KillsAlbionSolo > 0) stat.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "PlayerStatistic.Solo.AlbionPlayer") + ": " + player.KillsAlbionSolo.ToString("N0"));
						if (player.KillsMidgardSolo > 0) stat.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "PlayerStatistic.Solo.MidgardPlayer") + ": " + player.KillsMidgardSolo.ToString("N0"));
						total = player.KillsMidgardSolo + player.KillsAlbionSolo;
						break;
				}
				stat.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "PlayerStatistic.Solo.TotalPlayers") + ": " + total.ToString("N0"));
			}
			#endregion
			stat.Add(" ");
			#region Keeps
			//only show if there is a capture [by Suncheck]
			if ((player.CapturedKeeps + player.CapturedTowers) > 0)
			{
				stat.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "PlayerStatistic.Capture.Title"));
				//stat.Add("Relics Taken: " + player.RelicsTaken.ToString("N0"));
				//stat.Add("Albion Keeps Captured: " + player.CapturedAlbionKeeps.ToString("N0"));
				//stat.Add("Midgard Keeps Captured: " + player.CapturedMidgardKeeps.ToString("N0"));
				//stat.Add("Hibernia Keeps Captured: " + player.CapturedHiberniaKeeps.ToString("N0"));
				if (player.CapturedKeeps > 0) stat.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "PlayerStatistic.Capture.Keeps") + ": " + player.CapturedKeeps.ToString("N0"));
				//stat.Add("Keep Lords Slain: " + player.KeepLordsSlain.ToString("N0"));
				//stat.Add("Albion Towers Captured: " + player.CapturedAlbionTowers.ToString("N0"));
				//stat.Add("Midgard Towers Captured: " + player.CapturedMidgardTowers.ToString("N0"));
				//stat.Add("Hibernia Towers Captured: " + player.CapturedHiberniaTowers.ToString("N0"));
				if (player.CapturedTowers > 0) stat.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "PlayerStatistic.Capture.Towers") + ": " + player.CapturedTowers.ToString("N0"));
				//stat.Add("Tower Captains Slain: " + player.TowerCaptainsSlain.ToString("N0"));
				//stat.Add("Realm Guard Kills Albion: " + player.RealmGuardTotalKills.ToString("N0"));
				//stat.Add("Realm Guard Kills Midgard: " + player.RealmGuardTotalKills.ToString("N0"));
				//stat.Add("Realm Guard Kills Hibernia: " + player.RealmGuardTotalKills.ToString("N0"));
				//stat.Add("Total Realm Guard Kills: " + player.RealmGuardTotalKills.ToString("N0"));
			}
			#endregion
			stat.Add(" ");
			#region PvE
			//only show if there is a kill [by Suncheck]
			if ((player.KillsDragon + player.KillsEpicBoss + player.KillsLegion) > 0)
			{
				stat.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "PlayerStatistic.PvE.Title"));
				if (player.KillsDragon > 0) stat.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "PlayerStatistic.PvE.KillsDragon") + ": " + player.KillsDragon.ToString("N0"));
				if (player.KillsEpicBoss > 0) stat.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "PlayerStatistic.PvE.KillsEpic") + ": " + player.KillsEpicBoss.ToString("N0"));
				if (player.KillsLegion > 0) stat.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "PlayerStatistic.PvE.KillsLegion") + ": " + player.KillsLegion.ToString("N0"));
			}
			#endregion

			return stat;
		}

		/// <summary>
		/// Reset the keep with special server rules handling
		/// </summary>
		/// <param name="lord">The lord that was killed</param>
		/// <param name="killer">The lord's killer</param>
		public virtual void ResetKeep(GuardLord lord, GameObject killer)
		{
			PlayerMgr.UpdateStats(lord);
		}

		/// <summary>
		/// Experience a keep is worth when captured
		/// </summary>
		/// <param name="keep"></param>
		/// <returns></returns>
		public virtual long GetExperienceForKeep(AbstractGameKeep keep)
		{
			return 0;
		}

		public virtual double GetExperienceCapForKeep(AbstractGameKeep keep)
		{
			return 1.0;
		}

		/// <summary>
		/// Realm points a keep is worth when captured
		/// </summary>
		/// <param name="keep"></param>
		/// <returns></returns>
		public virtual int GetRealmPointsForKeep(AbstractGameKeep keep)
		{
			int value = 0;

			if (keep is GameKeep)
			{
				value = Math.Max(50, ServerProperties.Properties.KEEP_RP_BASE + ((keep.BaseLevel - 50) * ServerProperties.Properties.KEEP_RP_MULTIPLIER));
			}
			else
			{
				value = Math.Max(5, ServerProperties.Properties.TOWER_RP_BASE + ((keep.BaseLevel - 50) * ServerProperties.Properties.TOWER_RP_MULTIPLIER));
			}

			value += ((keep.Level - ServerProperties.Properties.STARTING_KEEP_LEVEL) * ServerProperties.Properties.UPGRADE_MULTIPLIER);

			return Math.Max(5, value);
		}

		/// <summary>
		/// Bounty points a keep is worth when captured
		/// </summary>
		/// <param name="keep"></param>
		/// <returns></returns>
		public virtual int GetBountyPointsForKeep(AbstractGameKeep keep)
		{
			return 0;
		}


		/// <summary>
		/// How much money does this keep reward when captured
		/// </summary>
		/// <param name="keep"></param>
		/// <returns></returns>
		public virtual long GetMoneyValueForKeep(AbstractGameKeep keep)
		{
			return 0;
		}


		/// <summary>
		/// Is the player allowed to generate news
		/// </summary>
		/// <param name="player">the player</param>
		/// <returns>true if the player is allowed to generate news</returns>
		public virtual bool CanGenerateNews(GamePlayer player)
		{
			if (player.Client.Account.PrivLevel > 1)
				return false;

			return true;
		}

		/// <summary>
		/// Gets the NPC name based on server type
		/// </summary>
		/// <param name="source">The "looking" player</param>
		/// <param name="target">The considered NPC</param>
		/// <returns>The name of the target</returns>
		public virtual string GetNPCName(GamePlayer source, GameNPC target)
		{
			return target.Name;
		}

		/// <summary>
		/// Gets the NPC guild name based on server type
		/// </summary>
		/// <param name="source">The "looking" player</param>
		/// <param name="target">The considered NPC</param>
		/// <returns>The guild name of the target</returns>
		public virtual string GetNPCGuildName(GamePlayer source, GameNPC target)
		{
			return target.GuildName;
		}


		/// <summary>
		/// Get the items (merchant) list name for a lot marker in the specified region
		/// </summary>
		/// <param name="regionID"></param>
		/// <returns></returns>
		public virtual string GetLotMarkerListName(ushort regionID)
		{
			switch (regionID)
			{
				case 2:
					return "housing_alb_lotmarker";
				case 102:
					return "housing_mid_lotmarker";
				case 202:
					return "housing_hib_lotmarker";
				default:
					return "housing_custom_lotmarker";
			}
		}


		/// <summary>
		/// Send merchant window containing housing items that can be purchased by a player.  If this list is customized
		/// then the customized list must also be handled in BuyHousingItem
		/// </summary>
		/// <param name="player"></param>
		/// <param name="merchantType"></param>
		public virtual void SendHousingMerchantWindow(GamePlayer player, DOL.GS.PacketHandler.eMerchantWindowType merchantType)
		{
			switch (merchantType)
			{
				case eMerchantWindowType.HousingInsideShop:
					player.Out.SendMerchantWindow(HouseTemplateMgr.IndoorShopItems, eMerchantWindowType.HousingInsideShop);
					break;
				case eMerchantWindowType.HousingOutsideShop:
					player.Out.SendMerchantWindow(HouseTemplateMgr.OutdoorShopItems, eMerchantWindowType.HousingOutsideShop);
					break;
				case eMerchantWindowType.HousingBindstoneHookpoint:
					player.Out.SendMerchantWindow(HouseTemplateMgr.IndoorBindstoneShopItems, eMerchantWindowType.HousingBindstoneHookpoint);
					break;
				case eMerchantWindowType.HousingCraftingHookpoint:
					player.Out.SendMerchantWindow(HouseTemplateMgr.IndoorCraftShopItems, eMerchantWindowType.HousingCraftingHookpoint);
					break;
				case eMerchantWindowType.HousingNPCHookpoint:
					player.Out.SendMerchantWindow(HouseTemplateMgr.IndoorNPCShopItems, eMerchantWindowType.HousingNPCHookpoint);
					break;
				case eMerchantWindowType.HousingVaultHookpoint:
					player.Out.SendMerchantWindow(HouseTemplateMgr.IndoorVaultShopItems, eMerchantWindowType.HousingVaultHookpoint);
					break;
				default:
					player.Out.SendMessage("Unknown merchant type!", eChatType.CT_Staff, eChatLoc.CL_SystemWindow);
					log.ErrorFormat("Unknown merchant type {0}", merchantType);
					break;
			}
		}


		/// <summary>
		/// Buys an item off a housing merchant.  If the list has been customized then this must be modified to
		/// match that customized list.
		/// </summary>
		/// <param name="player"></param>
		/// <param name="slot"></param>
		/// <param name="count"></param>
		/// <param name="merchantType"></param>
		public virtual void BuyHousingItem(GamePlayer player, ushort slot, byte count, DOL.GS.PacketHandler.eMerchantWindowType merchantType)
		{
			MerchantTradeItems items = null;

			switch (merchantType)
			{
				case eMerchantWindowType.HousingInsideShop:
					items = HouseTemplateMgr.IndoorShopItems;
					break;
				case eMerchantWindowType.HousingOutsideShop:
					items = HouseTemplateMgr.OutdoorShopItems;
					break;
				case eMerchantWindowType.HousingBindstoneHookpoint:
					items = HouseTemplateMgr.IndoorBindstoneShopItems;
					break;
				case eMerchantWindowType.HousingCraftingHookpoint:
					items = HouseTemplateMgr.IndoorCraftShopItems;
					break;
				case eMerchantWindowType.HousingNPCHookpoint:
					items = HouseTemplateMgr.IndoorNPCShopItems;
					break;
				case eMerchantWindowType.HousingVaultHookpoint:
					items = HouseTemplateMgr.IndoorVaultShopItems;
					break;
			}

			GameMerchant.OnPlayerBuy(player, slot, count, items);
		}


		/// <summary>
		/// Get a housing hookpoint NPC
		/// </summary>
		/// <param name="house"></param>
		/// <param name="templateID"></param>
		/// <param name="heading"></param>
		/// <param name="index"></param>
		/// <returns></returns>
		public virtual GameNPC PlaceHousingNPC(DOL.GS.Housing.House house, ItemTemplate item, IPoint3D location, ushort heading)
		{
			NpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(item.Bonus);

			try
			{
				string defaultClassType = ServerProperties.Properties.GAMENPC_DEFAULT_CLASSTYPE;

				if (npcTemplate == null || string.IsNullOrEmpty(npcTemplate.ClassType))
				{
					log.Warn("[Housing] null classtype in hookpoint attachment, using GAMENPC_DEFAULT_CLASSTYPE instead");
				}
				else
				{
					defaultClassType = npcTemplate.ClassType;
				}

				var npc = (GameNPC)Assembly.GetAssembly(typeof(GameServer)).CreateInstance(defaultClassType, false);
				if (npc == null)
				{
					foreach (Assembly asm in ScriptMgr.Scripts)
					{
						npc = (GameNPC)asm.CreateInstance(defaultClassType, false);
						if (npc != null) break;
					}
				}

				if (npc == null)
				{
					HouseMgr.log.Error("[Housing] Can't create instance of type: " + defaultClassType);
					return null;
				}

				npc.Model = 0;

				if (npcTemplate != null)
				{
					npc.LoadTemplate(npcTemplate);
				}
				else
				{
					npc.Size = 50;
					npc.Level = 50;
					npc.GuildName = "No Template Found";
				}

				if (npc.Model == 0)
				{
					// defaults if templates are missing
					if (house.Realm == eRealm.Albion)
					{
						npc.Model = (ushort)Util.Random(7, 8);
					}
					else if (house.Realm == eRealm.Midgard)
					{
						npc.Model = (ushort)Util.Random(160, 161);
					}
					else
					{
						npc.Model = (ushort)Util.Random(309, 310);
					}
				}

				// always set the npc realm to the house model realm
				npc.Realm = house.Realm;

				npc.Name = item.Name;
				npc.CurrentHouse = house;
				npc.InHouse = true;
				npc.OwnerID = item.Id_nb;
				npc.X = location.X;
				npc.Y = location.Y;
				npc.Z = location.Z;
				npc.Heading = heading;
				npc.CurrentRegionID = house.RegionID;
				if ((npc.Flags & GameNPC.eFlags.PEACE) == 0)
				{
					npc.Flags ^= GameNPC.eFlags.PEACE;
				}
				npc.AddToWorld();
				return npc;
			}
			catch (Exception ex)
			{
				log.Error("Error filling housing hookpoint using npc template ID " + item.Bonus, ex);
			}

			return null;
		}


		public virtual GameStaticItem PlaceHousingInteriorItem(DOL.GS.Housing.House house, ItemTemplate item, IPoint3D location, ushort heading)
		{
			GameStaticItem hookpointObject = new GameStaticItem();
			hookpointObject.CurrentHouse = house;
			hookpointObject.InHouse = true;
			hookpointObject.OwnerID = item.Id_nb;
			hookpointObject.X = location.X;
			hookpointObject.Y = location.Y;
			hookpointObject.Z = location.Z;
			hookpointObject.Heading = heading;
			hookpointObject.CurrentRegionID = house.RegionID;
			hookpointObject.Name = item.Name;
			hookpointObject.Model = (ushort)item.Model;
			hookpointObject.AddToWorld();

			return hookpointObject;
		}

		/// <summary>
		/// This creates the housing consignment merchant attached to a house.
		/// You can override this to create your own consignment merchant derived from the standard merchant
		/// </summary>
		/// <returns></returns>
		public virtual GameConsignmentMerchant CreateHousingConsignmentMerchant(House house)
		{
			var m = new GameConsignmentMerchant();
			m.Name = "Consignment Merchant";
			return m;
		}

		/// <summary>
		/// Standard Rules For Player Level UP
		/// </summary>
		/// <param name="player"></param>
		/// <param name="previousLevel"></param>
		public virtual void OnPlayerLevelUp(GamePlayer player, int previousLevel)
		{
		}
		#region MessageToLiving
		/// <summary>
		/// Send system text message to system window
		/// </summary>
		/// <param name="living"></param>
		/// <param name="message"></param>
		public virtual void MessageToLiving(GameLiving living, string message)
		{
			MessageToLiving(living, message, eChatType.CT_System, eChatLoc.CL_SystemWindow);
		}
		/// <summary>
		/// Send custom text message to system window
		/// </summary>
		/// <param name="living"></param>
		/// <param name="message"></param>
		/// <param name="type"></param>
		public virtual void MessageToLiving(GameLiving living, string message, eChatType type)
		{
			MessageToLiving(living, message, type, eChatLoc.CL_SystemWindow);
		}
		/// <summary>
		/// Send custom text message to GameLiving
		/// </summary>
		/// <param name="living"></param>
		/// <param name="message"></param>
		/// <param name="type"></param>
		/// <param name="loc"></param>
		public virtual void MessageToLiving(GameLiving living, string message, eChatType type, eChatLoc loc)
		{
			if (living is GamePlayer)
				((GamePlayer)living).Out.SendMessage(message, type, loc);
		}

        #endregion

        /// <summary>
        /// Returns true if A considers B friendly
        /// </summary>
        public bool IsFriendly(GameObject A, GameObject B)
        {
            return (A.Realm == B.Realm);
        }
    }
}
