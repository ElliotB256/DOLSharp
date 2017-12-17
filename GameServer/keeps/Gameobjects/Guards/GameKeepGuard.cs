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
using DOL.AI.Brain;
using DOL.Database;
using DOL.Events;
using DOL.GS.PacketHandler;
using DOL.Language;

using System.Reflection;
using log4net;


namespace DOL.GS.Keeps
{
	/// <summary>
	/// Keep guard is gamemob with just different brain and load from other DB table
	/// </summary>
	public class GameKeepGuard : GameNPC, IKeepItem
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		private Patrol m_Patrol = null;
		public Patrol PatrolGroup
		{
			get { return m_Patrol; }
			set { m_Patrol = value; }
		}

		private string m_templateID = "";
		public string TemplateID
		{
			get { return m_templateID; }
			set { m_templateID = value; }
		}

		private GameKeepComponent m_component;
		public GameKeepComponent Component
		{
			get { return m_component; }
			set { m_component = value; }
		}

		private DBKeepPosition m_position;
		public DBKeepPosition Position
		{
			get { return m_position; }
			set { m_position = value; }
		}

		private GameKeepHookPoint m_hookPoint;
		public GameKeepHookPoint HookPoint
		{
			get { return m_hookPoint; }
			set { m_hookPoint = value; }
		}

		private eRealm m_modelRealm = eRealm.None;
		public eRealm ModelRealm
		{
			get { return m_modelRealm; }
			set { m_modelRealm = value; }
		}

		public bool IsTowerGuard
		{
			get
			{
				if (this.Component != null && this.Component.AbstractKeep != null)
				{
					return this.Component.AbstractKeep is GameKeepTower;
				}
				return false;
			}
		}

		public bool IsPortalKeepGuard
		{
			get
			{
				if (this.Component == null || this.Component.AbstractKeep == null)
					return false;
				return this.Component.AbstractKeep.IsPortalKeep;
			}
		}

		/// <summary>
		/// We do this because if we set level when a guard is waiting to respawn,
		/// the guard will never respawn because the guard is given full health and
		/// is then considered alive
		/// </summary>
		public override byte Level
		{
			get
			{
				if(IsPortalKeepGuard)
					return 255;

				return base.Level;
			}
			set
			{
				if (this.IsRespawning)
					m_level = value;
				else
					base.Level = value;
			}
		}

		public override double GetArmorAbsorb(eArmorSlot slot)
		{
			double abs = GetModified(eProperty.ArmorAbsorption);

			if (this is GuardLord)
			{
				abs += 5;
			}

			return Math.Max(0.0, abs * 0.01);
		}

		/// <summary>
		/// Guards always have Mana to cast spells
		/// </summary>
		public override int Mana
		{
			get { return 50000; }
		}

		public override int MaxHealth
		{
			get { return GetModified(eProperty.MaxHealth) + (base.Level * 4); }
		}

		private bool m_changingPositions = false;


		#region Combat

		/// <summary>
		/// The distance attack range
		/// </summary>
		public virtual int AttackRangeDistance
		{
			get
			{
				return 0;
			}
		}
        
		public void GuardStartSpellHealCheckLOS(GamePlayer player, ushort response, ushort targetOID)
		{
			if ((response & 0x100) == 0x100)
			{
				SpellMgr.CastHealSpell(this, TargetObject as GameLiving);
			}
		}
		public void GuardStartSpellNukeCheckLOS(GamePlayer player, ushort response, ushort targetOID)
		{
			if ((response & 0x100) == 0x100)
			{
				SpellMgr.CastNukeSpell(this, TargetObject as GameLiving);
			}
		}
        
		/// <summary>
		/// Because of Spell issues, we will always return this true
		/// </summary>
		/// <param name="target"></param>
		/// <param name="viewangle"></param>
		/// <returns></returns>
		public override bool IsObjectInFront(GameObject target, double viewangle, bool rangeCheck = true)
		{
			return true;
		}
       
		/// <summary>
		/// When guards Die and it isnt a keep reset (this killer) we call GuardSpam function
		/// </summary>
		/// <param name="killer"></param>
		public override void Die()
		{
			base.Die();
			if (RespawnInterval == -1)
				Delete();
		}

