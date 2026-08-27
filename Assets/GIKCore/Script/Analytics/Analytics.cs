using System.Collections.Generic;
using UnityEngine;

namespace GIKCore
{
    public static class Analytics
    {
        private const string LogTag = "[Analytics]";

        private static readonly HashSet<string> AppsFlyerOnlyEvents = new HashSet<string>
        {
            EventName.AfSession,
            EventName.AfTutorialCompletion,
            EventName.AfInterLoaded,
            EventName.AfInterDisplayed,
            EventName.AfRewardedLoaded,
            EventName.AfRewardedDisplayed
        };

        public static bool IsAppsFlyerOnlyEvent(string eventName)
        {
            return !string.IsNullOrEmpty(eventName) && AppsFlyerOnlyEvents.Contains(eventName);
        }

        public static void LogEvent(string eventName, Dictionary<string, object> parameters = null)
        {
            if (string.IsNullOrEmpty(eventName))
            {
                Debug.LogError($"{LogTag} LogEvent called with an empty event name.");
                return;
            }

            Dictionary<string, object> payload = CopyParameters(parameters);

            if (IsAppsFlyerOnlyEvent(eventName))
            {
                DispatchAppsFlyer(eventName, payload);
                DispatchFacebook(eventName, payload);
                return;
            }

            DispatchFirebase(eventName, payload);
        }

        public static void LogLevel(string eventName, int level, Dictionary<string, object> parameters = null)
        {
            Dictionary<string, object> payload = parameters == null
                ? new Dictionary<string, object>(1)
                : new Dictionary<string, object>(parameters);

            payload[ParameterName.Level] = level;
            LogEvent(eventName, payload);
        }

        public static void LogScreen(string screenName, string screenClass = null)
        {
            if (string.IsNullOrEmpty(screenName))
            {
                Debug.LogError($"{LogTag} LogScreen called with an empty screen name.");
                return;
            }

            Dictionary<string, object> payload = new Dictionary<string, object>(2)
            {
                { ParameterName.ScreenName, screenName }
            };

            if (!string.IsNullOrEmpty(screenClass))
            {
                payload[ParameterName.ScreenClass] = screenClass;
            }

            LogEvent(EventName.ScreenView, payload);
        }

        public static void LogPurchase(string productName, decimal price, string currency, Dictionary<string, object> parameters = null)
        {
            if (string.IsNullOrEmpty(currency))
            {
                Debug.LogError($"{LogTag} LogPurchase called with an empty currency.");
                return;
            }

            Dictionary<string, object> payload = parameters == null
                ? new Dictionary<string, object>(3)
                : new Dictionary<string, object>(parameters);

            payload[ParameterName.ProductName] = productName;
            payload[ParameterName.Price] = decimal.ToDouble(price);
            payload[ParameterName.Currency] = currency;

            DispatchFirebase(EventName.IapSuccess, payload);
            DispatchAppsFlyer(EventName.AfPurchase, payload);
            DispatchFacebookPurchase(price, currency, payload);
        }

        public static void LogAdRevenue(double revenue, string currency, string adFormat, Dictionary<string, object> parameters = null)
        {
            if (string.IsNullOrEmpty(currency))
            {
                Debug.LogError($"{LogTag} LogAdRevenue called with an empty currency.");
                return;
            }

            Dictionary<string, object> payload = parameters == null
                ? new Dictionary<string, object>(3)
                : new Dictionary<string, object>(parameters);

            payload[ParameterName.Revenue] = revenue;
            payload[ParameterName.Currency] = currency;
            payload[ParameterName.AdFormat] = adFormat;

            DispatchFirebase(EventName.AdRevenue, payload);
            DispatchAppsFlyer(EventName.AfAdRevenue, payload);
            DispatchFacebook(EventName.AfAdRevenue, payload, (float)revenue);
        }

        public static void SetUserProperty(string propertyName, string value)
        {
            if (string.IsNullOrEmpty(propertyName))
            {
                Debug.LogError($"{LogTag} SetUserProperty called with an empty property name.");
                return;
            }

            DispatchUserProperty(propertyName, value);
        }

        public static void SetUserProperty(string propertyName, int value)
        {
            SetUserProperty(propertyName, value.ToString());
        }

        public static void SetUserProperty(string propertyName, bool value)
        {
            SetUserProperty(propertyName, value ? "true" : "false");
        }

        public static void SetUserId(string userId)
        {
            DispatchUserId(userId);
        }

        public static void LogException(string source, string message)
        {
            CrashlyticsReporter.LogException(source, message);
        }

        public static Dictionary<string, object> CreateParam(string name, object value)
        {
            return new Dictionary<string, object>(1) { { name, value } };
        }

        public static Dictionary<string, object> CreateParam(
            string name1, object value1,
            string name2, object value2)
        {
            return new Dictionary<string, object>(2)
            {
                { name1, value1 },
                { name2, value2 }
            };
        }

        public static Dictionary<string, object> CreateParam(
            string name1, object value1,
            string name2, object value2,
            string name3, object value3)
        {
            return new Dictionary<string, object>(3)
            {
                { name1, value1 },
                { name2, value2 },
                { name3, value3 }
            };
        }

        public static Dictionary<string, object> CreateParam(
            string name1, object value1,
            string name2, object value2,
            string name3, object value3,
            string name4, object value4)
        {
            return new Dictionary<string, object>(4)
            {
                { name1, value1 },
                { name2, value2 },
                { name3, value3 },
                { name4, value4 }
            };
        }

        private static Dictionary<string, object> CopyParameters(Dictionary<string, object> parameters)
        {
            return parameters == null || parameters.Count == 0
                ? null
                : new Dictionary<string, object>(parameters);
        }

        private static void DispatchFirebase(string eventName, Dictionary<string, object> payload)
        {
#if FIREBASE_ANALYTICS
            FirebaseAnalyticsProvider.LogEvent(eventName, payload);
#endif
        }

        private static void DispatchAppsFlyer(string eventName, Dictionary<string, object> payload)
        {
#if APPSFLYER_SDK
            AppsFlyerProvider.LogEvent(eventName, payload);
#endif
        }

        private static void DispatchFacebook(string eventName, Dictionary<string, object> payload, float? valueToSum = null)
        {
#if FACEBOOK_SDK
            FacebookProvider.LogEvent(eventName, payload, valueToSum);
#endif
        }

        private static void DispatchFacebookPurchase(decimal price, string currency, Dictionary<string, object> payload)
        {
#if FACEBOOK_SDK
            FacebookProvider.LogPurchase(price, currency, payload);
#endif
        }

        private static void DispatchUserProperty(string propertyName, string value)
        {
#if FIREBASE_ANALYTICS
            FirebaseAnalyticsProvider.SetUserProperty(propertyName, value);
#endif
        }

        private static void DispatchUserId(string userId)
        {
#if FIREBASE_ANALYTICS
            FirebaseAnalyticsProvider.SetUserId(userId);
#endif
#if APPSFLYER_SDK
            AppsFlyerProvider.SetCustomerUserId(userId);
#endif
        }
    }
}
