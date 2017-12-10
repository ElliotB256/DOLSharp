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
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;

using DOL.AI.Brain;
using DOL.Database;
using DOL.Events;
using DOL.GS.Effects;
using DOL.GS.Keeps;
using DOL.GS.PacketHandler;
using DOL.GS.PropertyCalc;
using DOL.GS.Spells;
using DOL.GS.Styles;
using DOL.GS.ModularSkills;
using DOL.Language;

using DOL.Talents;

namespace DOL.GS
{
	/// <summary>
	/// This class holds all information that each
	/// living object in the world uses
	/// </summary>
	public abstract class GameLiving : GameObject, ITalentOwner
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Can this living accept any item regardless of tradable or droppable?
        /// </summary>
        public virtual bool CanTradeAnyItem
        {
            get { return false; }
        }

        protected short m_race;
        public virtual short Race
        {
            get { return m_race; }
            set { m_race = value; }
        }

        /// <summary>
        /// Holds the turning disabled counter
        /// </summary>
        protected sbyte m_turningDisabledCount;
        /// <summary>
        /// Gets/Sets wether the player can turn the character
        /// </summary>
        public bool IsTurningDisabled
        {
            get { return m_turningDisabledCount > 0; }
        }

        /// <summary>
        /// Disables the turning for this living
        /// </summary>
        /// <param name="add"></param>
        public virtual void DisableTurning(bool add)
        {
            if (add) m_turningDisabledCount++;
            else m_turningDisabledCount--;

            if (m_turningDisabledCount < 0)
                m_turningDisabledCount = 0;
        }

        #region Combat

        /// <summary>
        /// The living starts an attack on a target
        /// </summary>
        public event EventHandler<Attack> Attacking;

        /// <summary>
        /// The living has attacked a target
        /// </summary>
        public event EventHandler<AttackOutcome> Attacked;

        /// <summary>
        /// The living starts to receive an attack from an attacker
        /// </summary>
        public event EventHandler<Attack> BeingAttacked;

        /// <summary>
        /// The living has been attacked
        /// </summary>
        public event EventHandler<AttackOutcome> BeenAttacked;

        /// <summary>
        /// Make an attack
        /// </summary>
        /// <param name="attack"></param>
        public void MakeAttack(GameLiving target, Attack attack)
        {
            attack.Attacker = this;
            attack.Target = target;

            var handler = Attacking;
            handler?.Invoke(this, attack);

            var outcome = target?.ReceiveAttack(attack);
            var attackedHandler = Attacked;
            attackedHandler?.Invoke(this, outcome);
        }

        /// <summary>
        /// An attack starts on this living
        /// </summary>
        /// <param name="attack"></param>
        public AttackOutcome ReceiveAttack(Attack attack)
        {
            var handler = BeingAttacked;
            handler?.Invoke(this, attack);

            var outcome = attack.DetermineResult();

            var baHandler = BeenAttacked;
            baHandler?.Invoke(this, outcome);

            return outcome;
        }

        #endregion

        #region enums

        /// <summary>
        /// The possible states for a ranged attack
        /// </summary>
        public enum eRangedAttackState : byte
		{
			/// <summary>
			/// No ranged attack active
			/// </summary>
			None = 0,
			/// <summary>
			/// Ranged attack in aim-state
			/// </summary>
			Aim,
			/// <summary>
			/// Player wants to fire the shot/throw NOW!
			/// </summary>
			Fire,
			/// <summary>
			/// Ranged attack will fire when ready
			/// </summary>
			AimFire,
			/// <summary>
			/// Ranged attack will fire and reload when ready
			/// </summary>
			AimFireReload,
			/// <summary>
			/// Ranged attack is ready to be fired
			/// </summary>
			ReadyToFire,
		}

		/// <summary>
		/// The type of range attack
		/// </summary>
		public enum eRangedAttackType : byte
		{
			/// <summary>
			/// A normal ranged attack
			/// </summary>
			Normal = 0,
			/// <summary>
			/// A critical shot is attempted
			/// </summary>
			Critical,
			/// <summary>
			/// A longshot is attempted
			/// </summary>
			Long,
			/// <summary>
			/// A volley shot is attempted
			/// </summary>
			Volley,
			/// <summary>
			/// A sure shot is attempted
			/// </summary>
			SureShot,
			/// <summary>
			/// A rapid shot is attempted
			/// </summary>
			RapidFire,
		}

		/// <summary>
		/// Holds all the ways this living can
		/// be healed
		/// </summary>
		public enum eHealthChangeType : byte
		{
			/// <summary>
			/// The health was changed by something unknown
			/// </summary>
			Unknown = 0,
			/// <summary>
			/// Regeneration changed the health
			/// </summary>
			Regenerate = 1,
			/// <summary>
			/// A spell changed the health
			/// </summary>
			Spell = 2,
			/// <summary>
			/// A potion changed the health
			/// </summary>
			Potion = 3
		}
		/// <summary>
		/// Holds all the ways this living can
		/// be healed
		/// </summary>
		public enum eManaChangeType : byte
		{
			/// <summary>
			/// Unknown mana change
			/// </summary>
			Unknown = 0,
			/// <summary>
			/// Mana was changed by regenerate
			/// </summary>
			Regenerate = 1,
			/// <summary>
			/// Mana was changed by spell
			/// </summary>
			Spell = 2,
			/// <summary>
			/// Mana was changed by potion
			/// </summary>
			Potion = 3
		}
		/// <summary>
		/// Holds all the ways this living can
		/// be healed
		/// </summary>
		public enum eEnduranceChangeType : byte
		{
			/// <summary>
			/// Enduracen was changed by unknown
			/// </summary>
			Unknown = 0,
			/// <summary>
			/// Endurance was changed by Regenerate
			/// </summary>
			Regenerate = 1,
			/// <summary>
			/// Enduracen was changed by spell
			/// </summary>
			Spell = 2,
			/// <summary>
			/// Enduracen was changed by potion
			/// </summary>
			Potion = 3
		}
		/// <summary>
		/// Holds the possible activeWeaponSlot values
		/// </summary>
		public enum eActiveWeaponSlot : byte
		{
			/// <summary>
			/// Weapon slot righthand
			/// </summary>
			Standard = 0x00,
			/// <summary>
			/// Weaponslot twohanded
			/// </summary>
			TwoHanded = 0x01,
			/// <summary>
			/// Weaponslot distance
			/// </summary>
			Distance = 0x02
		}

		/// <summary>
		/// Holds the possible activeQuiverSlot values
		/// </summary>
		public enum eActiveQuiverSlot : byte
		{
			/// <summary>
			/// No quiver slot active
			/// </summary>
			None = 0x00,
			/// <summary>
			/// First quiver slot
			/// </summary>
			First = 0x10,
			/// <summary>
			/// Second quiver slot
			/// </summary>
			Second = 0x20,
			/// <summary>
			/// Third quiver slot
			/// </summary>
			Third = 0x40,
			/// <summary>
			/// Fourth quiver slot
			/// </summary>
			Fourth = 0x80,
		}

		public enum eXPSource
		{
			NPC,
			Player,
			Quest,
			Mission,
			Task,
			Praying,
			GM,
			Other
		}

        #endregion

        #region old combat shit

        /// <summary>
        /// if living is stunned or not
        /// </summary>
        protected bool m_stunned;
		/// <summary>
		/// Gets the stunned flag of this living
		/// </summary>
		public bool IsStunned
		{
			get { return m_stunned; }
			set { m_stunned = value; }
		}

		/// <summary>
		/// say if living is mezzed or not
		/// </summary>
		protected bool m_mezzed;
		/// <summary>
		/// Gets the mesmerized flag of this living
		/// </summary>
		public bool IsMezzed
		{
			get { return m_mezzed; }
			set { m_mezzed = value; }
		}

		/// <summary>
		/// Gets the current strafing mode
		/// </summary>
		public virtual bool IsStrafing
		{
			get { return false; }
			set { }
		}

		/// <summary>
		/// Holds the weaponslot to be used
		/// </summary>
		protected eActiveWeaponSlot m_activeWeaponSlot;

		/// <summary>
		/// The objects currently attacking this living
		/// To be more exact, the objects that are in combat
		/// and have this living as target.
		/// </summary>
		protected readonly List<GameObject> m_attackers;

		/// <summary>
		/// Returns the current active weapon slot of this living
		/// </summary>
		public virtual eActiveWeaponSlot ActiveWeaponSlot
		{
			get { return m_activeWeaponSlot; }
		}

		/// <summary>
		/// Create a pet for this living
		/// </summary>
		/// <param name="template"></param>
		/// <returns></returns>
		public virtual GamePet CreateGamePet(INpcTemplate template)
		{
			return new GamePet(template);
		}

		/// <summary>
		/// A new pet has been summoned, do we do anything?
		/// </summary>
		/// <param name="pet"></param>
		public virtual void OnPetSummoned(GamePet pet)
		{
		}


		/// <summary>
		/// last attack tick in either pve or pvp
		/// </summary>
		public virtual long LastAttackTick
		{
			get
			{
				if (m_lastAttackTickPvE > m_lastAttackTickPvP)
					return m_lastAttackTickPvE;
				return m_lastAttackTickPvP;
			}
		}

		/// <summary>
		/// last attack tick for pve
		/// </summary>
		protected long m_lastAttackTickPvE;
		/// <summary>
		/// gets/sets gametick when this living has attacked its target in pve
		/// </summary>
		public virtual long LastAttackTickPvE
		{
			get { return m_lastAttackTickPvE; }
			set
			{
				m_lastAttackTickPvE = value;
				if (this is GameNPC)
				{
					if ((this as GameNPC).Brain is IControlledBrain)
					{
						((this as GameNPC).Brain as IControlledBrain).Owner.LastAttackTickPvE = value;
					}
				}
			}
		}

		/// <summary>
		/// last attack tick for pvp
		/// </summary>
		protected long m_lastAttackTickPvP;
		/// <summary>
		/// gets/sets gametick when this living has attacked its target in pvp
		/// </summary>
		public virtual long LastAttackTickPvP
		{
			get { return m_lastAttackTickPvP; }
			set
			{
				m_lastAttackTickPvP = value;
				if (this is GameNPC)
				{
					if ((this as GameNPC).Brain is IControlledBrain)
					{
						((this as GameNPC).Brain as IControlledBrain).Owner.LastAttackTickPvP = value;
					}
				}
			}
		}

		/// <summary>
		/// gets the last attack or attackedbyenemy tick in pvp
		/// </summary>
		public long LastCombatTickPvP
		{
			get
			{
				if (m_lastAttackTickPvP > m_lastAttackedByEnemyTickPvP)
					return m_lastAttackTickPvP;
				else return m_lastAttackedByEnemyTickPvP;
			}
		}

		/// <summary>
		/// gets the last attack or attackedbyenemy tick in pve
		/// </summary>
		public long LastCombatTickPvE
		{
			get
			{
				if (m_lastAttackTickPvE > m_lastAttackedByEnemyTickPvE)
					return m_lastAttackTickPvE;
				else return m_lastAttackedByEnemyTickPvE;
			}
		}

		/// <summary>
		/// last attacked by enemy tick in either pvp or pve
		/// </summary>
		public virtual long LastAttackedByEnemyTick
		{
			get
			{
				if (m_lastAttackedByEnemyTickPvP > m_lastAttackedByEnemyTickPvE)
					return m_lastAttackedByEnemyTickPvP;
				return m_lastAttackedByEnemyTickPvE;
			}
		}

		/// <summary>
		/// last attacked by enemy tick in pve
		/// </summary>
		protected long m_lastAttackedByEnemyTickPvE;
		/// <summary>
		/// gets/sets gametick when this living was last time attacked by an enemy in pve
		/// </summary>
		public virtual long LastAttackedByEnemyTickPvE
		{
			get { return m_lastAttackedByEnemyTickPvE; }
			set
			{
				m_lastAttackedByEnemyTickPvE = value;
				if (this is GameNPC)
				{
					if ((this as GameNPC).Brain is IControlledBrain)
					{
						((this as GameNPC).Brain as IControlledBrain).Owner.LastAttackedByEnemyTickPvE = value;
					}
				}
			}
		}

