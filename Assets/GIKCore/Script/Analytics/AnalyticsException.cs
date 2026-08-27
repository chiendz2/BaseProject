using System;

namespace GIKCore
{
    public class AnalyticsException : Exception
    {
        public override string StackTrace { get; }

        public AnalyticsException(string source, string message) : base($"{source}: {message}")
        {
            StackTrace = source;
        }
    }
}
