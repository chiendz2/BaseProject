using System.Collections.Generic;

namespace GIKCore
{
    public readonly struct AnalyticsEvent
    {
        public readonly string Name;
        public readonly Dictionary<string, object> Parameters;
        public readonly float? ValueToSum;

        public AnalyticsEvent(string name, Dictionary<string, object> parameters, float? valueToSum = null)
        {
            Name = name;
            Parameters = parameters;
            ValueToSum = valueToSum;
        }
    }
}