		/// <summary>
		/// last attacked by enemy tick in pve
		/// </summary>
		protected long m_lastAttackedByEnemyTickPvP;
		/// <summary>
		/// gets/sets gametick when this living was last time attacked by an enemy in pvp
		/// </summary>
		public virtual long LastAttackedByEnemyTickPvP
		{
			get { return m_lastAttackedByEnemyTickPvP; }
			set
			{
				m_lastAttackedByEnemyTickPvP = value;
				if (this is GameNPC)
				{
					if ((this as GameNPC).Brain is IControlledBrain)
					{
						((this as GameNPC).Brain as IControlledBrain).Owner.LastAttackedByEnemyTickPvP = value;
					}
				}
			}
		}

		/// <summary>
		/// Total damage RvR Value
		/// </summary>
		protected long m_damageRvRMemory;
		/// <summary>
		/// gets the DamageRvR Memory of this living (always 0 for Gameliving)
		/// </summary>
		public virtual long DamageRvRMemory
		{
			get { return 0; }
			set
			{
				m_damageRvRMemory = 0;
			}
		}

		/// <summary>
		/// calculate item armor factor influenced by quality, con and duration
		/// </summary>
		/// <param name="slot"></param>
		/// <returns></returns>
		public virtual double GetArmorAF(eArmorSlot slot)
		{
			return GetModified(eProperty.ArmorFactor);
		}

		/// <summary>
		/// Calculates armor absorb level
		/// </summary>
		/// <param name="slot"></param>
		/// <returns></returns>
		public virtual double GetArmorAbsorb(eArmorSlot slot)
		{
			return GetModified(eProperty.ArmorAbsorption) * 0.01;
		}

		/// <summary>
		/// Returns the weapon used to attack, null=natural
		/// </summary>
		public virtual InventoryItem AttackWeapon
		{
			get
			{
				if (Inventory != null)
				{
					switch (ActiveWeaponSlot)
					{
							case eActiveWeaponSlot.Standard: return Inventory.GetItem(eInventorySlot.RightHandWeapon);
							case eActiveWeaponSlot.TwoHanded: return Inventory.GetItem(eInventorySlot.TwoHandWeapon);
							case eActiveWeaponSlot.Distance: return Inventory.GetItem(eInventorySlot.DistanceWeapon);
					}
				}
				return null;
			}
		}

		/// <summary>
		/// Gets the attack-state of this living
		/// </summary>
		public virtual bool AttackState { get; protected set; }

		/// <summary>
		/// Whether or not the living can be attacked.
		/// </summary>
		public override bool IsAttackable
		{
			get
			{
				return (IsAlive &&
				        !IsStealthed &&
				        ObjectState == GameObject.eObjectState.Active);
			}
		}

		/// <summary>
		/// Whether the living is actually attacking something.
		/// </summary>
		public virtual bool IsAttacking
		{
			get { return (AttackState); }
		}

		/// <summary>
		/// Whether this living is crowd controlled.
		/// </summary>
		public virtual bool IsCrowdControlled
		{
			get
			{
				return (IsStunned || IsMezzed);
			}
		}

		/// <summary>
		/// Whether this living can actually do anything.
		/// </summary>
		public virtual bool IsIncapacitated
		{
			get
			{
				return (ObjectState != eObjectState.Active || !IsAlive || IsStunned || IsMezzed);
			}
		}
		
		/// <summary>
		/// returns if this living is alive
		/// </summary>
		public virtual bool IsAlive
		{
			get { return Health > 0; }
		}

		/// <summary>
		/// True if living is low on health, else false.
		/// </summary>
		public virtual bool IsLowHealth
		{
			get
			{
				return (Health < 0.1 * MaxHealth);
			}
		}

		protected bool m_isMuted = false;
		/// <summary>
		/// returns if this living is muted
		/// </summary>
		public virtual bool IsMuted
		{
			get { return m_isMuted; }
			set
			{
				m_isMuted = value;
			}
		}

		/// <summary>
		/// Check this flag to see if this living is involved in combat
		/// </summary>
		public virtual bool InCombat
		{
			get
			{
				if ((InCombatPvE || InCombatPvP) == false)
					return false;
				return true;
			}
		}

		/// <summary>
		/// Check this flag to see if this living has been involved in combat in the given milliseconds
		/// </summary>
		public virtual bool InCombatInLast(int milliseconds)
		{
			if ((InCombatPvEInLast(milliseconds) || InCombatPvPInLast(milliseconds)) == false)
				return false;
			return true;
		}

		/// <summary>
		/// checks if the living is involved in pvp combat
		/// </summary>
		public virtual bool InCombatPvP
		{
			get
			{
				Region region = CurrentRegion;
				if (region == null)
					return false;

				if (LastCombatTickPvP == 0)
					return false;

				return LastCombatTickPvP + 10000 >= region.Time;
			}
		}

		/// <summary>
		/// checks if the living is involved in pvp combat in the given milliseconds
		/// </summary>
		public virtual bool InCombatPvPInLast(int milliseconds)
		{
			Region region = CurrentRegion;
			if (region == null)
				return false;

			if (LastCombatTickPvP == 0)
				return false;

			return LastCombatTickPvP + milliseconds >= region.Time;
		}

		/// <summary>
		/// checks if the living is involved in pve combat
		/// </summary>
		public virtual bool InCombatPvE
		{
			get
			{
				Region region = CurrentRegion;
				if (region == null)
					return false;

				if (LastCombatTickPvE == 0)
					return false;

				return LastCombatTickPvE + 10000 >= region.Time;
			}
		}

		/// <summary>
		/// checks if the living is involved in pve combat in the given milliseconds
		/// </summary>
		public virtual bool InCombatPvEInLast(int milliseconds)
		{
			Region region = CurrentRegion;
			if (region == null)
				return false;

			if (LastCombatTickPvE == 0)
				return false;

			//if (LastCombatTickPvE + 10000 - region.Time > 0 && this is GameNPC && (this as GameNPC).Brain is IControlledBrain)
			//	log.Debug(Name + " in combat " + (LastCombatTickPvE + 10000 - region.Time));

			return LastCombatTickPvE + milliseconds >= region.Time;
		}

		/// <summary>
		/// Returns the amount of experience this living is worth
		/// </summary>
		public virtual long ExperienceValue
		{
			get
			{
				return GetExperienceValueForLevel(Level);
			}
		}

		/// <summary>
		/// Realm point value of this living
		/// </summary>
		public virtual int RealmPointsValue
		{
			get { return 0; }
		}

		/// <summary>
		/// Bounty point value of this living
		/// </summary>
		public virtual int BountyPointsValue
		{
			get { return 0; }
		}

		/// <summary>
		/// Money value of this living
		/// </summary>
		public virtual long MoneyValue
		{
			get { return 0; }
		}

		/// <summary>
		/// How much over the XP cap can this living reward.
		/// 1.0 = none
		/// 2.0 = twice cap
		/// etc.
		/// </summary>
		public virtual double ExceedXPCapAmount
		{
			get { return 1.0; }
		}

		#region XP array

		/// <summary>
		/// Holds pre calculated experience values of the living for special levels
		/// </summary>
		public static readonly long[] XPForLiving =
		{
			// noret: first 52 are from exp table, think mythic has changed some values
			// cause they don't fit the formula; rest are calculated.
			// with this formula group with 8 lv50 players should hit cap on lv67 mobs what looks about correct
			// http://www.daocweave.com/daoc/general/experience_table.htm
			5,					// xp for level 0
			10,					// xp for level 1
			20,					// xp for level 2
			40,					// xp for level 3
			80,					// xp for level 4
			160,				// xp for level 5
			320,				// xp for level 6
			640,				// xp for level 7
			1280,				// xp for level 8
			2560,				// xp for level 9
			5120,				// xp for level 10
			7240,				// xp for level 11
			10240,				// xp for level 12
			14480,				// xp for level 13
			20480,				// xp for level 14
			28980,				// xp for level 15
			40960,				// xp for level 16
			57930,				// xp for level 17
			81920,				// xp for level 18
			115850,				// xp for level 19
			163840,				// xp for level 20
			206435,				// xp for level 21
			231705,				// xp for level 22
			327680,				// xp for level 23
			412850,				// xp for level 24
			520160,				// xp for level 25
			655360,				// xp for level 26
			825702,				// xp for level 27
			1040319,			// xp for level 28
			1310720,			// xp for level 29
			1651404,			// xp for level 30
			2080638,			// xp for level 31
			2621440,			// xp for level 32
			3302807,			// xp for level 33
			4161277,			// xp for level 34
			5242880,			// xp for level 35
			6022488,			// xp for level 36
			6918022,			// xp for level 37
			7946720,			// xp for level 38
			9128384,			// xp for level 39
			10485760,			// xp for level 40
			12044975,			// xp for level 41
			13836043,			// xp for level 42
			15893440,			// xp for level 43
			18258769,			// xp for level 44
			20971520,			// xp for level 45
			24089951,			// xp for level 46
			27672087,			// xp for level 47
			31625241,			// xp for level 48; sshot505.tga
			36513537,			// xp for level 49
			41943040,			// xp for level 50
			48179911,			// xp for level 51
			52428800,			// xp for level 52
			63573760,			// xp for level 53
			73027074,			// xp for level 54
			83886080,			// xp for level 55
			96359802,			// xp for level 56
			110688346,			// xp for level 57
			127147521,			// xp for level 58
			146054148,			// xp for level 59
			167772160,			// xp for level 60
			192719604,			// xp for level 61
			221376692,			// xp for level 62
			254295042,			// xp for level 63
			292108296,			// xp for level 64
			335544320,			// xp for level 65
			385439208,			// xp for level 66
			442753384,			// xp for level 67
			508590084,			// xp for level 68
			584216593,			// xp for level 69
			671088640,			// xp for level 70
			770878416,			// xp for level 71
			885506769,			// xp for level 72
			1017180169,			// xp for level 73
			1168433187,			// xp for level 74
			1342177280,			// xp for level 75
			1541756833,			// xp for level 76
			1771013538,			// xp for level 77
			2034360338,			// xp for level 78
			2336866374,			// xp for level 79
			2684354560,			// xp for level 80
			3083513667,			// xp for level 81
			3542027077,			// xp for level 82
			4068720676,			// xp for level 83
			4673732748,			// xp for level 84
			5368709120,			// xp for level 85
			6167027334,			// xp for level 86
			7084054154,			// xp for level 87
			8137441353,			// xp for level 88
			9347465497,			// xp for level 89
			10737418240,		// xp for level 90
			12334054669,		// xp for level 91
			14168108308,		// xp for level 92
			16274882707,		// xp for level 93
			18694930994,		// xp for level 94
			21474836480,		// xp for level 95
			24668109338,		// xp for level 96
			28336216617,		// xp for level 97
			32549765415,		// xp for level 98
			37389861988,		// xp for level 99
			42949672960			// xp for level 100
		};

		/// <summary>
		/// Holds the level of target at which no exp is given
		/// </summary>
		public static readonly int[] NoXPForLevel =
		{
			-3,		//for level 0
			-2,		//for level 1
			-1,		//for level 2
			0,		//for level 3
			1,		//for level 4
			2,		//for level 5
			3,		//for level 6
			4,		//for level 7
			5,		//for level 8
			6,		//for level 9
			6,		//for level 10
			6,		//for level 11
			6,		//for level 12
			7,		//for level 13
			8,		//for level 14
			9,		//for level 15
			10,		//for level 16
			11,		//for level 17
			12,		//for level 18
			13,		//for level 19
			13,		//for level 20
			13,		//for level 21
			13,		//for level 22
			14,		//for level 23
			15,		//for level 24
			16,		//for level 25
			17,		//for level 26
			18,		//for level 27
			19,		//for level 28
			20,		//for level 29
			21,		//for level 30
			22,		//for level 31
			23,		//for level 32
			24,		//for level 33
			25,		//for level 34
			25,		//for level 35
			25,		//for level 36
			25,		//for level 37
			25,		//for level 38
			25,		//for level 39
			25,		//for level 40
			26,		//for level 41
			27,		//for level 42
			28,		//for level 43
			29,		//for level 44
			30,		//for level 45
			31,		//for level 46
			32,		//for level 47
			33,		//for level 48
			34,		//for level 49
			35,		//for level 50
		};

