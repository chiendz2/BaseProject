using System;
using UnityEngine;
#if FIREBASE_CRASHLYTICS
using Firebase.Crashlytics;
#endif

namespace GIKCore
{
    public static class CrashlyticsReporter
    {
        private const string LogTag = "[CrashlyticsReporter]";

#if FIREBASE_CRASHLYTICS
        public static void Log(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            Crashlytics.Log(message);
        }

        public static void SetCustomKey(string key, string value)
        {
            if (string.IsNullOrEmpty(key))
            {
                return;
            }

            Crashlytics.SetCustomKey(key, value);
        }

        public static void LogException(string source, string message)
        {
            Crashlytics.LogException(new AnalyticsException(source, message));
        }

        public static void LogException(Exception exception)
        {
            if (exception == null)
            {
                return;
            }

            Crashlytics.LogException(exception);
        }
#else
        public static void Log(string message)
        {
        }

        public static void SetCustomKey(string key, string value)
        {
        }

        public static void LogException(string source, string message)
        {
            Debug.LogWarning($"{LogTag} FIREBASE_CRASHLYTICS is not defined: {source}: {message}");
        }

        public static void LogException(Exception exception)
        {
            if (exception == null)
            {
                return;
            }

            Debug.LogWarning($"{LogTag} FIREBASE_CRASHLYTICS is not defined: {exception}");
        }
#endif
    }
}
