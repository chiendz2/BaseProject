using System.Collections.Generic;

namespace GIKCore
{
    public readonly struct AnalyticsPurchase
    {
        public readonly decimal Amount;
        public readonly string Currency;
        public readonly Dictionary<string, object> Parameters;

        public AnalyticsPurchase(decimal amount, string currency, Dictionary<string, object> parameters)
        {
            Amount = amount;
            Currency = currency;
            Parameters = parameters;
        }
    }
}
