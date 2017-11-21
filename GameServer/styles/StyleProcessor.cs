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

using DOL.Language;
using DOL.AI.Brain;
using DOL.Database;
using DOL.GS.Keeps;
using DOL.GS.PacketHandler;
using DOL.GS.Spells;
using DOL.GS.Effects;

using log4net;

namespace DOL.GS.Styles
{
	/// <summary>
	/// Processes styles and style related stuff.
	/// </summary>
	public class StyleProcessor
	{
		/// <summary>
		/// Defines a logger for this class.
		/// </summary>
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		/// <summary>
		/// Returns wether this player can use a particular style
		/// right now. Tests for all preconditions like prerequired
		/// styles, previous attack result, ...
		/// </summary>
		/// <param name="living">The living wanting to execute a style</param>
		/// <param name="style">The style to execute</param>
		/// <param name="weapon">The weapon used to execute the style</param>
		/// <returns>true if the player can execute the style right now, false if not</returns>
		public static bool CanUseStyle(GameLiving living, Style style, InventoryItem weapon)
		{
			//First thing in processors, lock the objects you modify
			//This way it makes sure the objects are not modified by
			//several different threads at the same time!
			lock (living)
			{
				GameLiving target = living.TargetObject as GameLiving;
				if (target == null) return false;

				//Required attack result
				GameLiving.eAttackResult requiredAttackResult = GameLiving.eAttackResult.Any;
				switch (style.AttackResultRequirement)
				{
					case Style.eAttackResultRequirement.Any: requiredAttackResult = GameLiving.eAttackResult.Any; break;
					case Style.eAttackResultRequirement.Block: requiredAttackResult = GameLiving.eAttackResult.Blocked; break;
					case Style.eAttackResultRequirement.Evade: requiredAttackResult = GameLiving.eAttackResult.Evaded; break;
					case Style.eAttackResultRequirement.Fumble: requiredAttackResult = GameLiving.eAttackResult.Fumbled; break;
					case Style.eAttackResultRequirement.Hit: requiredAttackResult = GameLiving.eAttackResult.HitUnstyled; break;
					case Style.eAttackResultRequirement.Style: requiredAttackResult = GameLiving.eAttackResult.HitStyle; break;
					case Style.eAttackResultRequirement.Miss: requiredAttackResult = GameLiving.eAttackResult.Missed; break;
					case Style.eAttackResultRequirement.Parry: requiredAttackResult = GameLiving.eAttackResult.Parried; break;
				}

				AttackData lastAD = (AttackData)living.TempProperties.getProperty<object>(GameLiving.LAST_ATTACK_DATA, null);

				switch (style.OpeningRequirementType)
				{

					case Style.eOpening.Offensive:
						//Style required before this one?
						if (style.OpeningRequirementValue != 0
							&& (lastAD == null
							|| lastAD.AttackResult != GameLiving.eAttackResult.HitStyle
							|| lastAD.Style == null
							|| lastAD.Style.ID != style.OpeningRequirementValue
							|| lastAD.Target != target)) // style chains are possible only on the same target
						{
							//DOLConsole.WriteLine("Offensive: Opening Requirement style needed failed!("+style.OpeningRequirementValue+")");
							return false;
						}

						//Last attack result
						GameLiving.eAttackResult lastRes = (lastAD != null) ? lastAD.AttackResult : GameLiving.eAttackResult.Any;

						if (requiredAttackResult != GameLiving.eAttackResult.Any && lastRes != requiredAttackResult)
						{
							//DOLConsole.WriteLine("Offensive: AttackResult Requirement failed!("+requiredAttackResult.ToString()+", was "+lastRes+")");
							return false;
						}
						break;

					case Style.eOpening.Defensive:
						AttackData targetsLastAD = (AttackData)target.TempProperties.getProperty<object>(GameLiving.LAST_ATTACK_DATA, null);

						//Last attack result
						if (requiredAttackResult != GameLiving.eAttackResult.Any)
						{
							if (targetsLastAD == null || targetsLastAD.Target != living)
							{
								return false;
							}

							if (requiredAttackResult != GameLiving.eAttackResult.HitStyle && targetsLastAD.AttackResult != requiredAttackResult)
							{
								//DOLConsole.WriteLine("Defensive: AttackResult Requirement failed!("+requiredAttackResult.ToString()+", was "+lastEnemyRes+")");
								return false;
							}
							else if (requiredAttackResult == GameLiving.eAttackResult.HitStyle && targetsLastAD.Style == null)
							{
								//DOLConsole.WriteLine("Defensive: AttackResult Requirement failed!("+requiredAttackResult.ToString()+", was "+lastEnemyRes+")");
								return false;
							}
						}
						break;

					case Style.eOpening.Positional:
						//check here if target is in front of attacker
						if (!living.IsObjectInFront(target, 120))
							return false;

						//you can't use positional styles on keep doors or walls
						if ((target is GameKeepComponent || target is GameKeepDoor) && (Style.eOpeningPosition)style.OpeningRequirementValue != Style.eOpeningPosition.Front)
							return false;

						// get players angle on target
                        float angle = target.GetAngle( living );
						//player.Out.SendDebugMessage("Positional check: "+style.OpeningRequirementValue+" angle "+angle+" target heading="+target.Heading);						

						switch ((Style.eOpeningPosition)style.OpeningRequirementValue)
						{
							//Back Styles
							//60 degree since 1.62 patch
							case Style.eOpeningPosition.Back:
								if (!(angle >= 150 && angle < 210)) return false;
								break;
							// Side Styles  
							//105 degree since 1.62 patch
							case Style.eOpeningPosition.Side:
								if (!(angle >= 45 && angle < 150) && !(angle >= 210 && angle < 315)) return false;
								break;
							// Front Styles
							// 90 degree
							case Style.eOpeningPosition.Front:
								if (!(angle >= 315 || angle < 45)) return false;
								break;
						}
						//DOLConsole.WriteLine("Positional check success: "+style.OpeningRequirementValue);
						break;
				}

				if (style.StealthRequirement && !living.IsStealthed)
					return false;

				if (!CheckWeaponType(style, living, weapon))
					return false;

				//				if(player.Endurance < CalculateEnduranceCost(style, weapon.SPD_ABS))
				//					return false;

				return true;
			}
		}