		#region Guard Spam
		/// <summary>
		/// Sends message to guild for guard death with enemy count in area
		/// </summary>
		/// <param name="guard">The guard object</param>
		public static void GuardSpam(GameKeepGuard guard)
		{
			if (guard.Component == null) return;
			if (guard.Component.AbstractKeep == null) return;
			if (guard.Component.AbstractKeep.Guild == null) return;

			int inArea = guard.GetEnemyCountInArea();
            string message = LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "GameKeepGuard.GuardSpam.Killed", guard.Name, guard.Component.AbstractKeep.Name, inArea);
            KeepGuildMgr.SendMessageToGuild(message, guard.Component.AbstractKeep.Guild);
		}

		/// <summary>
		/// Gets the count of enemies in the Area
		/// </summary>
		/// <returns></returns>
		public int GetEnemyCountInArea()
		{
			int inArea = 0;
			foreach (GamePlayer NearbyPlayers in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
			{
				if (this.Component != null)
				{
					if (GameServer.KeepManager.IsEnemy(this.Component.AbstractKeep, NearbyPlayers))
						inArea++;
				}
				else
				{
					if (GameServer.ServerRules.IsAllowedToAttack(this, NearbyPlayers, true))
						inArea++;
				}
			}
			return inArea;
		}


		#endregion

		/// <summary>
		/// Has the NPC been attacked recently.. currently 10 seconds
		/// </summary>
		public bool BeenAttackedRecently
		{
			get
			{
				return CurrentRegion.Time - LastAttackedByEnemyTick < 10 * 1000;
			}
		}
		#endregion

		/// <summary>
		/// When we add a guard to the world, we also attach an AttackFinished handler
		/// We use this to check LOS and range issues for our ranged guards
		/// </summary>
		/// <returns></returns>
		public override bool AddToWorld()
		{
			base.RoamingRange = 0;
			base.TetherRange = 10000;
			
			if (!base.AddToWorld())
				return false;
			
			if (PatrolGroup != null && !m_changingPositions)
			{
				bool foundGuard = false;
				foreach (GameKeepGuard guard in PatrolGroup.PatrolGuards)
				{
					if (guard.IsAlive && guard.CurrentWayPoint != null)
					{
						CurrentWayPoint = guard.CurrentWayPoint;
						m_changingPositions = true;
						MoveTo(guard.CurrentRegionID, guard.X - Util.Random(200, 350), guard.Y - Util.Random(200, 350), guard.Z, guard.Heading);
						m_changingPositions = false;
						foundGuard = true;
						break;
					}
				}

				if (!foundGuard)
					CurrentWayPoint = PatrolGroup.PatrolPath;

				MoveOnPath(Patrol.PATROL_SPEED);
			}

			return true;
		}

		/// <summary>
		/// When we remove from world, we remove our special handler
		/// </summary>
		/// <returns></returns>
		public override bool RemoveFromWorld()
		{
			if (base.RemoveFromWorld())
			{
				return true;
			}

			return false;
		}

		/// <summary>
		/// Method to stop a guards respawn
		/// </summary>
		public void StopRespawn()
		{
			if (IsRespawning)
				m_respawnTimer.Stop();
		}

		/// <summary>
		/// When guards respawn we refresh them, if a patrol guard respawns we
		/// call a special function to update leadership
		/// </summary>
		/// <param name="respawnTimer"></param>
		/// <returns></returns>
		protected override int RespawnTimerCallback(RegionTimer respawnTimer)
		{
			int temp = base.RespawnTimerCallback(respawnTimer);
			if (Component != null && Component.AbstractKeep != null)
			{
				Component.AbstractKeep.TemplateManager.GetMethod("RefreshTemplate").Invoke(null, new object[] { this });
			}
			else
			{
				TemplateMgr.RefreshTemplate(this);
			}
			return temp;
		}

		/// <summary>
		/// Gets the messages when you click on a guard
		/// </summary>
		/// <param name="player">The player that has done the clicking</param>
		/// <returns></returns>
		public override IList GetExamineMessages(GamePlayer player)
		{
			//You target [Armwoman]
			//You examine the Armswoman. She is friendly and is a realm guard.
			//She has upgraded equipment (5).
			IList list = new ArrayList(4);
            list.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameKeepGuard.GetExamineMessages.YouTarget", GetName(0, false)));

			if (Realm != eRealm.None)
			{
                list.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameKeepGuard.GetExamineMessages.YouExamine", GetName(0, false), GetPronoun(0, true), GetAggroLevelString(player, false)));
				if (this.Component != null)
				{
					string text = "";
					if (this.Component.AbstractKeep.Level > 1 && this.Component.AbstractKeep.Level < 250 && GameServer.ServerRules.IsSameRealm(player, this, true))
                        text = LanguageMgr.GetTranslation(player.Client.Account.Language, "GameKeepGuard.GetExamineMessages.Upgraded", GetPronoun(0, true), this.Component.AbstractKeep.Level);
					if (ServerProperties.Properties.USE_KEEP_BALANCING && this.Component.AbstractKeep.Region == 163 && !(this.Component.AbstractKeep is GameKeepTower))
                        text += LanguageMgr.GetTranslation(player.Client.Account.Language, "GameKeepGuard.GetExamineMessages.Balancing", GetPronoun(0, true), (Component.AbstractKeep.BaseLevel - 50).ToString());
					if (text != "")
						list.Add(text);
				}
			}
			return list;
		}

		/// <summary>
		/// Gets the pronoun for the guards gender
		/// </summary>
		/// <param name="form">Form of the pronoun</param>
		/// <param name="firstLetterUppercase">Weather or not we want the first letter uppercase</param>
		/// <returns></returns>
		public override string GetPronoun(int form, bool firstLetterUppercase)
		{
			string s = "";
			switch (form)
			{
				default:
					{
						// Subjective
						if (Gender == GS.eGender.Male)
                            s = LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "GameKeepGuard.GetPronoun.He");
                        else s = LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "GameKeepGuard.GetPronoun.She");
						if (!firstLetterUppercase)
							s = s.ToLower();
						break;
					}
				case 1:
					{
						// Possessive
                        if (Gender == eGender.Male)
                            s = LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "GameKeepGuard.GetPronoun.His");
                        else s = LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "GameKeepGuard.GetPronoun.Hers");
						if (!firstLetterUppercase)
							s = s.ToLower();
						break;
					}
				case 2:
					{
						// Objective
                        if (Gender == eGender.Male)
                            s = LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "GameKeepGuard.GetPronoun.Him");
                        else s = LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "GameKeepGuard.GetPronoun.Her");
						if (!firstLetterUppercase)
							s = s.ToLower();
						break;
					}
			}
			return s;
		}

		#region Database

		string m_dataObjectID = "";

		/// <summary>
		/// Load the guard from the database
		/// </summary>
		/// <param name="mobobject">The database mobobject</param>
		public override void LoadFromDatabase(DataObject mobobject)
		{
			base.LoadFromDatabase(mobobject);
			foreach (AbstractArea area in this.CurrentAreas)
			{
				if (area is KeepArea)
				{
					AbstractGameKeep keep = (area as KeepArea).Keep;
					Component = new GameKeepComponent();
					Component.AbstractKeep = keep;
					m_dataObjectID = mobobject.ObjectId;
					// mob reload command might be reloading guard, so check to make sure it isn't already added
					if (Component.AbstractKeep.Guards.ContainsKey(m_dataObjectID) == false)
					{
						Component.AbstractKeep.Guards.Add(m_dataObjectID, this);
					}
					break;
				}
			}

			if (Component != null && Component.AbstractKeep != null)
			{
				Component.AbstractKeep.TemplateManager.GetMethod("RefreshTemplate").Invoke(null, new object[] { this });
			}
			else
			{
				TemplateMgr.RefreshTemplate(this);
			}
		}

		public void DeleteObject()
		{
			if (Component != null)
			{
				if (Component.AbstractKeep != null)
				{
					if (Component.AbstractKeep.Guards.ContainsKey(m_dataObjectID))
					{
						Component.AbstractKeep.Guards.Remove(m_dataObjectID);
					}
					else
					{
						log.Warn("Can't find " + Name + " in Component Guard list.");
					}
				}
				else
				{
					log.Warn("Keep is null on delete of guard " + Name + ".");
				}

				Component.Delete();
			}

			HookPoint = null;
			Component = null;
			if (Inventory != null)
				Inventory.ClearInventory();
			Inventory = null;
			Position = null;
			TempProperties.removeAllProperties();

			base.Delete();

			SetOwnBrain(null);
			CurrentRegion = null;

			GameEventMgr.RemoveAllHandlersForObject(this);
		}

		public override void Delete()
		{
			if (HookPoint != null && Component != null)
			{
				Component.AbstractKeep.Guards.Remove(this.ObjectID);
			}

			TempProperties.removeAllProperties();

			base.Delete();
		}

		public override void DeleteFromDatabase()
		{
			foreach (AbstractArea area in this.CurrentAreas)
			{
				if (area is KeepArea && Component != null)
				{
					Component.AbstractKeep.Guards.Remove(this.InternalID);
					break;
				}
			}
			base.DeleteFromDatabase();
		}

		/// <summary>
		/// Load the guard from a position
		/// </summary>
		/// <param name="pos">The position for the guard</param>
		/// <param name="component">The component it is being spawned on</param>
		public void LoadFromPosition(DBKeepPosition pos, GameKeepComponent component)
		{
			m_templateID = pos.TemplateID;
			m_component = component;
			component.AbstractKeep.Guards[m_templateID] = this;
			PositionMgr.LoadGuardPosition(pos, this);
			if (Component != null && Component.AbstractKeep != null)
			{
				Component.AbstractKeep.TemplateManager.GetMethod("RefreshTemplate").Invoke(null, new object[] { this });
			}
			else
			{
				TemplateMgr.RefreshTemplate(this);
			}
			this.AddToWorld();
		}

		/// <summary>
		/// Move a guard to a position
		/// </summary>
		/// <param name="position">The new position for the guard</param>
		public void MoveToPosition(DBKeepPosition position)
		{
			PositionMgr.LoadGuardPosition(position, this);
			if (!this.InCombat)
				this.MoveTo(this.CurrentRegionID, this.X, this.Y, this.Z, this.Heading);
		}
		#endregion

		/// <summary>
		/// Change guild of guard (emblem on equipment) when keep is claimed
		/// </summary>
		public void ChangeGuild()
		{
			ClothingMgr.EquipGuard(this);

			Guild guild = this.Component.AbstractKeep.Guild;
			string guildname = "";
			if (guild != null)
				guildname = guild.Name;

			this.GuildName = guildname;

			if (this.Inventory == null)
				return;

			int emblem = 0;
			if (guild != null)
				emblem = guild.Emblem;
			InventoryItem lefthand = this.Inventory.GetItem(eInventorySlot.LeftHandWeapon);
			if (lefthand != null)
				lefthand.Emblem = emblem;

			InventoryItem cloak = this.Inventory.GetItem(eInventorySlot.Cloak);
			if (cloak != null)
			{
				cloak.Emblem = emblem;

				if (cloak.Emblem != 0)
					cloak.Model = 558; // change to a model that looks ok with an emblem

			}
			if (IsAlive)
			{
				BroadcastLivingEquipmentUpdate();
			}
		}

		/// <summary>
		/// Adding special handling for walking to a point for patrol guards to be in a formation
		/// </summary>
		/// <param name="tx"></param>
		/// <param name="ty"></param>
		/// <param name="tz"></param>
		/// <param name="speed"></param>
		public override void WalkTo(int tx, int ty, int tz, short speed)
		{
			int offX = 0; int offY = 0;
			if (IsMovingOnPath && PatrolGroup != null)
				PatrolGroup.GetMovementOffset(this, out offX, out offY);
			base.WalkTo(tx - offX, ty - offY, tz, speed);
		}

		/// <summary>
		/// Walk to the spawn point, always max speed for keep guards, or continue patrol.
		/// </summary>
		public override void WalkToSpawn()
		{
			if (PatrolGroup != null)
			{
				StopFollowing();

				StandardMobBrain brain = Brain as StandardMobBrain;
				if (brain != null && brain.HasAggro)
				{
					brain.ClearAggroList();
				}

				PatrolGroup.StartPatrol();
			}
			else
			{
				WalkToSpawn(MaxSpeed);
			}
		}

	}
}
