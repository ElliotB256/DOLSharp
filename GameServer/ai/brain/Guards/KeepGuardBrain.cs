using System;
using log4net;
using System.Reflection;
using DOL.GS;
using DOL.GS.Keeps;
using DOL.GS.Movement;

namespace DOL.AI.Brain
{
	/// <summary>
	/// Brain Class for Area Capture Guards
	/// </summary>
	public class KeepGuardBrain : StandardMobBrain
	{
		/// <summary>
		/// Defines a logger for this class.
		/// </summary>
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		public GameKeepGuard guard;
		/// <summary>
		/// Constructor for the Brain setting default values
		/// </summary>
		public KeepGuardBrain()
			: base()
		{
			AggroLevel = 90;
			AggroRange = 1500;
		}

		public void SetAggression(int aggroLevel, int aggroRange)
		{
			AggroLevel = aggroLevel;
			AggroRange = aggroRange;
		}

		public override int ThinkInterval
		{
			get
			{
				return 1500;
			}
		}
		
		public override bool AggroLOS
		{
			get { return true; }
		}
	}
}
