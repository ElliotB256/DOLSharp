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
using DOL.AI.Brain;
using DOL.Database;
using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.GS.Keeps
{
	public class TemplateMgr
	{
		public static void RefreshTemplate(GameKeepGuard guard)
		{
			SetGuardRealm(guard);
			SetGuardGuild(guard);
			SetGuardRespawn(guard);
			SetGuardGender(guard);
			SetGuardModel(guard);
			SetGuardName(guard);
			SetBlockEvadeParryChance(guard);
			SetGuardBrain(guard);
			SetGuardSpeed(guard);
			SetGuardLevel(guard);
			SetGuardResists(guard);
			SetGuardStats(guard);
			SetGuardAggression(guard);
			ClothingMgr.EquipGuard(guard);
			ClothingMgr.SetEmblem(guard);
		}

		private static void SetGuardRealm(GameKeepGuard guard)
		{
			if (guard.Component != null)
			{
				guard.Realm = guard.Component.AbstractKeep.Realm;

				if (guard.Realm != eRealm.None)
				{
					guard.ModelRealm = guard.Realm;
				}
				else
				{
					guard.ModelRealm = (eRealm)Util.Random(1, 3);
				}
			}
			else
			{
				guard.Realm = guard.CurrentZone.Realm;
				guard.ModelRealm = guard.Realm;
			}
		}

		private static void SetGuardGuild(GameKeepGuard guard)
		{
			if (guard.Component == null)
			{
				guard.GuildName = "";
			}
			else if (guard.Component.AbstractKeep.Guild == null)
			{
				guard.GuildName = "";
			}
			else
			{
				guard.GuildName = guard.Component.AbstractKeep.Guild.Name;
			}
		}

		private static void SetGuardRespawn(GameKeepGuard guard)
		{
				guard.RespawnInterval = Util.Random(5, 25) * 60 * 1000;
		}

		private static void SetGuardAggression(GameKeepGuard guard)
		{
		}

		public static void SetGuardLevel(GameKeepGuard guard)
		{
			if (guard.Component != null)
			{
				guard.Component.AbstractKeep.SetGuardLevel(guard);
			}
		}

		private static void SetGuardGender(GameKeepGuard guard)
		{
			//portal keep guards are always male
			if (guard.IsPortalKeepGuard)
			{
                guard.Gender = eGender.Male;
			}
			else
			{
				if (Util.Chance(50))
				{
					guard.Gender = eGender.Male;
				}
				else
				{
                    guard.Gender = eGender.Female;
				}
			}
		}


		#region Hastener Models
		public static ushort AlbionHastener = 244;
		public static ushort MidgardHastener = 22;
		public static ushort HiberniaHastener = 1910;
		#endregion

		#region AlbionClassModels
		public static ushort BritonMale = 32;
		public static ushort BritonFemale = 35;
		public static ushort HighlanderMale = 39;
		public static ushort HighlanderFemale = 43;
		public static ushort SaracenMale = 48;
		public static ushort SaracenFemale = 52;
		public static ushort AvalonianMale = 61;
		public static ushort AvalonianFemale = 65;
		public static ushort IcconuMale = 716;
		public static ushort IcconuFemale = 724;
		public static ushort HalfOgreMale = 1008;
		public static ushort HalfOgreFemale = 1020;
        public static ushort MinotaurMaleAlb = 1395;
		#endregion
		#region MidgardClassModels
		public static ushort TrollMale = 137;
		public static ushort TrollFemale = 145;
		public static ushort NorseMale = 503;
		public static ushort NorseFemale = 507;
		public static ushort KoboldMale = 169;
		public static ushort KoboldFemale = 177;
		public static ushort DwarfMale = 185;
		public static ushort DwarfFemale = 193;
		public static ushort ValkynMale = 773;
		public static ushort ValkynFemale = 781;
		public static ushort FrostalfMale = 1051;
		public static ushort FrostalfFemale = 1063;
        public static ushort MinotaurMaleMid = 1407;
		#endregion
		#region HiberniaClassModels
		public static ushort FirbolgMale = 286;
		public static ushort FirbolgFemale = 294;
		public static ushort CeltMale = 302;
		public static ushort CeltFemale = 310;
		public static ushort LurikeenMale = 318;
		public static ushort LurikeenFemale = 326;
		public static ushort ElfMale = 334;
		public static ushort ElfFemale = 342;
		public static ushort SharMale = 1075;
		public static ushort SharFemale = 1087;
		public static ushort SylvianMale = 700;
		public static ushort SylvianFemale = 708;
        public static ushort MinotaurMaleHib = 1419;
		#endregion

		/// <summary>
		/// Sets a guards model
		/// </summary>
		/// <param name="guard">The guard object</param>
		private static void SetGuardModel(GameKeepGuard guard)
		{
			if(!ServerProperties.Properties.AUTOMODEL_GUARDS_LOADED_FROM_DB && !guard.LoadedFromScript)
			{
				return;
			}

            guard.Model = NorseMale;
		}

        /// <summary>
        /// Gets short name of keeps
        /// </summary>
        /// <param name="KeepName">Complete name of the Keep</param>
        private static string GetKeepShortName(string KeepName)
        {
            string ShortName;
            if (KeepName.StartsWith("Caer"))//Albion
            {
                ShortName = KeepName.Substring(5);
            }
			else if (KeepName.StartsWith("Fort"))
			{
				ShortName = KeepName.Substring(5);
			}
			else if (KeepName.StartsWith("Dun"))//Hibernia
            {
                if (KeepName == "Dun nGed")
                {
                    ShortName = "Ged";
                }
                else if (KeepName == "Dun da Behn")
                {
                    ShortName = "Behn";
                }
                else
                {
                    ShortName = KeepName.Substring(4);
                }
            }
			else if (KeepName.StartsWith("Castle"))// Albion Relic
			{
				ShortName = KeepName.Substring(7);
			}
			else//Midgard
			{
				if (KeepName.Contains(" "))
					ShortName = KeepName.Substring(0, KeepName.IndexOf(" ", 0));
				else
					ShortName = KeepName;
			}
            return ShortName;
        }

		/// <summary>
		/// Sets a guards name
		/// </summary>
		/// <param name="guard">The guard object</param>
		private static void SetGuardName(GameKeepGuard guard)
		{
			if (guard is GuardLord)
			{
				if (guard.Component == null)
				{
                    guard.Name = LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "SetGuardName.Commander", guard.CurrentZone.Description);
                    return;
				}
				else if (guard.IsTowerGuard)
				{
                    guard.Name = LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "SetGuardName.TowerCaptain");
                    return;
				}
			}
            guard.Name = "Guard LOL";

			if (guard.Realm == eRealm.None)
			{
				guard.Name = LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "SetGuardName.Renegade", guard.Name);
			}
        }

		/// <summary>
		/// Sets a guards Block, Parry and Evade change
		/// </summary>
		/// <param name="guard">The guard object</param>
		private static void SetBlockEvadeParryChance(GameKeepGuard guard)
		{
			guard.BlockChance = 0;
			guard.EvadeChance = 0;
			guard.ParryChance = 0;

			if (guard is GuardLord || guard is MissionMaster)
			{
				guard.BlockChance = 15;
				guard.ParryChance = 15;

				if (guard.ModelRealm != eRealm.Albion)
				{
					guard.EvadeChance = 10;
					guard.ParryChance = 5;
				}
			}
		}

		/// <summary>
		/// Sets the guards brain
		/// </summary>
		/// <param name="guard">The guard object</param>
		public static void SetGuardBrain(GameKeepGuard guard)
		{
		}

		/// <summary>
		/// Sets the guards speed
		/// </summary>
		/// <param name="guard">The guard object</param>
		public static void SetGuardSpeed(GameKeepGuard guard)
		{
			if (guard.IsPortalKeepGuard)
			{
				guard.MaxSpeedBase = 575;
			}
			if ((guard is GuardLord && guard.Component != null))
			{
				guard.MaxSpeedBase = 0;
			}
			else if (guard.Level < 250)
			{
				if (guard.Realm == eRealm.None)
				{
					guard.MaxSpeedBase = 200;
				}
				else if (guard.Level < 50)
				{
					guard.MaxSpeedBase = 210;
				}
				else
				{
					guard.MaxSpeedBase = 250;
				}
			}
			else
			{
				guard.MaxSpeedBase = 575;
			}
		}

		/// <summary>
		/// Sets a guards resists
		/// </summary>
		/// <param name="guard">The guard object</param>
		private static void SetGuardResists(GameKeepGuard guard)
		{
			for (int i = (int)eProperty.Resist_First; i <= (int)eProperty.Resist_Last; i++)
			{
				if (guard is GuardLord)
				{
					guard.BaseBuffBonusCategory[i] = 40;
				}
				else if (guard.Level < 50)
				{
					guard.BaseBuffBonusCategory[i] = guard.Level / 2 + 1;
				}
				else
				{
					guard.BaseBuffBonusCategory[i] = 26;
				}
			}
		}

		/// <summary>
		/// Sets a guards stats
		/// </summary>
		/// <param name="guard">The guard object</param>
		private static void SetGuardStats(GameKeepGuard guard)
		{
			if (guard is GuardLord)
			{
				guard.Strength = (short)(20 + (guard.Level * 8));
				guard.Dexterity = (short)(guard.Level * 2);
				guard.Constitution = (short)(DOL.GS.ServerProperties.Properties.GAMENPC_BASE_CON);
				guard.Quickness = 60;
			}
			else
			{
				guard.Strength = (short)(20 + (guard.Level * 7));
				guard.Dexterity = (short)(guard.Level);
				guard.Constitution = (short)(DOL.GS.ServerProperties.Properties.GAMENPC_BASE_CON);
				guard.Quickness = 40;
			}
		}
	}
}