		#endregion

		/// <summary>
		/// Checks whether object is grey con to this living
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public virtual bool IsObjectGreyCon(GameObject obj)
		{
			return IsObjectGreyCon(this, obj);
		}

		/// <summary>
		/// Checks whether target is grey con to source
		/// </summary>
		/// <param name="source"></param>
		/// <param name="target"></param>
		/// <returns></returns>
		static public bool IsObjectGreyCon(GameObject source, GameObject target)
		{
			int sourceLevel = source.EffectiveLevel;
			if (sourceLevel < GameLiving.NoXPForLevel.Length)
			{
				//if target level is less or equals to level that is grey to source
				if (target.EffectiveLevel <= GameLiving.NoXPForLevel[sourceLevel])
					return true;
			}
			else
			{
				if (source.GetConLevel(target) <= -3)
					return true;
			}
			return false;
		}

		/// <summary>
		/// Calculates the experience value of this living for special levels
		/// </summary>
		/// <param name="level"></param>
		/// <returns></returns>
		public virtual long GetExperienceValueForLevel(int level)
		{
			return GameServer.ServerRules.GetExperienceForLiving(level);
		}

		/// <summary>
		/// Gets/sets the targetObject's visibility
		/// </summary>
		public virtual bool TargetInView
        { get { return true; } set { } }

		/// <summary>
		/// Gets or sets the GroundTargetObject's visibility
		/// </summary>
		public virtual bool GroundTargetInView
		{
			get { return true; }
			set { }
		}

		/// <summary>
		/// Base chance this living can be interrupted
		/// </summary>
		public virtual int BaseInterruptChance
		{
			get { return 65; }
		}

		/// <summary>
		/// Gets/Sets the item that is used for ranged attack
		/// </summary>
		/// <returns>Item that will be used for range/accuracy/damage modifications</returns>
		protected virtual InventoryItem RangeAttackAmmo
		{
			get { return null; }
			set { }
		}
        
		protected bool IsValidTarget
		{
			get
			{
				return m_ObjectState == eObjectState.Active;
			}
		}

		/// <summary>
		/// This method is called whenever this living
		/// should take damage from some source
		/// </summary>
		/// <param name="source">the damage source</param>
		/// <param name="damageType">the damage type</param>
		/// <param name="damageAmount">the amount of damage</param>
		/// <param name="criticalAmount">the amount of critical damage</param>
		public override void TakeDamage(GameObject source, eDamageType damageType, int damageAmount, int criticalAmount)
		{
			base.TakeDamage(source, damageType, damageAmount, criticalAmount);
			bool wasAlive = IsAlive;

			Health -= damageAmount + criticalAmount;

			if (!IsAlive)
			{
				if (wasAlive)
					Die(source);
			}
			else
			{
				if (IsLowHealth)
					Notify(GameLivingEvent.LowHealth, this, null);
			}
		}

		/// <summary>
		/// Changes the health
		/// </summary>
		/// <param name="changeSource">the source that inited the changes</param>
		/// <param name="healthChangeType">the change type</param>
		/// <param name="changeAmount">the change amount</param>
		/// <returns>the amount really changed</returns>
		public virtual int ChangeHealth(GameObject changeSource, eHealthChangeType healthChangeType, int changeAmount)
		{
			//TODO fire event that might increase or reduce the amount
			int oldHealth = Health;
			Health += changeAmount;
			int healthChanged = Health - oldHealth;
			return healthChanged;
		}

		/// <summary>
		/// Changes the mana
		/// </summary>
		/// <param name="changeSource">the source that inited the changes</param>
		/// <param name="manaChangeType">the change type</param>
		/// <param name="changeAmount">the change amount</param>
		/// <returns>the amount really changed</returns>
		public virtual int ChangeMana(GameObject changeSource, eManaChangeType manaChangeType, int changeAmount)
		{
			//TODO fire event that might increase or reduce the amount
			int oldMana = Mana;
			Mana += changeAmount;
			return Mana - oldMana;
		}

		/// <summary>
		/// Changes the endurance
		/// </summary>
		/// <param name="changeSource">the source that inited the changes</param>
		/// <param name="enduranceChangeType">the change type</param>
		/// <param name="changeAmount">the change amount</param>
		/// <returns>the amount really changed</returns>
		public virtual int ChangeEndurance(GameObject changeSource, eEnduranceChangeType enduranceChangeType, int changeAmount)
		{
			//TODO fire event that might increase or reduce the amount
			int oldEndurance = Endurance;
			Endurance += changeAmount;
			return Endurance - oldEndurance;
		}

		/// <summary>
		/// Called when this living dies
		/// </summary>
		public virtual void Die(GameObject killer)
		{
			GameServer.ServerRules.OnLivingKilled(this, killer);

			m_attackers.Clear();

			// cancel all concentration effects
			ConcentrationEffects.CancelAll();

			TargetObject = null;

			// cancel all left effects
			EffectList.CancelAll();

			// Stop the regeneration timers
			StopHealthRegeneration();
			StopPowerRegeneration();
			StopEnduranceRegeneration();

			//Reduce health to zero
			Health = 0;

			// Remove all last attacked times
			
			LastAttackedByEnemyTickPvE = 0;
			LastAttackedByEnemyTickPvP = 0;
			//Let's send the notification at the end
			Notify(GameLivingEvent.Dying, this, new DyingEventArgs(killer));
		}

		/// <summary>
		/// Called when the living is gaining experience
		/// </summary>
		/// <param name="expTotal">total amount of xp to gain</param>
		/// <param name="expCampBonus">camp bonus to display</param>
		/// <param name="expGroupBonus">group bonus to display</param>
		/// <param name="expOutpostBonus">outpost bonux to display</param>
		/// <param name="sendMessage">should exp gain message be sent</param>
		/// <param name="allowMultiply">should the xp amount be multiplied</param>
		public virtual void GainExperience(eXPSource xpSource, long expTotal, long expCampBonus, long expGroupBonus, long expOutpostBonus, bool sendMessage, bool allowMultiply, bool notify)
		{
			if (expTotal > 0 && notify) Notify(GameLivingEvent.GainedExperience, this, new GainedExperienceEventArgs(expTotal, expCampBonus, expGroupBonus, expOutpostBonus, sendMessage, allowMultiply, xpSource));
		}
		/// <summary>
		/// Called when this living gains realm points
		/// </summary>
		/// <param name="amount">amount of realm points gained</param>
		public virtual void GainRealmPoints(long amount)
		{
			Notify(GameLivingEvent.GainedRealmPoints, this, new GainedRealmPointsEventArgs(amount));
		}
		/// <summary>
		/// Called when this living gains bounty points
		/// </summary>
		/// <param name="amount"></param>
		public virtual void GainBountyPoints(long amount)
		{
			Notify(GameLivingEvent.GainedBountyPoints, this, new GainedBountyPointsEventArgs(amount));
		}
		/// <summary>
		/// Called when the living is gaining experience
		/// </summary>
		/// <param name="exp">base amount of xp to gain</param>
		public void GainExperience(eXPSource xpSource, long exp)
		{
			GainExperience(xpSource, exp, 0, 0, 0, true, false, true);
		}

		/// <summary>
		/// Called when the living is gaining experience
		/// </summary>
		/// <param name="exp">base amount of xp to gain</param>
		/// <param name="allowMultiply">Do we allow the xp to be multiplied</param>
		public void GainExperience(eXPSource xpSource, long exp, bool allowMultiply)
		{
			GainExperience(xpSource, exp, 0, 0, 0, true, allowMultiply, true);
		}

		/// <summary>
		/// Called when an enemy of this living is killed
		/// </summary>
		/// <param name="enemy">enemy killed</param>
		public virtual void EnemyKilled(GameLiving enemy)
		{
			Notify(GameLivingEvent.EnemyKilled, this, new EnemyKilledEventArgs(enemy));
		}

		/// <summary>
		/// Checks whether Living has ability to use lefthanded weapons
		/// </summary>
		public virtual bool CanUseLefthandedWeapon
		{
			get { return false; }
			set { }
		}

		/// <summary>
		/// Holds visible active weapon slots
		/// </summary>
		protected byte m_visibleActiveWeaponSlots = 0xFF; // none by default

		/// <summary>
		/// Gets visible active weapon slots
		/// </summary>
		public byte VisibleActiveWeaponSlots
		{
			get { return m_visibleActiveWeaponSlots; }
			set { m_visibleActiveWeaponSlots=value; }
		}

		/// <summary>
		/// Holds the living's cloak hood state
		/// </summary>
		protected bool m_isCloakHoodUp;

		/// <summary>
		/// Sets/gets the living's cloak hood state
		/// </summary>
		public virtual bool IsCloakHoodUp
		{
			get { return m_isCloakHoodUp; }
			set { m_isCloakHoodUp = value; }
		}

		/// <summary>
		/// Holds the living's cloak hood state
		/// </summary>
		protected bool m_IsCloakInvisible = false;

		/// <summary>
		/// Sets/gets the living's cloak visible state
		/// </summary>
		public virtual bool IsCloakInvisible
		{
			get { return m_IsCloakInvisible; }
			set { m_IsCloakInvisible = value; }
		}

		/// <summary>
		/// Holds the living's helm visible state
		/// </summary>
		protected bool m_IsHelmInvisible = false;

		/// <summary>
		/// Sets/gets the living's cloak hood state
		/// </summary>
		public virtual bool IsHelmInvisible
		{
			get { return m_IsHelmInvisible; }
			set { m_IsHelmInvisible = value; }
		}

		/// <summary>
		/// Switches the active weapon to another one
		/// </summary>
		/// <param name="slot">the new eActiveWeaponSlot</param>
		public virtual void SwitchWeapon(eActiveWeaponSlot slot)
		{
			if (Inventory == null)
				return;

			InventoryItem rightHandSlot = Inventory.GetItem(eInventorySlot.RightHandWeapon);
			InventoryItem leftHandSlot = Inventory.GetItem(eInventorySlot.LeftHandWeapon);
			InventoryItem twoHandSlot = Inventory.GetItem(eInventorySlot.TwoHandWeapon);
			InventoryItem distanceSlot = Inventory.GetItem(eInventorySlot.DistanceWeapon);

			// simple active slot logic:
			// 0=right hand, 1=left hand, 2=two-hand, 3=range, F=none
			int rightHand = (VisibleActiveWeaponSlots & 0x0F);
			int leftHand = (VisibleActiveWeaponSlots & 0xF0) >> 4;


			// set new active weapon slot
			switch (slot)
			{
				case eActiveWeaponSlot.Standard:
					{
						if (rightHandSlot == null)
							rightHand = 0xFF;
						else
							rightHand = 0x00;

						if (leftHandSlot == null)
							leftHand = 0xFF;
						else
							leftHand = 0x01;
					}
					break;

				case eActiveWeaponSlot.TwoHanded:
					{
						if (twoHandSlot != null && (twoHandSlot.Hand == 1 || this is GameNPC)) // 2h
						{
							rightHand = leftHand = 0x02;
							break;
						}

						// 1h weapon in 2h slot
						if (twoHandSlot == null)
							rightHand = 0xFF;
						else
							rightHand = 0x02;

						if (leftHandSlot == null)
							leftHand = 0xFF;
						else
							leftHand = 0x01;
					}
					break;

				case eActiveWeaponSlot.Distance:
					{
						leftHand = 0xFF; // cannot use left-handed weapons if ranged slot active

						if (distanceSlot == null)
							rightHand = 0xFF;
						else if (distanceSlot.Hand == 1 || this is GameNPC) // NPC equipment does not have hand so always assume 2 handed bow
							rightHand = leftHand = 0x03; // bows use 2 hands, throwing axes 1h
						else
							rightHand = 0x03;
					}
					break;
			}

			m_activeWeaponSlot = slot;

			// pack active weapon slots value back
			m_visibleActiveWeaponSlots = (byte)(((leftHand & 0x0F) << 4) | (rightHand & 0x0F));
		}
		#endregion
		#region Property/Bonus/Buff/PropertyCalculator fields
		/// <summary>
		/// Array for property boni for abilities
		/// </summary>
		protected IPropertyIndexer m_abilityBonus = new PropertyIndexer();
		/// <summary>
		/// Ability bonus property
		/// </summary>
		public virtual IPropertyIndexer AbilityBonus
		{
			get { return m_abilityBonus; }
		}

