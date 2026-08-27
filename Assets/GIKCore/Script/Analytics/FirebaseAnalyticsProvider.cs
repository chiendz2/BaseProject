#if FIREBASE_ANALYTICS
using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using Firebase.Analytics;

namespace GIKCore
{
    public static class FirebaseAnalyticsProvider
    {
        private const string LogTag = "[FirebaseAnalyticsProvider]";
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

        public static void SetUserProperty(string propertyName, string value)
        {
            if (string.IsNullOrEmpty(propertyName))
            {
                return;
            }

            FirebaseAnalytics.SetUserProperty(propertyName, value);
        }

        public static void SetUserId(string userId)
        {
            FirebaseAnalytics.SetUserId(userId);
        }

        private static void Send(AnalyticsEvent pendingEvent)
        {
            try
            {
                Parameter[] parameters = ToParameters(pendingEvent.Parameters);
                if (parameters == null)
                {
                    FirebaseAnalytics.LogEvent(pendingEvent.Name);
                }
                else
                {
                    FirebaseAnalytics.LogEvent(pendingEvent.Name, parameters);
                }
            }
            catch (Exception exception)
            {
                Debug.LogError($"{LogTag} Failed to send {pendingEvent.Name}: {exception}");
            }
        }

        private static Parameter[] ToParameters(Dictionary<string, object> parameters)
        {
            if (parameters == null || parameters.Count == 0)
            {
                return null;
            }

            List<Parameter> converted = new List<Parameter>(parameters.Count);
            foreach (KeyValuePair<string, object> pair in parameters)
            {
                if (string.IsNullOrEmpty(pair.Key) || pair.Value == null)
                {
                    continue;
                }

                if (pair.Value is double doubleValue)
                {
                    converted.Add(new Parameter(pair.Key, doubleValue));
                }
                else if (pair.Value is float floatValue)
                {
                    converted.Add(new Parameter(pair.Key, floatValue));
                }
                else if (pair.Value is long longValue)
                {
                    converted.Add(new Parameter(pair.Key, longValue));
                }
                else if (pair.Value is int intValue)
                {
                    converted.Add(new Parameter(pair.Key, intValue));
                }
                else if (pair.Value is bool boolValue)
                {
                    converted.Add(new Parameter(pair.Key, boolValue ? 1L : 0L));
                }
                else
                {
                    string text = Convert.ToString(pair.Value, CultureInfo.InvariantCulture);
                    if (!string.IsNullOrEmpty(text))
                    {
                        converted.Add(new Parameter(pair.Key, text));
                    }
                }
            }

            return converted.Count == 0 ? null : converted.ToArray();
        }
    }
}
#endif
