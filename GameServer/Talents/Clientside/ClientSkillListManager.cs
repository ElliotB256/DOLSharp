using System;
using System.Collections.Generic;

namespace DOL.Talents.Clientside
{
    /// <summary>
    /// Class that manages presentation of spells to the client.
    /// Builds lists of client spells from ITalentSet's ITalents.
    /// </summary>
    public class ClientSkillListManager
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private List<ClientSkillList> m_skillLists;
        private object m_skillListLock = new object();

        /// <summary>
        /// Set of talents used for constructing spell lists
        /// </summary>
        private ITalentSet m_talentSet;

        public ClientSkillListManager(ITalentSet set)
        {
            m_talentSet = set;
            m_skillLists = new List<ClientSkillList>();
        }

        public const String UNCATEGORISED_SPELLLINE_NAME = "Abilities";

        private SkillGroupTalent UncategorisedSpellLine = new SkillGroupTalent(UNCATEGORISED_SPELLLINE_NAME);

        /// <summary>
        /// Constructs spell lists from ITalentSet
        /// </summary>
        public void Build()
        {
            log.Debug(String.Format("Building ClientSkillLists for owner={0}", m_talentSet.Owner));
            IEnumerable<ITalent> talents = m_talentSet.GetAllTalents();

            lock (m_skillListLock)
            {
                m_skillLists.Clear();
                Dictionary<SkillGroupTalent, ClientSkillList> spelllines = new Dictionary<SkillGroupTalent, ClientSkillList>();
                spelllines.Add(UncategorisedSpellLine, new ClientSkillList(UncategorisedSpellLine));

                // Create spell lines
                foreach (ITalent talent in talents)
                {
                    if (talent is SkillGroupTalent)
                        spelllines.Add(talent as SkillGroupTalent, new ClientSkillList((SkillGroupTalent)talent));
                }

                // Enumerate through spells, add them to lines
                foreach (ITalent talent in talents)
                {
                    if (talent.ClientImplementation is ClientSpellImplementation)
                    {
                        ClientSpellImplementation sp = (ClientSpellImplementation)talent.ClientImplementation;

                        //Find and add spell to line
                        if (sp.SpellLine != null && spelllines.ContainsKey(sp.SpellLine))
                            spelllines[sp.SpellLine].Talents.Add(talent);
                        else
                            spelllines[UncategorisedSpellLine].Talents.Add(talent);
                    }
                }

                //Build client spell lists
                foreach (ClientSkillList list in spelllines.Values)
                    m_skillLists.Add(list);
            }
        }

        /// <summary>
        /// Returns a list of ClientSkillLists that group together usable abilities.
        /// </summary>
        /// <returns></returns>
        public List<ClientSkillList> GetSkillLists()
        {
            List<ClientSkillList> list = new List<ClientSkillList>();
            lock (m_skillListLock)
            { m_skillLists.ForEach(x => list.Add(x)); }
            return list;
        }

        /// <summary>
        /// Selects an ITalent by index. Used for selecting skills from client.
        /// </summary>
        public ITalent SelectByIndex(int lineIndex, int levelIndex)
        {
            List<ClientSkillList> skilllists = GetSkillLists();
            if (lineIndex < 0 || lineIndex >= skilllists.Count)
            {
                log.Warn(String.Format("Unable to select by lineIndex={0}, skilllists Count={1}.", lineIndex, skilllists.Count));
                return null;
            }

            ClientSkillList skilllist = skilllists[lineIndex];
            if (levelIndex < 0 || levelIndex >= skilllist.Talents.Count)
            {
                log.Warn(String.Format("Unable to select by levelIndex={0}, skilllist Count={1}.", levelIndex, skilllist.Talents.Count));
                return null;
            }
            return skilllist.Talents[levelIndex];
        }
    }
}