		/// <summary>
		/// Array for property boni by items
		/// </summary>
		protected IPropertyIndexer m_itemBonus = new PropertyIndexer();
		/// <summary>
		/// Property Item Bonus field
		/// </summary>
		public virtual IPropertyIndexer ItemBonus
		{
			get { return m_itemBonus; }
		}


		/// <summary>
		/// Array for buff boni
		/// </summary>
		protected IPropertyIndexer m_buff1Bonus = new PropertyIndexer();
		/// <summary>
		/// Property Buff bonus category
		/// what it means depends from the PropertyCalculator for a property element
		/// </summary>
		public IPropertyIndexer BaseBuffBonusCategory
		{
			get { return m_buff1Bonus; }
		}

		/// <summary>
		/// Array for second buff boni
		/// </summary>
		protected IPropertyIndexer m_buff2Bonus = new PropertyIndexer();
		/// <summary>
		/// Property Buff bonus category
		/// what it means depends from the PropertyCalculator for a property element
		/// </summary>
		public IPropertyIndexer SpecBuffBonusCategory
		{
			get { return m_buff2Bonus; }
		}

		/// <summary>
		/// Array for third debuff boni
		/// </summary>
		protected IPropertyIndexer m_debuffBonus = new PropertyIndexer();
		/// <summary>
		/// Property Buff bonus category
		/// what it means depends from the PropertyCalculator for a property element
		/// </summary>
		public IPropertyIndexer DebuffCategory
		{
			get { return m_debuffBonus; }
		}

		/// <summary>
		/// Array for forth buff boni
		/// </summary>
		protected IPropertyIndexer m_buff4Bonus = new PropertyIndexer();
		/// <summary>
		/// Property Buff bonus category
		/// what it means depends from the PropertyCalculator for a property element
		/// </summary>
		public IPropertyIndexer BuffBonusCategory4
		{
			get { return m_buff4Bonus; }
		}

		/// <summary>
		/// Array for first multiplicative buff boni
		/// </summary>
		protected IMultiplicativeProperties m_buffMult1Bonus = new MultiplicativePropertiesHybrid();
		/// <summary>
		/// Property Buff bonus category
		/// what it means depends from the PropertyCalculator for a property element
		/// </summary>
		public IMultiplicativeProperties BuffBonusMultCategory1
		{
			get { return m_buffMult1Bonus; }
		}

		/// <summary>
		/// Array for spec debuff boni
		/// </summary>
		protected IPropertyIndexer m_specDebuffBonus = new PropertyIndexer();
		/// <summary>
		/// Property Buff bonus category
		/// what it means depends from the PropertyCalculator for a property element
		/// </summary>
		public IPropertyIndexer SpecDebuffCategory
		{
			get { return m_specDebuffBonus; }
		}
		
		/// <summary>
		/// property calculators for each property
		/// look at PropertyCalculator class for more description
		/// </summary>
		internal static readonly IPropertyCalculator[] m_propertyCalc = new IPropertyCalculator[(int)eProperty.MaxProperty+1];

		/// <summary>
		/// retrieve a property value of that living
		/// this value is modified/capped and ready to use
		/// </summary>
		/// <param name="property"></param>
		/// <returns></returns>
		public virtual int GetModified(eProperty property)
		{
			if (m_propertyCalc != null && m_propertyCalc[(int)property] != null)
			{
				return m_propertyCalc[(int)property].CalcValue(this, property);
			}
			else
			{
				log.ErrorFormat("{0} did not find property calculator for property ID {1}.", Name, (int)property);
			}
			return 0;
		}

		//Eden : secondary resists, such AoM, vampiir magic resistance etc, should not apply in CC duration, disease, debuff etc, using a new function
		public virtual int GetModifiedBase(eProperty property)
		{
			if (m_propertyCalc != null && m_propertyCalc[(int)property] != null)
			{
				return m_propertyCalc[(int)property].CalcValueBase(this, property);
			}
			else
			{
				log.ErrorFormat("{0} did not find base property calculator for property ID {1}.", Name, (int)property);
			}
			return 0;
		}

		/// <summary>
		/// Retrieve a property value of this living's buff bonuses only;
		/// caps and cap increases apply.
		/// </summary>
		/// <param name="property"></param>
		/// <returns></returns>
		public virtual int GetModifiedFromBuffs(eProperty property)
		{
			if (m_propertyCalc != null && m_propertyCalc[(int)property] != null)
			{
				return m_propertyCalc[(int)property].CalcValueFromBuffs(this, property);
			}
			else
			{
				log.ErrorFormat("{0} did not find buff property calculator for property ID {1}.", Name, (int)property);
			}
			return 0;
		}

		/// <summary>
		/// Retrieve a property value of this living's item bonuses only;
		/// caps and cap increases apply.
		/// </summary>
		/// <param name="property"></param>
		/// <returns></returns>
		public virtual int GetModifiedFromItems(eProperty property)
		{
			if (m_propertyCalc != null && m_propertyCalc[(int)property] != null)
			{
				return m_propertyCalc[(int)property].CalcValueFromItems(this, property);
			}
			else
			{
				log.ErrorFormat("{0} did not find item property calculator for property ID {1}.", Name, (int)property);
			}
			return 0;
		}

		/// <summary>
		/// has to be called after properties were changed and updates are needed
		/// TODO: not sure about property change detection, has to be reviewed
		/// </summary>
		public virtual void PropertiesChanged()
		{
		}

		#endregion
		#region Stats, Resists
		/// <summary>
		/// The name of the states
		/// </summary>
		public static readonly string[] STAT_NAMES = new string[]{"Unknown Stat","Strength", "Dexterity", "Constitution", "Quickness", "Intelligence",
			"Piety", "Empathy", "Charisma"};

		/// <summary>
		/// base values for char stats
		/// </summary>
		protected readonly short[] m_charStat = new short[8];
		/// <summary>
		/// get a unmodified char stat value
		/// </summary>
		/// <param name="stat"></param>
		/// <returns></returns>
		public int GetBaseStat(eStat stat)
		{
			return m_charStat[stat - eStat._First];
		}
		/// <summary>
		/// changes a base stat value
		/// </summary>
		/// <param name="stat"></param>
		/// <param name="amount"></param>
		public virtual void ChangeBaseStat(eStat stat, short amount)
		{
			m_charStat[stat - eStat._First] += amount;
		}

		/// <summary>
		/// this field is just for convinience and speed purposes
		/// converts the damage types to resist fields
		/// </summary>
		protected static readonly eProperty[] m_damageTypeToResistBonusConversion = new eProperty[] {
			eProperty.Resist_Natural, //0,
			eProperty.Resist_Crush,
			eProperty.Resist_Slash,
			eProperty.Resist_Thrust,
			0, 0, 0, 0, 0, 0,
			eProperty.Resist_Body,
			eProperty.Resist_Cold,
			eProperty.Resist_Energy,
			eProperty.Resist_Heat,
			eProperty.Resist_Matter,
			eProperty.Resist_Spirit
		};
		/// <summary>
		/// gets the resistance value by damage type, refer to eDamageType for constants
		/// </summary>
		/// <param name="damageType"></param>
		/// <returns></returns>
		public virtual eProperty GetResistTypeForDamage(eDamageType damageType)
		{
			if ((int)damageType < m_damageTypeToResistBonusConversion.Length)
			{
				return m_damageTypeToResistBonusConversion[(int)damageType];
			}
			else
			{
				log.ErrorFormat("No resist found for damage type {0} on living {1}!", (int)damageType, Name);
				return 0;
			}
		}
		/// <summary>
		/// gets the resistance value by damage types
		/// </summary>
		/// <param name="damageType">the damag etype</param>
		/// <returns>the resist value</returns>
		public virtual int GetResist(eDamageType damageType)
		{
			return GetModified(GetResistTypeForDamage(damageType));
		}

		public virtual int GetResistBase(eDamageType damageType)
		{
			return GetModifiedBase(GetResistTypeForDamage(damageType));
		}

		/// <summary>
		/// Get the resistance to damage by resist type
		/// </summary>
		/// <param name="property">one of the Resist_XXX properties</param>
		/// <returns>the resist value</returns>
		public virtual int GetDamageResist(eProperty property)
		{
			return SkillBase.GetRaceResist( m_race, (eResist)property );
		}

		/// <summary>
		/// Gets the Damage Resist for a damage type
		/// </summary>
		/// <param name="damageType"></param>
		/// <returns></returns>
		public virtual int GetDamageResist(eDamageType damageType)
		{
			return GetDamageResist(GetResistTypeForDamage(damageType));
		}

		/// <summary>
		/// temp properties
		/// </summary>
		private readonly PropertyCollection m_tempProps = new PropertyCollection();

		/// <summary>
		/// use it to store temporary properties on this living
		/// beware to use unique keys so they do not interfere
		/// </summary>
		public PropertyCollection TempProperties
		{
			get { return m_tempProps; }
		}

		/// <summary>
		/// Gets or Sets the effective level of the Object
		/// </summary>
		public override int EffectiveLevel
		{
			get { return GetModified(eProperty.LivingEffectiveLevel); }
		}

		/// <summary>
		/// returns the level of a specialization
		/// if 0 is returned, the spec is non existent on living
		/// </summary>
		/// <param name="keyName"></param>
		/// <returns></returns>
		public virtual int GetBaseSpecLevel(string keyName)
		{
			return Level;
		}

		/// <summary>
		/// returns the level of a specialization + bonuses from RR and Items
		/// if 0 is returned, the spec is non existent on the living
		/// </summary>
		/// <param name="keyName"></param>
		/// <returns></returns>
		public virtual int GetModifiedSpecLevel(string keyName)
		{
			return Level;
		}

		#endregion
		#region Regeneration
		/// <summary>
		/// GameTimer used for restoring hp
		/// </summary>
		protected RegionTimer m_healthRegenerationTimer;
		/// <summary>
		/// GameTimer used for restoring mana
		/// </summary>
		protected RegionTimer m_powerRegenerationTimer;
		/// <summary>
		/// GameTimer used for restoring endurance
		/// </summary>
		protected RegionTimer m_enduRegenerationTimer;

		/// <summary>
		/// The default frequency of regenerating health in milliseconds
		/// </summary>
		protected const ushort m_healthRegenerationPeriod = 3000;

		/// <summary>
		/// Interval for health regeneration tics
		/// </summary>
		protected virtual ushort HealthRegenerationPeriod
		{
			get { return m_healthRegenerationPeriod; }
		}

		/// <summary>
		/// The default frequency of regenerating power in milliseconds
		/// </summary>
		protected const ushort m_powerRegenerationPeriod = 3000;

		/// <summary>
		/// Interval for power regeneration tics
		/// </summary>
		protected virtual ushort PowerRegenerationPeriod
		{
			get { return m_powerRegenerationPeriod; }
		}

		/// <summary>
		/// The default frequency of regenerating endurance in milliseconds
		/// </summary>
		protected const ushort m_enduranceRegenerationPeriod = 1000;

