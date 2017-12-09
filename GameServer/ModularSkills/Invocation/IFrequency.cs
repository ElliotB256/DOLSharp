using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOL.GS.ModularSkills
{
    public interface IFrequency
    {
        /// <summary>
        /// Frequency to tick at. This is a tick rate measured in Hz.
        /// </summary>
        float Frequency { get; }
    }
}
