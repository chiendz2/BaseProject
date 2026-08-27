#define DEBUG_ANALYTIC_INTERNAL
using System;
using System.Collections.Generic;
using System.Text;
#if FIREBASE_SDK
using Firebase.Analytics;
#endif
using Inwave.Production.Helper;
using Inwave.Production.Utils;
using UnityEngine;
using UnityEngine.Analytics;

public partial class AnalyticHelper : FastSingleton<AnalyticHelper>
    {
#if DEBUG_ANALYTIC
        private const string LogTag = "AnalyticHelper";
#endif
        private const string _deviceInfo = "DeviceInfo";
        private const string _firebaseEventPrefix = "FirebaseEvent";
        private static string _levelPrefix = "new_lvup";

        public static bool IsAdmin()
        {
            return Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.OSXEditor;
        }

        public static bool IsChina()
        {
            return false;
        }

        public static void FireDeviceDetail()
        {
            FireString(_deviceInfo, DeviceHelper.GetDeviceDetails());
        }

        public static void FireString(string name, Dictionary<string, object> data = null)
        {
            LogEvent(name, data);
        }

        public static void LogScreen(string screenName)
        {
#if FIREBASE_SDK
            Dictionary<string, object> param = new Dictionary<string, object>
            {
                { FirebaseAnalytics.ParameterScreenName, screenName }
            };
            LogEvent(FirebaseAnalytics.EventScreenView, param);
#endif
        }

        public static void LogScreen(string screenName, string screenClass)
        {
#if FIREBASE_SDK
            Dictionary<string, object> param = new Dictionary<string, object>
            {
                { FirebaseAnalytics.ParameterScreenName, screenName },
                { FirebaseAnalytics.ParameterScreenClass, screenClass.ToLower() }
            };
            LogEvent(FirebaseAnalytics.EventScreenView, param);
#endif
        }

        public static void LogLevel(int level)
        {
            LogLevel(level,null);
        }

        public static void SetUserProperty(string userPropertyName, int value)
        {
#if FIREBASE_SDK
        FirebaseAnalytics.SetUserProperty(userPropertyName, value.ToString());
#endif
        }
        public static void SetUserProperty(string userPropertyName, bool boolenValue)
        {
#if FIREBASE_SDK
        FirebaseAnalytics.SetUserProperty(userPropertyName, boolenValue.ToString());
#endif
        }

        public static void LogLevel(int level,Dictionary<string,object> param)
        {
            LogEvent($"{_levelPrefix}_{level}",param);
        }

        #region Core
#if FIREBASE_SDK
        static List<KeyValuePair<string, Parameter[]>> queueData = new List<KeyValuePair<string, Parameter[]>>();
#endif
        public static void LogEvent(string eventName, Dictionary<string, object> param = null)
        {
            LogThirdPartyEvent(eventName, param);

#if FIREBASE_SDK
            try
            {


            {
#if UNITY_EDITOR
                        StringBuilder stringBuilder = new StringBuilder($"{_firebaseEventPrefix}: {eventName}");

                        if (param != null && param.Count > 0)
                        {
                            stringBuilder.Append("{ ");

                            foreach (var p in param)
                            {
                                stringBuilder.Append($" {p.Key} : \"{p.Value}\";");
                            }

                            stringBuilder.Append(" }");
                        }
#endif

                if (IsChina())
                    {
                        Analytics.CustomEvent(eventName, param);
                    }

                    // Event af_* là của AppsFlyer/Facebook -> không đưa vào hàng đợi Firebase.
                    // Vẫn đi tiếp xuống dưới để phần flush hàng đợi chạy bình thường.
                    if (!IsAppsFlyerOnlyEvent(eventName) && queueData.Count < 50)
                    {
                        List<Parameter> parameters = new List<Parameter>();
                        if (param != null && param.Count > 0)
                        {
                            foreach (var p in param)
                            {
                                if (!string.IsNullOrEmpty(p.Key) && p.Value != null)
                                {
                                    if (p.Value is double)
                                    {
                                        parameters.Add(new Parameter(p.Key, (double)p.Value));
                                    }
                                    else if (p.Value is long)
                                    {
                                        parameters.Add(new Parameter(p.Key, (long)p.Value));
                                    }
                                    else if (p.Value is float)
                                    {
                                        parameters.Add(new Parameter(p.Key, (float)p.Value));
                                    }
                                    else if (p.Value is int)
                                    {
                                        parameters.Add(new Parameter(p.Key, (int)p.Value));
                                    }
                                    else if (!string.IsNullOrEmpty(p.Value.ToString()))
                                    {
                                        parameters.Add(new Parameter(p.Key, p.Value.ToString()));
                                    }
                                }
                            }
                        }

                        queueData.Add(new KeyValuePair<string, Parameter[]>(eventName, parameters.ToArray()));
                    }
//#if DEBUG_ANALYTIC && DEBUG_ANALYTIC_INTERNAL
//                    LogUtils.LogWarning(LogTag, $"Ready: {FirebaseSDK.Status == FirebaseSDK.FirebaseStatus.Ready}");
//#endif
                    FlushFirebaseQueue();
                }
            }
            catch (Exception ex)
            {
                CustomException.Fire($"LogEvent:{eventName}", ex.ToString());
            }
#endif
        }

        internal static void FlushFirebaseQueue()
        {
#if FIREBASE_SDK
            if (FirebaseSDK.Status != FirebaseSDK.FirebaseStatus.Ready)
                return;

            foreach (var data in queueData)
            {
                FirebaseAnalytics.LogEvent(data.Key, data.Value);
                LogProviderSubmission("Firebase", data.Key);
            }

            queueData.Clear();
#endif
        }

#endregion


        #region Param Utils

        public static Dictionary<string, object> CreateParam(string name, object value)
        {
            Dictionary<string,object> param = new Dictionary<string,object> { { name, value } };
            return param;
        }

        public static Dictionary<string, object> CreateParam(
            string name1, object value1,
            string name2, object value2)
        {
            Dictionary<string, object> param = new Dictionary<string, object>
            {
                { name1, value1 },
                { name2, value2 }
            };
            return param;
        }

        public static Dictionary<string, object> CreateParam(
            string name1, object value1,
            string name2, object value2,
            string name3, object value3)
        {
            Dictionary<string, object> param = new Dictionary<string, object>
            {
                { name1, value1 },
                { name2, value2 },
                { name3, value3 }
            };
            return param;
        }

    #endregion
}