		/// <summary>
		/// Interval for endurance regeneration tics
		/// </summary>
		protected virtual ushort EnduranceRegenerationPeriod
		{
			get { return m_enduranceRegenerationPeriod; }
		}

		/// <summary>
		/// The lock object for lazy regen timers initialization
		/// </summary>
		protected readonly object m_regenTimerLock = new object();

		/// <summary>
		/// Starts the health regeneration
		/// </summary>
		public virtual void StartHealthRegeneration()
		{
			if (ObjectState != eObjectState.Active)
				return;
			lock (m_regenTimerLock)
			{
				if (m_healthRegenerationTimer == null)
				{
					m_healthRegenerationTimer = new RegionTimer(this);
					m_healthRegenerationTimer.Callback = new RegionTimerCallback(HealthRegenerationTimerCallback);
				}
				else if (m_healthRegenerationTimer.IsAlive)
				{
					return;
				}

				m_healthRegenerationTimer.Start(HealthRegenerationPeriod);
			}
		}
		/// <summary>
		/// Starts the power regeneration
		/// </summary>
		public virtual void StartPowerRegeneration()
		{
			if (ObjectState != eObjectState.Active)
				return;
			lock (m_regenTimerLock)
			{
				if (m_powerRegenerationTimer == null)
				{
					m_powerRegenerationTimer = new RegionTimer(this);
					m_powerRegenerationTimer.Callback = new RegionTimerCallback(PowerRegenerationTimerCallback);
				}
				else if (m_powerRegenerationTimer.IsAlive)
				{
					return;
				}

				m_powerRegenerationTimer.Start(PowerRegenerationPeriod);
			}
		}
		/// <summary>
		/// Starts the endurance regeneration
		/// </summary>
		public virtual void StartEnduranceRegeneration()
		{
			if (ObjectState != eObjectState.Active)
				return;
			lock (m_regenTimerLock)
			{
				if (m_enduRegenerationTimer == null)
				{
					m_enduRegenerationTimer = new RegionTimer(this);
					m_enduRegenerationTimer.Callback = new RegionTimerCallback(EnduranceRegenerationTimerCallback);
				}
				else if (m_enduRegenerationTimer.IsAlive)
				{
					return;
				}
				m_enduRegenerationTimer.Start(EnduranceRegenerationPeriod);
			}
		}
		/// <summary>
		/// Stop the health regeneration
		/// </summary>
		public virtual void StopHealthRegeneration()
		{
			lock (m_regenTimerLock)
			{
				if (m_healthRegenerationTimer == null)
					return;
				m_healthRegenerationTimer.Stop();
				m_healthRegenerationTimer = null;
			}
		}
		/// <summary>
		/// Stop the power regeneration
		/// </summary>
		public virtual void StopPowerRegeneration()
		{
			lock (m_regenTimerLock)
			{
				if (m_powerRegenerationTimer == null)
					return;
				m_powerRegenerationTimer.Stop();
				m_powerRegenerationTimer = null;
			}
		}
		/// <summary>
		/// Stop the endurance regeneration
		/// </summary>
		public virtual void StopEnduranceRegeneration()
		{
			lock (m_regenTimerLock)
			{
				if (m_enduRegenerationTimer == null)
					return;
				m_enduRegenerationTimer.Stop();
				m_enduRegenerationTimer = null;
			}
		}
		/// <summary>
		/// Timer callback for the hp regeneration
		/// </summary>
		/// <param name="callingTimer">timer calling this function</param>
		protected virtual int HealthRegenerationTimerCallback(RegionTimer callingTimer)
		{
			if (Health < MaxHealth)
			{
				ChangeHealth(this, eHealthChangeType.Regenerate, GetModified(eProperty.HealthRegenerationRate));
			}

			//If we are fully healed, we stop the timer
			if (Health >= MaxHealth)
				return 0;

			if (InCombat)
			{
				// in combat each tic is aprox 15 seconds - tolakram
				return HealthRegenerationPeriod * 5;
			}

			//Heal at standard rate
			return HealthRegenerationPeriod;
		}
		/// <summary>
		/// Callback for the power regenerationTimer
		/// </summary>
		/// <param name="selfRegenerationTimer">timer calling this function</param>
		protected virtual int PowerRegenerationTimerCallback(RegionTimer selfRegenerationTimer)
		{
			if (this is GamePlayer &&
			    (((GamePlayer)this).CharacterClass.ID == (int)eCharacterClass.Vampiir ||
			     (((GamePlayer)this).CharacterClass.ID > 59 && ((GamePlayer)this).CharacterClass.ID < 63))) // Maulers
			{
				double MinMana = MaxMana * 0.15;
				double OnePercMana = Math.Ceiling(MaxMana * 0.01);

				if (!InCombat)
				{
					if (ManaPercent < 15)
					{
						ChangeMana(this, eManaChangeType.Regenerate, (int)OnePercMana);
						return 4000;
					}
					else if (ManaPercent > 15)
					{
						ChangeMana(this, eManaChangeType.Regenerate, (int)(-OnePercMana));
						return 1000;
					}

					return 0;
				}
			}
			else
			{
				if (Mana < MaxMana)
				{
					ChangeMana(this, eManaChangeType.Regenerate, GetModified(eProperty.PowerRegenerationRate));
				}

				//If we are full, we stop the timer
				if (Mana >= MaxMana)
				{
					return 0;
				}
			}

			//If we were hit before we regenerated, we regenerate slower the next time
			if (InCombat)
			{
				return (int)(PowerRegenerationPeriod * 3.4);
			}

			//regen at standard rate
			return PowerRegenerationPeriod;
		}
		/// <summary>
		/// Callback for the endurance regenerationTimer
		/// </summary>
		/// <param name="selfRegenerationTimer">timer calling this function</param>
		protected virtual int EnduranceRegenerationTimerCallback(RegionTimer selfRegenerationTimer)
		{
			if (Endurance < MaxEndurance)
			{
				int regen = GetModified(eProperty.EnduranceRegenerationRate);
				if (regen > 0)
				{
					ChangeEndurance(this, eEnduranceChangeType.Regenerate, regen);
				}
			}
			if (Endurance >= MaxEndurance) return 0;

			return 500 + Util.Random(EnduranceRegenerationPeriod);
		}
		#endregion

		#region Mana/Health/Endurance/Concentration/Delete
		/// <summary>
		/// Amount of mana
		/// </summary>
		protected int m_mana;
		/// <summary>
		/// Amount of endurance
		/// </summary>
		protected int m_endurance;
		/// <summary>
		/// Maximum value that can be in m_endurance
		/// </summary>
		protected int m_maxEndurance;

		/// <summary>
		/// Gets/sets the object health
		/// </summary>
		public override int Health
		{
			get { return m_health; }
			set
			{

				int maxhealth = MaxHealth;
				if (value >= maxhealth)
				{
					m_health = maxhealth;
				}
				else if (value > 0)
				{
					m_health = value;
				}
				else
				{
					m_health = 0;
				}

				if (IsAlive && m_health < maxhealth)
				{
					StartHealthRegeneration();
				}
			}
		}

		public override int MaxHealth
		{
			get {	return GetModified(eProperty.MaxHealth); }
		}

		public virtual int Mana
		{
			get
			{
				return m_mana;
			}
			set
			{
				int maxmana = MaxMana;
				m_mana = Math.Min(value, maxmana);
				m_mana = Math.Max(m_mana, 0);
				if (IsAlive && (m_mana < maxmana || (this is GamePlayer && ((GamePlayer)this).CharacterClass.ID == (int)eCharacterClass.Vampiir)
				                || (this is GamePlayer && ((GamePlayer)this).CharacterClass.ID > 59 && ((GamePlayer)this).CharacterClass.ID < 63)))
				{
					StartPowerRegeneration();
				}
			}
		}

		public virtual int MaxMana
		{
			get
			{
				return GetModified(eProperty.MaxMana);
			}
		}

		public virtual byte ManaPercent
		{
			get
			{
				return (byte)(MaxMana <= 0 ? 0 : ((Mana * 100) / MaxMana));
			}
		}

		/// <summary>
		/// Gets/sets the object endurance
		/// </summary>
		public virtual int Endurance
		{
			get { return m_endurance; }
			set
			{
				m_endurance = Math.Min(value, m_maxEndurance);
				m_endurance = Math.Max(m_endurance, 0);
				if (IsAlive && m_endurance < m_maxEndurance)
				{
					StartEnduranceRegeneration();
				}
			}
		}

		/// <summary>
		/// Gets or sets the maximum endurance of this living
		/// </summary>
		public virtual int MaxEndurance
		{
			get { return m_maxEndurance; }
			set
			{
				m_maxEndurance = value;
				Endurance = Endurance; //cut extra end points if there are any or start regeneration
			}
		}

		/// <summary>
		/// Gets the endurance in percent of maximum
		/// </summary>
		public virtual byte EndurancePercent
		{
			get
			{
				return (byte)(MaxEndurance <= 0 ? 0 : ((Endurance * 100) / MaxEndurance));
			}
		}

		/// <summary>
		/// Gets/sets the object concentration
		/// </summary>
		public virtual int Concentration
		{
			get { return 0; }
		}

		/// <summary>
		/// Gets/sets the object maxconcentration
		/// </summary>
		public virtual int MaxConcentration
		{
			get { return 0; }
		}

		/// <summary>
		/// Gets the concentration in percent of maximum
		/// </summary>
		public virtual byte ConcentrationPercent
		{
			get
			{
				return (byte)(MaxConcentration <= 0 ? 0 : ((Concentration * 100) / MaxConcentration));
			}
		}

		/// <summary>
		/// Holds the concentration effects list
		/// </summary>
		private readonly ConcentrationList m_concEffects;
		/// <summary>
		/// Gets the concentration effects list
		/// </summary>
		public ConcentrationList ConcentrationEffects { get { return m_concEffects; } }

		/// <summary>
		/// Cancels all concentration effects by this living and on this living
		/// </summary>
		public void CancelAllConcentrationEffects()
		{
			CancelAllConcentrationEffects(false);
		}

		/// <summary>
		/// Cancels all concentration effects by this living and on this living
		/// </summary>
		public void CancelAllConcentrationEffects(bool leaveSelf)
		{
			// cancel conc spells
			ConcentrationEffects.CancelAll(leaveSelf);

			// cancel all active conc spell effects from other casters
			ArrayList concEffects = new ArrayList();
			lock (EffectList)
			{
				foreach (IGameEffect effect in EffectList)
				{
					if (effect is GameSpellEffect && ((GameSpellEffect)effect).Spell.Concentration > 0)
					{
						if (!leaveSelf || leaveSelf && ((GameSpellEffect)effect).SpellHandler.Caster != this)
							concEffects.Add(effect);
					}
				}
			}
			foreach (GameSpellEffect effect in concEffects)
			{
				effect.Cancel(false);
			}
		}

		#endregion
		#region Speed/Heading/Target/GroundTarget/GuildName/SitState/Level
		/// <summary>
		/// The targetobject of this living
		/// This is a weak reference to a GameObject, which
		/// means that the gameobject can be cleaned up even
		/// when this living has a reference on it ...
		/// </summary>
		protected readonly WeakReference m_targetObjectWeakReference;
		/// <summary>
		/// The current speed of this living
		/// </summary>
		protected short m_currentSpeed;
		/// <summary>
		/// The base maximum speed of this living
		/// </summary>
		protected short m_maxSpeedBase;
		/// <summary>
		/// Holds the Living's Coordinate inside the current Region
		/// </summary>
		protected Point3D m_groundTarget;

		/// <summary>
		/// Gets the current direction the Object is facing
		/// </summary>
		public override ushort Heading
		{
			get { return base.Heading; }
			set
			{
				ushort oldHeading = base.Heading;
				base.Heading = value;
				if (base.Heading != oldHeading)
					UpdateTickSpeed();
			}
		}

		private bool m_fixedSpeed = false;