		/// <summary>
		/// Tries to queue a new style in the player's style queue.
		/// Takes care of all conditions like setting backup styles and
		/// canceling styles if the style was queued already.
		/// </summary>
		/// <param name="living">The living to execute the style</param>
		/// <param name="style">The style to execute</param>
		public static void TryToUseStyle(GameLiving living, Style style)
		{
            log.Warn("Not Implemented.");
		}

		/// <summary>
		/// Executes the style of the given player. Prints
		/// out messages to the player notifying him of his success or failure.
		/// </summary>
		/// <param name="living">The living executing the styles</param>
		/// <param name="attackData">
		/// The AttackData that will be modified to contain the 
		/// new damage and the executed style.
		/// </param>
		/// <param name="weapon">The weapon used to execute the style</param>
		/// <returns>true if a style was performed, false if not</returns>
		public static bool ExecuteStyle(GameLiving living, AttackData attackData, InventoryItem weapon)
		{
            log.Warn("Not Implemented");
            return false;
		}

		/// <summary>
		/// Calculates endurance needed to use style
		/// </summary>
		/// <param name="living">The living doing the style</param>
		/// <param name="style">The style to be used</param>
		/// <param name="weaponSpd">The weapon speed</param>
		/// <returns>Endurance needed to use style</returns>
		public static int CalculateEnduranceCost(GameLiving living, Style style, int weaponSpd)
		{           
            int fatCost = weaponSpd * style.EnduranceCost / 40;
			if (weaponSpd < 40)
				fatCost++;
			
            fatCost = (int)Math.Ceiling(fatCost * living.GetModified(eProperty.FatigueConsumption) * 0.01);
			return Math.Max(1, fatCost);
		}

