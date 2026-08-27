#if APPSFLYER_SDK
using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using AppsFlyerSDK;

namespace GIKCore
{
    public static class AppsFlyerProvider
    {
        private const string LogTag = "[AppsFlyerProvider]";
        private const int QueueLimit = 50;

        private static readonly Queue<AnalyticsEvent> PendingEvents = new Queue<AnalyticsEvent>();
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

        public static void LogEvent(string eventName, Dictionary<string, object> parameters)
        {
            if (string.IsNullOrEmpty(eventName))
            {
                return;
            }

            AnalyticsEvent pendingEvent = new AnalyticsEvent(eventName, parameters);
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

        public static void Flush()
        {
            while (PendingEvents.Count > 0)
            {
                Send(PendingEvents.Dequeue());
            }
        }

        public static void SetCustomerUserId(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return;
            }

            AppsFlyer.setCustomerUserId(userId);
        }

        private static void Send(AnalyticsEvent pendingEvent)
        {
            try
            {
                AppsFlyer.sendEvent(pendingEvent.Name, ToStringMap(pendingEvent.Parameters));
            }
            catch (Exception exception)
            {
                Debug.LogError($"{LogTag} Failed to send {pendingEvent.Name}: {exception}");
            }
        }

        private static Dictionary<string, string> ToStringMap(Dictionary<string, object> parameters)
        {
            if (parameters == null || parameters.Count == 0)
            {
                return null;
            }

            Dictionary<string, string> converted = new Dictionary<string, string>(parameters.Count);
            foreach (KeyValuePair<string, object> pair in parameters)
            {
                if (string.IsNullOrEmpty(pair.Key) || pair.Value == null)
                {
                    continue;
                }

                converted[pair.Key] = Convert.ToString(pair.Value, CultureInfo.InvariantCulture);
            }

            return converted.Count == 0 ? null : converted;
        }
    }
}
#endif