		/// <summary>
		/// Does this NPC have a fixed speed, unchanged by any modifiers?
		/// </summary>
		public virtual bool FixedSpeed
		{
			get { return m_fixedSpeed; }
			set { m_fixedSpeed = value; }
		}

		/// <summary>
		/// Gets or sets the current speed of this living
		/// </summary>
		public virtual short CurrentSpeed
		{
			get
			{
				return m_currentSpeed;
			}
			set
			{
				m_currentSpeed = value;
				UpdateTickSpeed();
			}
		}

		/// <summary>
		/// Gets the maxspeed of this living
		/// </summary>
		public virtual short MaxSpeed
		{
			get
			{
				if (FixedSpeed)
					return MaxSpeedBase;

				return (short)GetModified(eProperty.MaxSpeed);
			}
		}

		/// <summary>
		/// Gets or sets the base max speed of this living
		/// </summary>
		public virtual short MaxSpeedBase
		{
			get { return m_maxSpeedBase; }
			set { m_maxSpeedBase = value; }
		}

		/// <summary>
		/// Gets or sets the target of this living
		/// </summary>
		public virtual GameObject TargetObject
		{
			get
			{
				return (m_targetObjectWeakReference.Target as GameObject);
			}
			set
			{
				m_targetObjectWeakReference.Target = value;
			}
		}
		public virtual bool IsSitting
		{
			get { return false; }
			set { }
		}
		/// <summary>
		/// Gets the Living's ground-target Coordinate inside the current Region
		/// </summary>
		public virtual Point3D GroundTarget
		{
			get { return m_groundTarget; }
		}

		/// <summary>
		/// Sets the Living's ground-target Coordinates inside the current Region
		/// </summary>
		public virtual void SetGroundTarget(int groundX, int groundY, int groundZ)
		{
			m_groundTarget.X = groundX;
			m_groundTarget.Y = groundY;
			m_groundTarget.Z = groundZ;
		}

		/// <summary>
		/// Gets or Sets the current level of the Object
		/// </summary>
		public override byte Level
		{
			get { return base.Level; }
			set
			{
				base.Level = value;
				if (ObjectState == eObjectState.Active)
				{
					foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
					{
						if (player == null)
							continue;
						player.Out.SendLivingDataUpdate(this, false);
					}
				}
			}
		}

		/// <summary>
		/// What is the base, unmodified level of this living
		/// </summary>
		public virtual byte BaseLevel
		{
			get { return Level; }
		}

		/// <summary>
		/// Calculates the level of a skill on this living.  Generally this is simply the level of the skill.
		/// </summary>
		public virtual int CalculateSkillLevel(Skill skill)
		{
			return skill.Level;
		}


		#endregion
		#region Movement
		/// <summary>
		/// The tick speed in X direction.
		/// </summary>
		public double TickSpeedX { get; protected set; }

		/// <summary>
		/// The tick speed in Y direction.
		/// </summary>
		public double TickSpeedY { get; protected set; }

		/// <summary>
		/// The tick speed in Z direction.
		/// </summary>
		public double TickSpeedZ { get; protected set; }

		/// <summary>
		/// Updates tick speed for this living.
		/// </summary>
		protected virtual void UpdateTickSpeed()
		{
			int speed = CurrentSpeed;

			if (speed == 0)
				SetTickSpeed(0, 0, 0);
			else
			{
				// Living will move in the direction it is currently heading.

				double heading = Heading * HEADING_TO_RADIAN;
				SetTickSpeed(-Math.Sin(heading), Math.Cos(heading), 0, speed);
			}
		}

		/// <summary>
		/// Set the tick speed, that is the distance covered in one tick.
		/// </summary>
		/// <param name="dx"></param>
		/// <param name="dy"></param>
		/// <param name="dz"></param>
		protected void SetTickSpeed(double dx, double dy, double dz)
		{
			TickSpeedX = dx;
			TickSpeedY = dy;
			TickSpeedZ = dz;
		}

		/// <summary>
		/// Set the tick speed, that is the distance covered in one tick.
		/// </summary>
		/// <param name="dx"></param>
		/// <param name="dy"></param>
		/// <param name="dz"></param>
		/// <param name="speed"></param>
		protected void SetTickSpeed(double dx, double dy, double dz, int speed)
		{
			double tickSpeed = speed * 0.001;
			SetTickSpeed(dx * tickSpeed, dy * tickSpeed, dz * tickSpeed);
		}

		/// <summary>
		/// The tick at which the movement started.
		/// </summary>
		public int MovementStartTick { get; set; }

		/// <summary>
		/// Elapsed ticks since movement started.
		/// </summary>
		protected int MovementElapsedTicks
		{
			get { return Environment.TickCount - MovementStartTick; }
		}

		/// <summary>
		/// True if the living is moving, else false.
		/// </summary>
		public virtual bool IsMoving
		{
			get { return m_currentSpeed != 0; }
		}

		/// <summary>
		/// The current X position of this living.
		/// </summary>
		public override int X
		{
			get
			{
				return (IsMoving)
					? (int)(base.X + MovementElapsedTicks * TickSpeedX)
					: base.X;
			}
			set
			{
				base.X = value;
			}
		}

		/// <summary>
		/// The current Y position of this living.
		/// </summary>
		public override int Y
		{
			get
			{
				return (IsMoving)
					? (int)(base.Y + MovementElapsedTicks * TickSpeedY)
					: base.Y;
			}
			set
			{
				base.Y = value;
			}
		}

		/// <summary>
		/// The current Z position of this living.
		/// </summary>
		public override int Z
		{
			get
			{
				return (IsMoving)
					? (int)(base.Z + MovementElapsedTicks * TickSpeedZ)
					: base.Z;
			}
			set
			{
				base.Z = value;
			}
		}

		/// <summary>
		/// Moves the item from one spot to another spot, possible even
		/// over region boundaries
		/// </summary>
		/// <param name="regionID">new regionid</param>
		/// <param name="x">new x</param>
		/// <param name="y">new y</param>
		/// <param name="z">new z</param>
		/// <param name="heading">new heading</param>
		/// <returns>true if moved</returns>
		public override bool MoveTo(ushort regionID, int x, int y, int z, ushort heading)
		{
			if (regionID != CurrentRegionID)
				CancelAllConcentrationEffects();

			return base.MoveTo(regionID, x, y, z, heading);
		}

		/// <summary>
		/// The stealth state of this living
		/// </summary>
		public virtual bool IsStealthed
		{
			get { return false; }
		}

		#endregion
		#region Say/Yell/Whisper/Emote/Messages

		private bool m_isSilent = false;

		/// <summary>
		/// Can this living say anything?
		/// </summary>
		public virtual bool IsSilent
		{
			get { return m_isSilent; }
			set { m_isSilent = value; }
		}


		/// <summary>
		/// This function is called when this object receives a Say
		/// </summary>
		/// <param name="source">Source of say</param>
		/// <param name="str">Text that was spoken</param>
		/// <returns>true if the text should be processed further, false if it should be discarded</returns>
		public virtual bool SayReceive(GameLiving source, string str)
		{
			if (source == null || str == null)
			{
				return false;
			}
			
			Notify(GameLivingEvent.SayReceive, this, new SayReceiveEventArgs(source, this, str));
			
			return true;
		}