		/// <summary>
		/// Returns whether player has correct weapon
		/// active for particular style
		/// </summary>
		/// <param name="style">The style to execute</param>
		/// <param name="living">The living wanting to execute the style</param>
		/// <param name="weapon">The weapon used to execute the style</param>
		/// <returns>true if correct weapon active</returns>
		protected static bool CheckWeaponType(Style style, GameLiving living, InventoryItem weapon)
		{
			if (living is GameNPC)
				return true;
			GamePlayer player = living as GamePlayer;
			if (player == null) return false;

			switch (style.WeaponTypeRequirement)
			{
				case Style.SpecialWeaponType.DualWield:
					// both weapons are needed to use style,
					// shield is not a weapon here
					InventoryItem rightHand = player.AttackWeapon;
					InventoryItem leftHand = player.Inventory.GetItem(eInventorySlot.LeftHandWeapon);

					if (rightHand == null || leftHand == null || (rightHand.Item_Type != Slot.RIGHTHAND && rightHand.Item_Type != Slot.LEFTHAND))
						return false;

					if (style.Spec == Specs.HandToHand && (rightHand.Object_Type != (int)eObjectType.HandToHand || leftHand.Object_Type != (int)eObjectType.HandToHand))
						return false;
					else if (style.Spec == Specs.Fist_Wraps && (rightHand.Object_Type != (int)eObjectType.FistWraps || leftHand.Object_Type != (int)eObjectType.FistWraps))
						return false;

					return leftHand.Object_Type != (int)eObjectType.Shield;

				case Style.SpecialWeaponType.AnyWeapon:
					// TODO: style can be used with any weapon type,
					// shield is not a weapon here
					return weapon != null;

				default:
					// WeaponTypeRequirement holds eObjectType of weapon needed for style
					// no weapon = can't use style
					if (weapon == null)
						return false;

					// can't use shield styles if no active weapon
					if (style.WeaponTypeRequirement == (int)eObjectType.Shield
						&& (player.AttackWeapon == null || (player.AttackWeapon.Item_Type != Slot.RIGHTHAND && player.AttackWeapon.Item_Type != Slot.LEFTHAND)))
						return false;

					// weapon type check
					return GameServer.ServerRules.IsObjectTypesEqual(
							(eObjectType)style.WeaponTypeRequirement,
							(eObjectType)weapon.Object_Type);
			}
		}

		/// <summary>
		/// Add the magical effect to target
		/// </summary>
		/// <param name="caster">The player who execute the style</param>
		/// <param name="target">The target of the style</param>
		/// <param name="spellID">The spellid of the magical effect</param>
		protected static ISpellHandler CreateMagicEffect(GameLiving caster, GameLiving target, int spellID)
		{
			SpellLine styleLine = SkillBase.GetSpellLine(GlobalSpellsLines.Combat_Styles_Effect);
			if (styleLine == null) return null;

			List<Spell> spells = SkillBase.GetSpellList(styleLine.KeyName);

			Spell styleSpell = null;
			foreach (Spell spell in spells)
			{
				if (spell.ID == spellID)
				{
					styleSpell = spell;
					break;
				}
			}

			ISpellHandler spellHandler = ScriptMgr.CreateSpellHandler(caster, styleSpell, styleLine);
			if (spellHandler == null && styleSpell != null && caster is GamePlayer)
			{
				((GamePlayer)caster).Out.SendMessage(styleSpell.Name + " not implemented yet (" + styleSpell.SpellType + ")", eChatType.CT_System, eChatLoc.CL_SystemWindow);
			}

			// No negative effects can be applied on a keep door or via attacking a keep door
			if ((target is GameKeepComponent || target is GameKeepDoor) && spellHandler.HasPositiveEffect == false)
			{
				return null;
			}


			return spellHandler;
		}


