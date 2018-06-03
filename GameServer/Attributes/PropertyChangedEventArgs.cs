using System;

namespace DOL.GS.PropertyCalc
{
    /// <summary>
    /// An event in which a property/statistic changes.
    /// </summary>
    public class PropertyChangedEventArgs : EventArgs
    {
        public PropertyChangedEventArgs(eProperty property, int oldValue, int newValue)
        {
            Property = property;
            OldValue = oldValue;
            NewValue = newValue;
        }

        /// <summary>
        /// The property that has changed
        /// </summary>
        public eProperty Property { get; private set; }

        /// <summary>
        /// Old value of the property
        /// </summary>
        public int OldValue { get; private set; }

        /// <summary>
        /// New value of the property
        /// </summary>
        public int NewValue { get; private set; }
    }
}