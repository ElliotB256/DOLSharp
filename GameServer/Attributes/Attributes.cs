using System;
using System.Reflection;

namespace DOL.GS.PropertyCalc
{
    /// <summary>
    /// Calculates and tracks properties of a GameLiving
    /// </summary>
    public class LivingProperties
    {
        //Todo: Event handling, automatically sending update packets (For PlayerProperties : LivingProperties)

        public LivingProperties(GameLiving owner)
        {
            m_owner = owner;

            m_TalentBonus = new PropertyIndexer();
            m_EquipmentBonus = new PropertyIndexer();
            m_EffectBonus = new PropertyIndexer();
        }

        /// <summary>
        /// The owner of these properties
        /// </summary>
        protected GameLiving m_owner;

        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        //These need to be private - don't want anything outside of attribute modfying them!

        /// <summary>
        /// Bonuses to properties based on talents of the living.
        /// </summary>
        private IPropertyIndexer m_TalentBonus;

        /// <summary>
        /// Bonuses to properties based on equipment used by the living.
        /// </summary>
        private IPropertyIndexer m_EquipmentBonus;

        /// <summary>
        /// Bonuses to properties based on applied skill effects.
        /// </summary>
        private IPropertyIndexer m_EffectBonus;

        public int GetTalentBonus(eProperty prop)
        {
            return m_TalentBonus[prop];
        }

        public int GetEquipmentBonus(eProperty prop)
        {
            return m_EquipmentBonus[prop];
        }

        public int GetEffectBonus(eProperty prop)
        {
            return m_EffectBonus[prop];
        }

        /// <summary>
        /// Lookup table for the calculator used for each property.
        /// </summary>
        internal static readonly IPropertyCalculator[] m_propertyCalc = new IPropertyCalculator[(int)eProperty.MaxProperty + 1];

        public virtual int GetProperty(eProperty property, eCalculationType type = eCalculationType.All)
        {
            if (m_propertyCalc != null && m_propertyCalc[(int)property] != null)
            {
                return m_propertyCalc[(int)property].CalculateValue(m_owner, property, type);
            }
            else
            {
                log.ErrorFormat("{0} did not find property calculator for property ID {1}.", m_owner.Name, (int)property);
            }
            return 0;
        }

        #region Static methods

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
                    log.Error("Error while loading Attribute calculators", e);
                return false;
            }
        }

        #endregion

    }
}