		/// <summary>
		/// Delve a Style handled by this processor
		/// </summary>
		/// <param name="delveInfo"></param>
		/// <param name="style"></param>
		/// <param name="player"></param>
		public static void DelveWeaponStyle(IList<string> delveInfo, Style style, GamePlayer player)
		{
			delveInfo.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "DetailDisplayHandler.HandlePacket.WeaponType", style.GetRequiredWeaponName()));
			string temp = LanguageMgr.GetTranslation(player.Client.Account.Language, "DetailDisplayHandler.HandlePacket.Opening") + " ";
			if (Style.eOpening.Offensive == style.OpeningRequirementType)
			{
				//attacker action result is opening
				switch (style.AttackResultRequirement)
				{
					case Style.eAttackResultRequirement.Hit:
						temp += LanguageMgr.GetTranslation(player.Client.Account.Language, "DetailDisplayHandler.HandlePacket.YouHit");
						break;
					case Style.eAttackResultRequirement.Miss:
						temp += LanguageMgr.GetTranslation(player.Client.Account.Language, "DetailDisplayHandler.HandlePacket.YouMiss");
						break;
					case Style.eAttackResultRequirement.Parry:
						temp += LanguageMgr.GetTranslation(player.Client.Account.Language, "DetailDisplayHandler.HandlePacket.TargetParrys");
						break;
					case Style.eAttackResultRequirement.Block:
						temp += LanguageMgr.GetTranslation(player.Client.Account.Language, "DetailDisplayHandler.HandlePacket.TargetBlocks");
						break;
					case Style.eAttackResultRequirement.Evade:
						temp += LanguageMgr.GetTranslation(player.Client.Account.Language, "DetailDisplayHandler.HandlePacket.TargetEvades");
						break;
					case Style.eAttackResultRequirement.Fumble:
						temp += LanguageMgr.GetTranslation(player.Client.Account.Language, "DetailDisplayHandler.HandlePacket.YouFumble");
						break;
					case Style.eAttackResultRequirement.Style:
						Style reqStyle = SkillBase.GetStyleByID(style.OpeningRequirementValue, player.CharacterClass.ID);
						if (reqStyle == null)
						{
							reqStyle = SkillBase.GetStyleByID(style.OpeningRequirementValue, 0);
						}
						temp = LanguageMgr.GetTranslation(player.Client.Account.Language, "DetailDisplayHandler.HandlePacket.OpeningStyle") + " ";
						if (reqStyle == null)
						{
							temp += "(style not found " + style.OpeningRequirementValue + ")";
						}
						else
						{
							temp += reqStyle.Name;
						}
						break;
					default:
						temp += LanguageMgr.GetTranslation(player.Client.Account.Language, "DetailDisplayHandler.HandlePacket.Any");
						break;
				}
			}
			else if (Style.eOpening.Defensive == style.OpeningRequirementType)
			{
				//defender action result is opening
				switch (style.AttackResultRequirement)
				{
					case Style.eAttackResultRequirement.Miss:
						temp += LanguageMgr.GetTranslation(player.Client.Account.Language, "DetailDisplayHandler.HandlePacket.TargetMisses");
						break;
					case Style.eAttackResultRequirement.Hit:
						temp += LanguageMgr.GetTranslation(player.Client.Account.Language, "DetailDisplayHandler.HandlePacket.TargetHits");
						break;
					case Style.eAttackResultRequirement.Parry:
						temp += LanguageMgr.GetTranslation(player.Client.Account.Language, "DetailDisplayHandler.HandlePacket.YouParry");
						break;
					case Style.eAttackResultRequirement.Block:
						temp += LanguageMgr.GetTranslation(player.Client.Account.Language, "DetailDisplayHandler.HandlePacket.YouBlock");
						break;
					case Style.eAttackResultRequirement.Evade:
						temp += LanguageMgr.GetTranslation(player.Client.Account.Language, "DetailDisplayHandler.HandlePacket.YouEvade");
						break;
					case Style.eAttackResultRequirement.Fumble:
						temp += LanguageMgr.GetTranslation(player.Client.Account.Language, "DetailDisplayHandler.HandlePacket.TargetFumbles");
						break;
					case Style.eAttackResultRequirement.Style:
						temp += LanguageMgr.GetTranslation(player.Client.Account.Language, "DetailDisplayHandler.HandlePacket.TargetStyle");
						break;
					default:
						temp += LanguageMgr.GetTranslation(player.Client.Account.Language, "DetailDisplayHandler.HandlePacket.Any");
						break;
				}
			}
			else if (Style.eOpening.Positional == style.OpeningRequirementType)
			{
				//attacker position to target is opening
				temp += LanguageMgr.GetTranslation(player.Client.Account.Language, "DetailDisplayHandler.HandlePacket.Positional");
				switch (style.OpeningRequirementValue)
				{
					case (int)Style.eOpeningPosition.Front:
						temp += LanguageMgr.GetTranslation(player.Client.Account.Language, "DetailDisplayHandler.HandlePacket.Front");
						break;
					case (int)Style.eOpeningPosition.Back:
						temp += LanguageMgr.GetTranslation(player.Client.Account.Language, "DetailDisplayHandler.HandlePacket.Back");
						break;
					case (int)Style.eOpeningPosition.Side:
						temp += LanguageMgr.GetTranslation(player.Client.Account.Language, "DetailDisplayHandler.HandlePacket.Side");
						break;

				}
			}