		/// <summary>
		/// Broadcasts a message to all living beings around this object
		/// </summary>
		/// <param name="str">string to broadcast (without any "xxx says:" in front!!!)</param>
		/// <returns>true if text was said successfully</returns>
		public virtual bool Say(string str)
		{
			if (str == null || IsSilent)
			{
				return false;
			}
			
			Notify(GameLivingEvent.Say, this, new SayEventArgs(str));
			
			foreach (GameNPC npc in GetNPCsInRadius(WorldMgr.SAY_DISTANCE))
			{
				GameNPC receiver = npc;
				// don't send say to the target, it will be whispered...
				if (receiver != this && receiver != TargetObject)
				{
					receiver.SayReceive(this, str);
				}
			}
			
			foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.SAY_DISTANCE))
			{
				GamePlayer receiver = player;
				if (receiver != this)
				{
					receiver.SayReceive(this, str);
				}
			}
			
			// whisper to Targeted NPC.
			if (TargetObject != null && TargetObject is GameNPC)
			{
				GameNPC targetNPC = (GameNPC)TargetObject;
				targetNPC.WhisperReceive(this, str);
			}
			
			return true;
		}

		/// <summary>
		/// This function is called when the living receives a yell
		/// </summary>
		/// <param name="source">GameLiving that was yelling</param>
		/// <param name="str">string that was yelled</param>
		/// <returns>true if the string should be processed further, false if it should be discarded</returns>
		public virtual bool YellReceive(GameLiving source, string str)
		{
			if (source == null || str == null)
			{
				return false;
			}
			
			Notify(GameLivingEvent.YellReceive, this, new YellReceiveEventArgs(source, this, str));
			
			return true;
		}

		/// <summary>
		/// Broadcasts a message to all living beings around this object
		/// </summary>
		/// <param name="str">string to broadcast (without any "xxx yells:" in front!!!)</param>
		/// <returns>true if text was yelled successfully</returns>
		public virtual bool Yell(string str)
		{
			if (str == null || IsSilent)
			{
				return false;
			}
			
			Notify(GameLivingEvent.Yell, this, new YellEventArgs(str));
			
			foreach (GameNPC npc in GetNPCsInRadius(WorldMgr.YELL_DISTANCE))
			{
				GameNPC receiver = npc;
				if (receiver != this)
				{
					receiver.YellReceive(this, str);
				}
			}
			
			foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.YELL_DISTANCE))
			{
				GamePlayer receiver = player;
				if (receiver != this)
				{
					receiver.YellReceive(this, str);
				}
			}
			
			return true;
		}

		/// <summary>
		/// This function is called when the Living receives a whispered text
		/// </summary>
		/// <param name="source">GameLiving that was whispering</param>
		/// <param name="str">string that was whispered</param>
		/// <returns>true if the string should be processed further, false if it should be discarded</returns>
		public virtual bool WhisperReceive(GameLiving source, string str)
		{
			if (source == null || str == null)
			{
				return false;
			}

			GamePlayer player = null;
			if (source != null && source is GamePlayer)
			{
				player = source as GamePlayer;
				long whisperdelay = player.TempProperties.getProperty<long>("WHISPERDELAY");
				if (whisperdelay > 0 && (CurrentRegion.Time - 1500) < whisperdelay && player.Client.Account.PrivLevel == 1)
				{
					//player.Out.SendMessage("Speak slower!", eChatType.CT_ScreenCenter, eChatLoc.CL_SystemWindow);
					return false;
				}
				
				player.TempProperties.setProperty("WHISPERDELAY", CurrentRegion.Time);

				foreach (DOL.GS.Quests.DataQuest q in DataQuestList)
				{
					q.Notify(GamePlayerEvent.WhisperReceive, this, new WhisperReceiveEventArgs(player, this, str));
				}
			}

			Notify(GameLivingEvent.WhisperReceive, this, new WhisperReceiveEventArgs(source, this, str));

			return true;
		}

		/// <summary>
		/// Sends a whisper to a target
		/// </summary>
		/// <param name="target">The target of the whisper</param>
		/// <param name="str">text to whisper (without any "xxx whispers:" in front!!!)</param>
		/// <returns>true if text was whispered successfully</returns>
		public virtual bool Whisper(GameObject target, string str)
		{
			if (target == null || str == null || IsSilent)
			{
				return false;
			}
			
			if (!this.IsWithinRadius(target, WorldMgr.WHISPER_DISTANCE))
			{
				return false;
			}
			
			Notify(GameLivingEvent.Whisper, this, new WhisperEventArgs(target, str));
			
			if (target is GameLiving)
			{
				return ((GameLiving)target).WhisperReceive(this, str);
			}
			
			return false;
		}
		/// <summary>
		/// Makes this living do an emote-animation
		/// </summary>
		/// <param name="emote">the emote animation to show</param>
		public virtual void Emote(eEmote emote)
		{
			foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
			{
				player.Out.SendEmoteAnimation(this, emote);
			}
		}

		/// <summary>
		/// A message to this living
		/// </summary>
		/// <param name="message"></param>
		/// <param name="type"></param>
		public virtual void MessageToSelf(string message, eChatType chatType)
		{
			// livings can't talk to themselves
		}

		/// <summary>
		/// A message from something we control
		/// </summary>
		/// <param name="message"></param>
		/// <param name="chatType"></param>
		public virtual void MessageFromControlled(string message, eChatType chatType)
		{
			// ignore for livings
		}


		#endregion
		#region Item/Money

		/// <summary>
		/// Called when the living is about to get an item from someone
		/// else
		/// </summary>
		/// <param name="source">Source from where to get the item</param>
		/// <param name="item">Item to get</param>
		/// <returns>true if the item was successfully received</returns>
		public override bool ReceiveItem(GameLiving source, InventoryItem item)
		{
			if (source == null || item == null) return false;

			Notify(GameLivingEvent.ReceiveItem, this, new ReceiveItemEventArgs(source, this, item));

			//If the item has been removed by the event handlers : return
			if (item == null || item.OwnerID == null)
			{
				return true;
			}

			if (base.ReceiveItem(source, item) == false)
			{
				if (source is GamePlayer)
				{
					((GamePlayer)source).Out.SendMessage(LanguageMgr.GetTranslation(((GamePlayer)source).Client.Account.Language, "GameLiving.ReceiveItem", Name), eChatType.CT_System, eChatLoc.CL_SystemWindow);
				}

				return false;
			}

			return true;
		}

		/// <summary>
		/// Called when the living is about to get money from someone
		/// else
		/// </summary>
		/// <param name="source">Source from where to get the money</param>
		/// <param name="money">array of money to get</param>
		/// <returns>true if the money was successfully received</returns>
		public override bool ReceiveMoney(GameLiving source, long money)
		{
			if (source == null || money <= 0) return false;

			Notify(GameLivingEvent.ReceiveMoney, this, new ReceiveMoneyEventArgs(source, this, money));

			if (source is GamePlayer)
				((GamePlayer)source).Out.SendMessage(LanguageMgr.GetTranslation(((GamePlayer)source).Client.Account.Language, "GameLiving.ReceiveMoney", Name), eChatType.CT_System, eChatLoc.CL_SystemWindow);

			//call base
			return base.ReceiveMoney(source, money);
		}
		#endregion
		#region Inventory
		/// <summary>
		/// Represent the inventory of all living
		/// </summary>
		protected IGameInventory m_inventory;

		/// <summary>
		/// Get/Set inventory
		/// </summary>
		public IGameInventory Inventory
		{
			get
			{
				return m_inventory;
			}
			set
			{
				m_inventory = value;
			}
		}
		#endregion
		#region Effects
		/// <summary>
		/// currently applied effects
		/// </summary>
		protected readonly GameEffectList m_effects;

		/// <summary>
		/// gets a list of active effects
		/// </summary>
		/// <returns></returns>
		public GameEffectList EffectList
		{
			get { return m_effects; }
		}

		/// <summary>
		/// Creates new effects list for this living.
		/// </summary>
		/// <returns>New effects list instance</returns>
		protected virtual GameEffectList CreateEffectsList()
		{
			return new GameEffectList(this);
		}

		#endregion
		#region Abilities

		/// <summary>
		/// Holds all abilities of the living (KeyName -> Ability)
		/// </summary>
		protected readonly Dictionary<string, Ability> m_abilities = new Dictionary<string, Ability>();

		protected readonly Object m_lockAbilities = new Object();

		/// <summary>
		/// Asks for existence of specific ability
		/// </summary>
		/// <param name="keyName">KeyName of ability</param>
		/// <returns>Does living have this ability</returns>
		public virtual bool HasAbility(string keyName)
		{
			bool hasit = false;
			
			lock (m_lockAbilities)
			{
				hasit = m_abilities.ContainsKey(keyName);
			}
			
			return hasit;
		}

		/// <summary>
		/// Add a new ability to a living
		/// </summary>
		/// <param name="ability"></param>
		public virtual void AddAbility(Ability ability)
		{
			AddAbility(ability, true);
		}

		/// <summary>
		/// Add or update an ability for this living
		/// </summary>
		/// <param name="ability"></param>
		/// <param name="sendUpdates"></param>
		public virtual void AddAbility(Ability ability, bool sendUpdates)
		{
			bool isNewAbility = false;
			lock (m_lockAbilities)
			{
				Ability oldAbility = null;
				m_abilities.TryGetValue(ability.KeyName, out oldAbility);
				
				if (oldAbility == null)
				{
					isNewAbility = true;
					m_abilities.Add(ability.KeyName, ability);
					ability.Activate(this, sendUpdates);
				}
				else
				{
					int oldLevel = oldAbility.Level;
					oldAbility.Level = ability.Level;
					
					isNewAbility |= oldAbility.Level > oldLevel;
				}
				
				if (sendUpdates && (isNewAbility && (this is GamePlayer)))
				{
					(this as GamePlayer).Out.SendMessage(LanguageMgr.GetTranslation((this as GamePlayer).Client.Account.Language, "GamePlayer.AddAbility.YouLearn", ability.Name), eChatType.CT_System, eChatLoc.CL_SystemWindow);
				}
			}
		}

		/// <summary>
		/// Remove an ability from this living
		/// </summary>
		/// <param name="abilityKeyName"></param>
		/// <returns></returns>
		public virtual bool RemoveAbility(string abilityKeyName)
		{
			Ability ability = null;
			lock (m_lockAbilities)
			{
				m_abilities.TryGetValue(abilityKeyName, out ability);
				
				if (ability == null)
					return false;
				
				ability.Deactivate(this, true);
				m_abilities.Remove(ability.KeyName);
			}
			
			if (this is GamePlayer)
				(this as GamePlayer).Out.SendMessage(LanguageMgr.GetTranslation((this as GamePlayer).Client.Account.Language, "GamePlayer.RemoveAbility.YouLose", ability.Name), eChatType.CT_System, eChatLoc.CL_SystemWindow);
			return true;
		}

		/// <summary>
		/// returns ability of living or null if non existent
		/// </summary>
		/// <param name="abilityKey"></param>
		/// <returns></returns>
		public Ability GetAbility(string abilityKey)
		{
			Ability ab = null;
			lock (m_lockAbilities)
			{
				m_abilities.TryGetValue(abilityKey, out ab);
			}
			
			return ab;
		}

		/// <summary>
		/// returns ability of living or null if no existant
		/// </summary>
		/// <returns></returns>
		public T GetAbility<T>() where T : Ability
		{
			T tmp;
			lock (m_lockAbilities)
			{
				tmp = (T)m_abilities.Values.FirstOrDefault(a => a.GetType().Equals(typeof(T)));
			}
			
			return tmp;
		}

		/// <summary>
		/// returns ability of living or null if no existant
		/// </summary>
		/// <param name="abilityType"></param>
		/// <returns></returns>
		[Obsolete("Use GetAbility<T>() instead")]
		public Ability GetAbility(Type abilityType)
		{
			lock (m_lockAbilities)
			{
				foreach (Ability ab in m_abilities.Values)
				{
					if (ab.GetType().Equals(abilityType))
						return ab;
				}
			}
			return null;
		}

		/// <summary>
		/// returns the level of ability
		/// if 0 is returned, the ability is non existent on living
		/// </summary>
		/// <param name="keyName"></param>
		/// <returns></returns>
		public int GetAbilityLevel(string keyName)
		{
			Ability ab = null;
			
			lock (m_lockAbilities)
			{
				m_abilities.TryGetValue(keyName, out ab);
			}
			
			if (ab == null)
				return 0;

			return Math.Max(1, ab.Level);
		}

		/// <summary>
		/// returns all abilities in a copied list
		/// </summary>
		/// <returns></returns>
		public IList GetAllAbilities()
		{
			List<Ability> list = new List<Ability>();
			lock (m_lockAbilities)
			{
				list = new List<Ability>(m_abilities.Values);
			}
			
			return list;
		}

        #endregion Abilities

        #region Talents

        protected ITalentSet m_talents = null;

        /// <summary>
        /// Talents of this living
        /// </summary>
        public ITalentSet Talents
        {   get { return m_talents; }
            protected set
            {
                if (m_talents != null)
                {
                    m_talents.TalentAdded -= OnTalentAdded;
                    m_talents.TalentRemoved -= OnTalentRemoved;
                }
                m_talents = value;
                if (m_talents != null)
                {
                    m_talents.TalentAdded += OnTalentAdded;
                    m_talents.TalentRemoved += OnTalentRemoved;
                }
            }
        }

        /// <summary>
        /// A talent is added to the living's talent set.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void OnTalentAdded(object sender, TalentAddedEventArgs e)
        {
            var mst = e.Talent as ModularSkillTalent;
            if (mst != null)
                mst.TryUseSkill += OnTryUseModularSkill;
        }

        /// <summary>
        /// A talent is removed from the living's talent set
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void OnTalentRemoved(object sender, TalentRemovedEventArgs e)
        {
            var mst = e.Talent as ModularSkillTalent;
            if (mst != null)
                mst.TryUseSkill -= OnTryUseModularSkill;
        }

        /// <summary>
        /// A skill owned by the living is used
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void OnTryUseModularSkill(object sender, TryUsingSkillEventArgs e)
        {
            Talents.GetAllTalents().OfType<ModularSkillTalent>().ForEach(mst => mst.HandleTryUsingOtherSkill(sender, e));
        }

        #endregion

        /// <summary>
        /// Checks if living has ability to use items of this type
        /// </summary>
        /// <param name="item"></param>
        /// <returns>true if living has ability to use item</returns>
        public virtual bool HasAbilityToUseItem(ItemTemplate item)
		{
			return GameServer.ServerRules.CheckAbilityToUseItem(this, item);
		}

		/// <summary>
		/// Table of skills currently disabled
		/// skill => disabletimeout (ticks) or 0 when endless
		/// </summary>
		private readonly Dictionary<KeyValuePair<int, Type>, KeyValuePair<long, Skill>> m_disabledSkills = new Dictionary<KeyValuePair<int, Type>, KeyValuePair<long, Skill>>();

		/// <summary>
		/// Gets the time left for disabling this skill in milliseconds
		/// </summary>
		/// <param name="skill"></param>
		/// <returns>milliseconds left for disable</returns>
		public virtual int GetSkillDisabledDuration(Skill skill)
		{
			lock ((m_disabledSkills as ICollection).SyncRoot)
			{
				KeyValuePair<int, Type> key = new KeyValuePair<int, Type>(skill.ID, skill.GetType());
				if (m_disabledSkills.ContainsKey(key))
				{
					long timeout = m_disabledSkills[key].Key;
					long left = timeout - CurrentRegion.Time;
					if (left <= 0)
					{
						left = 0;
						m_disabledSkills.Remove(key);
					}
					return (int)left;
				}
			}
			return 0;
		}

		/// <summary>
		/// Gets a copy of all disabled skills
		/// </summary>
		/// <returns></returns>
		public virtual ICollection<Skill> GetAllDisabledSkills()
		{
			lock ((m_disabledSkills as ICollection).SyncRoot)
			{
				List<Skill> skillList = new List<Skill>();
				
				foreach(KeyValuePair<long, Skill> disabled in m_disabledSkills.Values)
					skillList.Add(disabled.Value);
				
				return skillList;
			}
		}

		/// <summary>
		/// Grey out some skills on client for specified duration
		/// </summary>
		/// <param name="skill">the skill to disable</param>
		/// <param name="duration">duration of disable in milliseconds</param>
		public virtual void DisableSkill(Skill skill, int duration)
		{
			lock ((m_disabledSkills as ICollection).SyncRoot)
			{
				KeyValuePair<int, Type> key = new KeyValuePair<int, Type>(skill.ID, skill.GetType());
				if (duration > 0)
				{
					m_disabledSkills[key] = new KeyValuePair<long, Skill>(CurrentRegion.Time + duration, skill);
				}
				else
				{
					m_disabledSkills.Remove(key);
				}
			}
		}
		
		/// <summary>
		/// Grey out collection of skills on client for specified duration
		/// </summary>
		/// <param name="skill">the skill to disable</param>
		/// <param name="duration">duration of disable in milliseconds</param>
		public virtual void DisableSkill(ICollection<Tuple<Skill, int>> skills)
		{
			lock ((m_disabledSkills as ICollection).SyncRoot)
			{
				foreach (Tuple<Skill, int> tuple in skills)
				{
					Skill skill = tuple.Item1;
					int duration = tuple.Item2;
					
					KeyValuePair<int, Type> key = new KeyValuePair<int, Type>(skill.ID, skill.GetType());
					if (duration > 0)
					{
						m_disabledSkills[key] = new KeyValuePair<long, Skill>(CurrentRegion.Time + duration, skill);
					}
					else
					{
						m_disabledSkills.Remove(key);
					}
				}
			}
		}
		

		/// <summary>
		/// Removes Greyed out skills
		/// </summary>
		/// <param name="skill">the skill to remove</param>
		public virtual void RemoveDisabledSkill(Skill skill)
		{
			lock ((m_disabledSkills as ICollection).SyncRoot)
			{
				KeyValuePair<int, Type> key = new KeyValuePair<int, Type>(skill.ID, skill.GetType());
				if(m_disabledSkills.ContainsKey(key))
					m_disabledSkills.Remove(key);
			}
		}

		#region Broadcasting utils

		/// <summary>
		/// Broadcasts the living equipment to all players around
		/// </summary>
		public virtual void BroadcastLivingEquipmentUpdate()
		{
			if (ObjectState != eObjectState.Active)
				return;
			
			foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
			{
				if (player == null)
					continue;
				
				player.Out.SendLivingEquipmentUpdate(this);
			}
		}
		
		#endregion
		
		#region Region

		/// <summary>
		/// Removes the item from the world
		/// </summary>
		public override bool RemoveFromWorld()
		{
			if (!base.RemoveFromWorld()) return false;
			StopHealthRegeneration();
			StopPowerRegeneration();
			StopEnduranceRegeneration();

			if (this is GameNPC && ((GameNPC)this).SpellTimer != null) ((GameNPC)this).SpellTimer.Stop();
			if (m_healthRegenerationTimer != null) m_healthRegenerationTimer.Stop();
			if (m_powerRegenerationTimer != null) m_powerRegenerationTimer.Stop();
			if (m_enduRegenerationTimer != null) m_enduRegenerationTimer.Stop();
			m_healthRegenerationTimer = null;
			m_powerRegenerationTimer = null;
			m_enduRegenerationTimer = null;
			return true;
		}

		#endregion
		#region Spell Cast
		/// <summary>
		/// Multiplier for melee and magic.
		/// </summary>
		public virtual double Effectiveness
		{
			get { return 1.0; }
			set { }
		}

		public virtual bool IsCasting
		{
			get { return m_runningSpellHandler != null && m_runningSpellHandler.IsCasting; }
		}

		/// <summary>
		/// Returns true if the living has the spell effect, else false.
		/// </summary>
		/// <param name="spell"></param>
		/// <returns></returns>
		public override bool HasEffect(Spell spell)
		{
			lock (EffectList)
			{
				foreach (IGameEffect effect in EffectList)
				{
					if (effect is GameSpellEffect)
					{
						GameSpellEffect spellEffect = effect as GameSpellEffect;

						if (spellEffect.Spell.SpellType == spell.SpellType &&
						    spellEffect.Spell.EffectGroup == spell.EffectGroup)
							return true;
					}
				}
			}

			return base.HasEffect(spell);
		}

		/// <summary>
		/// Checks if the target has a type of effect
		/// </summary>
		/// <param name="target"></param>
		/// <param name="spell"></param>
		/// <returns></returns>
		public override bool HasEffect(Type effectType)
		{
			lock (EffectList)
			{
				foreach (IGameEffect effect in EffectList)
					if (effect.GetType() == effectType)
						return true;
			}

			return base.HasEffect(effectType);
		}

		/// <summary>
		/// Holds the currently running spell handler
		/// </summary>
		protected ISpellHandler m_runningSpellHandler;
		/// <summary>
		/// active spellhandler (casting phase) or null
		/// </summary>
		public ISpellHandler CurrentSpellHandler
		{
			// change for warlock
			get { return m_runningSpellHandler; }
			set { m_runningSpellHandler = value; }
		}

		/// <summary>
		/// Callback after spell casting is complete and next spell can be processed
		/// </summary>
		/// <param name="handler"></param>
		public virtual void OnAfterSpellCastSequence(ISpellHandler handler)
		{
			m_runningSpellHandler.CastingCompleteEvent -= new CastingCompleteCallback(OnAfterSpellCastSequence);
			m_runningSpellHandler = null;
		}

		/// <summary>
		/// Immediately stops currently casting spell
		/// </summary>
		public virtual void StopCurrentSpellcast()
		{
			if (m_runningSpellHandler != null)
				m_runningSpellHandler.InterruptCasting();
		}

		/// <summary>
		/// Cast a specific spell from given spell line
		/// </summary>
		/// <param name="spell">spell to cast</param>
		/// <param name="line">Spell line of the spell (for bonus calculations)</param>
		public virtual void CastSpell(Spell spell, SpellLine line)
		{
			if (IsStunned || IsMezzed)
			{
				Notify(GameLivingEvent.CastFailed, this, new CastFailedEventArgs(null, CastFailedEventArgs.Reasons.CrowdControlled));
				return;
			}

			if ((m_runningSpellHandler != null && spell.CastTime > 0))
			{
				Notify(GameLivingEvent.CastFailed, this, new CastFailedEventArgs(null, CastFailedEventArgs.Reasons.AlreadyCasting));
				return;
			}

			ISpellHandler spellhandler = ScriptMgr.CreateSpellHandler(this, spell, line);
			if (spellhandler != null)
			{
				m_runningSpellHandler = spellhandler;
				spellhandler.CastingCompleteEvent += new CastingCompleteCallback(OnAfterSpellCastSequence);
				spellhandler.CastSpell();
			}
			else
			{
				if (log.IsWarnEnabled)
					log.Warn(Name + " wants to cast but spell " + spell.Name + " not implemented yet");
				return;
			}
		}

		public virtual void CastSpell(ISpellCastingAbilityHandler ab)
		{
			ISpellHandler spellhandler = ScriptMgr.CreateSpellHandler(this, ab.Spell, ab.SpellLine);
			if (spellhandler != null)
			{
				// Instant cast abilities should not interfere with the spell queue
				if (spellhandler.Spell.CastTime > 0)
				{
					m_runningSpellHandler = spellhandler;
					m_runningSpellHandler.CastingCompleteEvent += new CastingCompleteCallback(OnAfterSpellCastSequence);
				}

				spellhandler.Ability = ab;
				spellhandler.CastSpell();
			}
		}

		
		/// <summary>
		/// Whether or not the living can cast a harmful spell
		/// at the moment.
		/// </summary>
		public virtual bool CanCastHarmfulSpells
		{
			get
			{
				return (!IsIncapacitated);
			}
		}

		public virtual IList<Spell> HarmfulSpells
		{
			get
			{
				return new List<Spell>();
			}
		}

		#endregion
		#region LoadCalculators
		/// <summary>
		/// Load the property calculations
		/// </summary>
		/// <returns></returns>
		public static bool LoadCalculators()
		{
			try
			{
				foreach (Assembly asm in ScriptMgr.GameServerScripts)
				{
					foreach (Type t in asm.GetTypes())
					{
						try
						{
							if (!t.IsClass || t.IsAbstract) continue;
							if (!typeof(IPropertyCalculator).IsAssignableFrom(t)) continue;
							IPropertyCalculator calc = (IPropertyCalculator)Activator.CreateInstance(t);
							foreach (PropertyCalculatorAttribute attr in t.GetCustomAttributes(typeof(PropertyCalculatorAttribute), false))
							{
								for (int i = (int)attr.Min; i <= (int)attr.Max; i++)
								{
									m_propertyCalc[i] = calc;
								}
							}
						}
						catch (Exception e)
						{
							if (log.IsErrorEnabled)
								log.Error("Error while working with type " + t.FullName, e);
						}
					}
				}
				return true;
			}
			catch (Exception e)
			{
				if (log.IsErrorEnabled)
					log.Error("GameLiving.LoadCalculators()", e);
				return false;
			}
		}
		#endregion
		#region ControlledNpc

		private byte m_petCount = 0;

		/// <summary>
		/// Gets the pet count for this living
		/// </summary>
		public byte PetCount
		{
			get { return m_petCount; }
			set { m_petCount = value; }
		}

		/// <summary>
		/// Holds the controlled object
		/// </summary>
		protected IControlledBrain[] m_controlledBrain = null;

		/// <summary>
		/// Initializes the ControlledNpcs for the GameLiving class
		/// </summary>
		/// <param name="num">Number of places to allocate.  If negative, sets to null.</param>
		public virtual void InitControlledBrainArray(int num)
		{
			if (num > 0)
			{
				m_controlledBrain = new IControlledBrain[num];
			}
			else
			{
				m_controlledBrain = null;
			}
		}

		/// <summary>
		/// Get or set the ControlledBrain.  Set always uses m_controlledBrain[0]
		/// </summary>
		public virtual IControlledBrain ControlledBrain
		{
			get
			{
				if (m_controlledBrain == null)
					return null;

				return m_controlledBrain[0];
			}
			set
			{
				m_controlledBrain[0] = value;
			}
		}

		public virtual bool IsControlledNPC(GameNPC npc)
		{
			if (npc == null)
			{
				return false;
			}
			IControlledBrain brain = npc.Brain as IControlledBrain;
			if (brain == null)
			{
				return false;
			}
			return brain.GetLivingOwner() == this;
		}

		/// <summary>
		/// Sets the controlled object for this player
		/// </summary>
		/// <param name="controlledNpc"></param>
		public virtual void SetControlledBrain(IControlledBrain controlledBrain)
		{
		}

		#endregion
		#region Group
		/// <summary>
		/// Holds the group of this living
		/// </summary>
		protected Group m_group;
		/// <summary>
		/// Holds the index of this living inside of the group
		/// </summary>
		protected byte m_groupIndex;

		/// <summary>
		/// Gets or sets the living's group
		/// </summary>
		public Group Group
		{
			get { return m_group; }
			set { m_group = value; }
		}

		/// <summary>
		/// Gets or sets the index of this living inside of the group
		/// </summary>
		public byte GroupIndex
		{
			get { return m_groupIndex; }
			set { m_groupIndex = value; }
		}
		#endregion
		
		/// <summary>
		/// Handle event notifications.
		/// </summary>
		/// <param name="e"></param>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		public override void Notify(DOLEvent e, object sender, EventArgs args)
		{
			if (e == GameLivingEvent.Interrupted && args != null)
			{
				if (CurrentSpellHandler != null)
					CurrentSpellHandler.CasterIsAttacked((args as InterruptedEventArgs).Attacker);

				return;
			}

			base.Notify(e, sender, args);
		}
		
		/// <summary>
		/// Constructor to create a new GameLiving
		/// </summary>
		public GameLiving()
			: base()
		{
			m_guildName = string.Empty;
			m_targetObjectWeakReference = new WeakRef(null);
			m_groundTarget = new Point3D(0, 0, 0);

			//Set all combat properties
			m_activeWeaponSlot = eActiveWeaponSlot.Standard;
			m_effects = CreateEffectsList();
			m_concEffects = new ConcentrationList(this);
			m_attackers = new List<GameObject>();

			m_health = 1;
			m_mana = 1;
			m_endurance = 1;
			m_maxEndurance = 1;
            Talents = new TalentSet(this);
        }
	}
}
