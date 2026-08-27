#if FACEBOOK_SDK
using System;
using System.Collections.Generic;
using UnityEngine;
using Facebook.Unity;

namespace GIKCore
{
    public static class FacebookProvider
    {
        private const string LogTag = "[FacebookProvider]";
        private const int QueueLimit = 50;

        private static readonly Queue<AnalyticsEvent> PendingEvents = new Queue<AnalyticsEvent>();
        private static readonly Queue<AnalyticsPurchase> PendingPurchases = new Queue<AnalyticsPurchase>();
        private static bool _ready;

        public static bool IsReady
        {
            get { return _ready; }
        }

        public static void SetReady()
        {
            _ready = true;
            Flush();
        }

        public static void LogEvent(string eventName, Dictionary<string, object> parameters, float? valueToSum = null)
        {
            if (string.IsNullOrEmpty(eventName))
            {
                return;
            }

            AnalyticsEvent pendingEvent = new AnalyticsEvent(eventName, parameters, valueToSum);
            if (_ready)
            {
                Send(pendingEvent);
                return;
            }

            if (PendingEvents.Count >= QueueLimit)
            {
                PendingEvents.Dequeue();
            }

            PendingEvents.Enqueue(pendingEvent);
        }

        public static void LogPurchase(decimal amount, string currency, Dictionary<string, object> parameters)
        {
            if (amount <= decimal.Zero || string.IsNullOrEmpty(currency))
            {
                return;
            }

            AnalyticsPurchase pendingPurchase = new AnalyticsPurchase(amount, currency, parameters);
            if (_ready)
            {
                SendPurchase(pendingPurchase);
                return;
            }

            if (PendingPurchases.Count >= QueueLimit)
            {
                PendingPurchases.Dequeue();
            }

            PendingPurchases.Enqueue(pendingPurchase);
        }

        public static void Flush()
        {
            while (PendingEvents.Count > 0)
            {
                Send(PendingEvents.Dequeue());
            }

            while (PendingPurchases.Count > 0)
            {
                SendPurchase(PendingPurchases.Dequeue());
            }
        }

        private static void Send(AnalyticsEvent pendingEvent)
        {
            try
            {
                FB.LogAppEvent(pendingEvent.Name, pendingEvent.ValueToSum, pendingEvent.Parameters);
            }
            catch (Exception exception)
            {
                Debug.LogError($"{LogTag} Failed to send {pendingEvent.Name}: {exception}");
            }
        }

        private static void SendPurchase(AnalyticsPurchase pendingPurchase)
        {
            try
            {
                FB.LogPurchase(pendingPurchase.Amount, pendingPurchase.Currency, pendingPurchase.Parameters);
            }
            catch (Exception exception)
            {
                Debug.LogError($"{LogTag} Failed to send purchase: {exception}");
            }
        }
    }
}
#endif