			delveInfo.Add(temp);

			if (style.OpeningRequirementValue != 0 && style.AttackResultRequirement == 0 && style.OpeningRequirementType == 0)
			{
				delveInfo.Add(string.Format("- Error: Opening Requirement '{0}' but requirement type is Any!", style.OpeningRequirementValue));
			}

			temp = "";

			foreach (Style st in SkillBase.GetStyleList(style.Spec, player.CharacterClass.ID))
			{
				if (st.AttackResultRequirement == Style.eAttackResultRequirement.Style && st.OpeningRequirementValue == style.ID)
				{
					temp = (temp == "" ? st.Name : temp + LanguageMgr.GetTranslation(player.Client.Account.Language, "DetailDisplayHandler.HandlePacket.Or", st.Name));
				}
			}

			if (temp != "")
			{
				delveInfo.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "DetailDisplayHandler.HandlePacket.FollowupStyle", temp));
			}

			temp = LanguageMgr.GetTranslation(player.Client.Account.Language, "DetailDisplayHandler.HandlePacket.FatigueCost") + " ";

			if (style.EnduranceCost < 5)
				temp += LanguageMgr.GetTranslation(player.Client.Account.Language, "DetailDisplayHandler.HandlePacket.VeryLow");
			else if (style.EnduranceCost < 10)
				temp += LanguageMgr.GetTranslation(player.Client.Account.Language, "DetailDisplayHandler.HandlePacket.Low");
			else if (style.EnduranceCost < 15)
				temp += LanguageMgr.GetTranslation(player.Client.Account.Language, "DetailDisplayHandler.HandlePacket.Medium");
			else if (style.EnduranceCost < 20)
				temp += LanguageMgr.GetTranslation(player.Client.Account.Language, "DetailDisplayHandler.HandlePacket.High");
			else
				temp += LanguageMgr.GetTranslation(player.Client.Account.Language, "DetailDisplayHandler.HandlePacket.VeryHigh");

			delveInfo.Add(temp);

			temp = LanguageMgr.GetTranslation(player.Client.Account.Language, "DetailDisplayHandler.HandlePacket.Damage") + " ";

			double tempGrowth = (style.GrowthRate * 50 + style.GrowthOffset) / 0.295; //0.295 is the rounded down style quantum that is used on Live

			if (style.GrowthRate == 0 && style.GrowthOffset == 0)
				temp += LanguageMgr.GetTranslation(player.Client.Account.Language, "DetailDisplayHandler.HandlePacket.None");
			else if (tempGrowth < 49)
				temp += LanguageMgr.GetTranslation(player.Client.Account.Language, "DetailDisplayHandler.HandlePacket.VeryLow");
			else if (tempGrowth < 99)
				temp += LanguageMgr.GetTranslation(player.Client.Account.Language, "DetailDisplayHandler.HandlePacket.Low");
			else if (tempGrowth < 149)
				temp += LanguageMgr.GetTranslation(player.Client.Account.Language, "DetailDisplayHandler.HandlePacket.Medium");
			else if (tempGrowth < 199)
				temp += LanguageMgr.GetTranslation(player.Client.Account.Language, "DetailDisplayHandler.HandlePacket.High");
			else if (tempGrowth < 249)
				temp += LanguageMgr.GetTranslation(player.Client.Account.Language, "DetailDisplayHandler.HandlePacket.VeryHigh");
			else
				temp += LanguageMgr.GetTranslation(player.Client.Account.Language, "DetailDisplayHandler.HandlePacket.Devastating");

			delveInfo.Add(temp);

			temp = LanguageMgr.GetTranslation(player.Client.Account.Language, "DetailDisplayHandler.HandlePacket.ToHit") + " ";

			if (style.BonusToHit <= -20)
				temp += LanguageMgr.GetTranslation(player.Client.Account.Language, "DetailDisplayHandler.HandlePacket.VeryHighPenalty");
			else if (style.BonusToHit <= -15)
				temp += LanguageMgr.GetTranslation(player.Client.Account.Language, "DetailDisplayHandler.HandlePacket.HighPenalty");
			else if (style.BonusToHit <= -10)
				temp += LanguageMgr.GetTranslation(player.Client.Account.Language, "DetailDisplayHandler.HandlePacket.MediumPenalty");
			else if (style.BonusToHit <= -5)
				temp += LanguageMgr.GetTranslation(player.Client.Account.Language, "DetailDisplayHandler.HandlePacket.LowPenalty");
			else if (style.BonusToHit < 0)
				temp += LanguageMgr.GetTranslation(player.Client.Account.Language, "DetailDisplayHandler.HandlePacket.VeryLowPenalty");
			else if (style.BonusToHit == 0)
				temp += LanguageMgr.GetTranslation(player.Client.Account.Language, "DetailDisplayHandler.HandlePacket.NoBonus");
			else if (style.BonusToHit < 5)
				temp += LanguageMgr.GetTranslation(player.Client.Account.Language, "DetailDisplayHandler.HandlePacket.VeryLowBonus");
			else if (style.BonusToHit < 10)
				temp += LanguageMgr.GetTranslation(player.Client.Account.Language, "DetailDisplayHandler.HandlePacket.LowBonus");
			else if (style.BonusToHit < 15)
				temp += LanguageMgr.GetTranslation(player.Client.Account.Language, "DetailDisplayHandler.HandlePacket.MediumBonus");
			else if (style.BonusToHit < 20)
				temp += LanguageMgr.GetTranslation(player.Client.Account.Language, "DetailDisplayHandler.HandlePacket.HighBonus");
			else
				temp += LanguageMgr.GetTranslation(player.Client.Account.Language, "DetailDisplayHandler.HandlePacket.VeryHighBonus");

			delveInfo.Add(temp);

			temp = LanguageMgr.GetTranslation(player.Client.Account.Language, "DetailDisplayHandler.HandlePacket.Defense") + " ";

			if (style.BonusToDefense <= -20)
				temp += LanguageMgr.GetTranslation(player.Client.Account.Language, "DetailDisplayHandler.HandlePacket.VeryHighPenalty");
			else if (style.BonusToDefense <= -15)
				temp += LanguageMgr.GetTranslation(player.Client.Account.Language, "DetailDisplayHandler.HandlePacket.HighPenalty");
			else if (style.BonusToDefense <= -10)
				temp += LanguageMgr.GetTranslation(player.Client.Account.Language, "DetailDisplayHandler.HandlePacket.MediumPenalty");
			else if (style.BonusToDefense <= -5)
				temp += LanguageMgr.GetTranslation(player.Client.Account.Language, "DetailDisplayHandler.HandlePacket.LowPenalty");
			else if (style.BonusToDefense < 0)
				temp += LanguageMgr.GetTranslation(player.Client.Account.Language, "DetailDisplayHandler.HandlePacket.VeryLowPenalty");
			else if (style.BonusToDefense == 0)
				temp += LanguageMgr.GetTranslation(player.Client.Account.Language, "DetailDisplayHandler.HandlePacket.NoBonus");
			else if (style.BonusToDefense < 5)
				temp += LanguageMgr.GetTranslation(player.Client.Account.Language, "DetailDisplayHandler.HandlePacket.VeryLowBonus");
			else if (style.BonusToDefense < 10)
				temp += LanguageMgr.GetTranslation(player.Client.Account.Language, "DetailDisplayHandler.HandlePacket.LowBonus");
			else if (style.BonusToDefense < 15)
				temp += LanguageMgr.GetTranslation(player.Client.Account.Language, "DetailDisplayHandler.HandlePacket.MediumBonus");
			else if (style.BonusToDefense < 20)
				temp += LanguageMgr.GetTranslation(player.Client.Account.Language, "DetailDisplayHandler.HandlePacket.HighBonus");
			else
				temp += LanguageMgr.GetTranslation(player.Client.Account.Language, "DetailDisplayHandler.HandlePacket.VeryHighBonus");

			delveInfo.Add(temp);

			if (style.Procs.Count > 0)
			{
				temp = LanguageMgr.GetTranslation(player.Client.Account.Language, "DetailDisplayHandler.HandlePacket.TargetEffect") + " ";

				SpellLine styleLine = SkillBase.GetSpellLine(GlobalSpellsLines.Combat_Styles_Effect);
				if (styleLine != null)
				{
					/*check if there is a class specific style proc*/
					bool hasClassSpecificProc = false;
					foreach (Tuple<Spell, int, int> proc in style.Procs)
					{
						if (proc.Item2 == player.CharacterClass.ID)
						{
							hasClassSpecificProc = true;
							break;
						}
					}

					foreach (Tuple<Spell, int, int> proc in style.Procs)
					{
						// RR4: we added all the procs to the style, now it's time to check for class ID
						if (hasClassSpecificProc && proc.Item2 != player.CharacterClass.ID) continue;
						else if (!hasClassSpecificProc && proc.Item2 != 0) continue;

						Spell spell = proc.Item1;
						if (spell != null)
						{
							ISpellHandler spellHandler = ScriptMgr.CreateSpellHandler(player.Client.Player, spell, styleLine);
							if (spellHandler == null)
							{
								temp += spell.Name + " (Not implemented yet)";
								delveInfo.Add(temp);
							}
							else
							{
								temp += spell.Name;
								delveInfo.Add(temp);
								delveInfo.Add(" ");//empty line
								delveInfo.AddRange(spellHandler.DelveInfo);
							}
						}
					}
				}
			}

			if (player.Client.Account.PrivLevel > 1)
			{
				delveInfo.Add(" ");
				delveInfo.Add("--- Style Technical Information ---");
				delveInfo.Add(" ");
				delveInfo.Add(string.Format("ID: {0}", style.ID));
				delveInfo.Add(string.Format("ClassID: {0}", style.ClassID));
				delveInfo.Add(string.Format("Icon: {0}", style.Icon));
				delveInfo.Add(string.Format("TwoHandAnimation: {0}", style.TwoHandAnimation));
				delveInfo.Add(string.Format("Spec: {0}", style.Spec));
				delveInfo.Add(string.Format("SpecLevelRequirement: {0}", style.SpecLevelRequirement));
				delveInfo.Add(string.Format("Level: {0}", style.Level));
				delveInfo.Add(string.Format("GrowthOffset: {0}", style.GrowthOffset));
				delveInfo.Add(string.Format("GrowthRate: {0}", style.GrowthRate));
				delveInfo.Add(string.Format("Endurance: {0}", style.EnduranceCost));
				delveInfo.Add(string.Format("StealthRequirement: {0}", style.StealthRequirement));
				delveInfo.Add(string.Format("WeaponTypeRequirement: {0}", style.WeaponTypeRequirement));
				string indicator = "";
				if (style.OpeningRequirementValue != 0 && style.AttackResultRequirement == 0 && style.OpeningRequirementType == 0)
				{
					indicator = "!!";
				}
				delveInfo.Add(string.Format("AttackResultRequirement: {0}({1}) {2}", style.AttackResultRequirement, (int)style.AttackResultRequirement, indicator));
				delveInfo.Add(string.Format("OpeningRequirementType: {0}({1}) {2}", style.OpeningRequirementType, (int)style.OpeningRequirementType, indicator));
				delveInfo.Add(string.Format("OpeningRequirementValue: {0}", style.OpeningRequirementValue));
				delveInfo.Add(string.Format("ArmorHitLocation: {0}({1})", style.ArmorHitLocation, (int)style.ArmorHitLocation));
				delveInfo.Add(string.Format("BonusToDefense: {0}", style.BonusToDefense));
				delveInfo.Add(string.Format("BonusToHit: {0}", style.BonusToHit));

				if (style.Procs != null && style.Procs.Count > 0)
				{
					delveInfo.Add(" ");

					string procs = "";
					foreach (Tuple<Spell, int, int> spell in style.Procs)
					{
						if (procs != "")
							procs += ", ";

						procs += spell.Item1.ID;
					}

					delveInfo.Add(string.Format("Procs: {0}", procs));
					delveInfo.Add(string.Format("RandomProc: {0}", style.RandomProc));
				}
			}

		}
	}
}
