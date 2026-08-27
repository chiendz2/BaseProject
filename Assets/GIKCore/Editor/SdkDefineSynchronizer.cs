using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace GIKCore
{
    [InitializeOnLoad]
    public static class SdkDefineSynchronizer
    {
        private const string LogTag = "[SdkDefineSynchronizer]";

        private readonly struct SdkDefine
        {
            public readonly string Symbol;
            public readonly string TypeName;

            public SdkDefine(string symbol, string typeName)
            {
                Symbol = symbol;
                TypeName = typeName;
            }
        }

        private static readonly NamedBuildTarget[] Targets =
        {
            NamedBuildTarget.Android,
            NamedBuildTarget.iOS,
            NamedBuildTarget.Standalone
        };

        private static readonly SdkDefine[] Defines =
        {
            new SdkDefine("FIREBASE_SDK", "Firebase.FirebaseApp"),
            new SdkDefine("FIREBASE_ANALYTICS", "Firebase.Analytics.FirebaseAnalytics"),
            new SdkDefine("FIREBASE_CRASHLYTICS", "Firebase.Crashlytics.Crashlytics"),
            new SdkDefine("FIREBASE_REMOTE_CONFIG", "Firebase.RemoteConfig.FirebaseRemoteConfig"),
            new SdkDefine("APPSFLYER_SDK", "AppsFlyerSDK.AppsFlyer"),
            new SdkDefine("FACEBOOK_SDK", "Facebook.Unity.FB")
        };

        static SdkDefineSynchronizer()
        {
            EditorApplication.delayCall += Synchronize;
        }

        [MenuItem("GIKCore/Sync SDK Defines")]
        public static void Synchronize()
        {
            bool changed = false;
            for (int i = 0; i < Targets.Length; i++)
            {
                changed |= SynchronizeTarget(Targets[i]);
            }

            if (!changed)
            {
                return;
            }

            AssetDatabase.SaveAssets();
            EditorApplication.ExecuteMenuItem("File/Save Project");
        }

        private static bool SynchronizeTarget(NamedBuildTarget target)
        {
            string current = PlayerSettings.GetScriptingDefineSymbols(target);
            List<string> symbols = new List<string>(current.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries));
            bool changed = false;

            for (int i = 0; i < Defines.Length; i++)
            {
                SdkDefine define = Defines[i];
                bool present = IsTypePresent(define.TypeName);
                bool declared = symbols.Contains(define.Symbol);

                if (present && !declared)
                {
                    symbols.Add(define.Symbol);
                    changed = true;
                    Debug.Log($"{LogTag} {target.TargetName}: added {define.Symbol}");
                }
                else if (!present && declared)
                {
                    symbols.Remove(define.Symbol);
                    changed = true;
                    Debug.Log($"{LogTag} {target.TargetName}: removed {define.Symbol}");
                }
            }

            if (changed)
            {
                PlayerSettings.SetScriptingDefineSymbols(target, string.Join(";", symbols.ToArray()));
            }

            return changed;
        }

        private static bool IsTypePresent(string typeName)
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < assemblies.Length; i++)
            {
                try
                {
                    if (assemblies[i].GetType(typeName, false) != null)
                    {
                        return true;
                    }
                }
                catch (Exception)
                {
                }
            }

            return false;
        }
    }
}
