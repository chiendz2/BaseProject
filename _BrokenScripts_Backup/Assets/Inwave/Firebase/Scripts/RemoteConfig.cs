#define DEBUG_ANALYTIC_INTERNAL
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Reflection;
using System.Text;
#if FIREBASE_SDK
using Firebase.Extensions;
using Firebase.RemoteConfig;
using Google.MiniJSON;
#endif
using Inwave.Production.Utils;


/// <summary>
/// Create a partial 'RemoteConfig' class for adding more properties
/// </summary>
public partial class RemoteConfig : FastSingleton<RemoteConfig>
{
    public static bool Loaded { get; private set; }

    public static string LogTag = "RemoteConfig";

    #region events

    public event Action OnEndLoad;

    public event Action OnLoadFailed;

    #endregion

    public const string EnableRemoteSyncKey = "EnableRemoteSync";

    [Space(height: 10), Header("Base")]
    public string MediationDataSegment;

    private static string prefix = "rc_";

    public bool EnableRemoteSync { get; private set; }

    public string NewUserParams { get; set; }


    public bool Initialized { get; private set; }

    public bool AutoInitialization => _autoInitialization;

    [SerializeField]
    [Space(height: 10, order = -1), Header("Init")]
    private bool _autoInitialization = false;

#if FIREBASE_SDK
    private FirebaseRemoteConfig _fbRemoteConfigInstance;
#endif
    public void SetEnableRemoteSync(bool enable)
    {
        if (enable)
        {
            PlayerPrefs.SetInt(EnableRemoteSyncKey, 1);
        }
        else
        {
            PlayerPrefs.SetInt(EnableRemoteSyncKey, 0);
        }
        PlayerPrefs.Save();
    }

    void Start()
    {
        if (_autoInitialization && !Initialized)
        {
            Initialize();
        }
    }
    public void Initialize()
    {
        // Load from the save config
        LoadFromPrefs();
        EnableRemoteSync = PlayerPrefs.GetInt(EnableRemoteSyncKey, 1) == 1;
        if (!EnableRemoteSync)
        {
            string path = GetLocalConfigPath();
            if (File.Exists(path))
            {
                try
                {
                    string contents = File.ReadAllText(path);
                    JsonUtility.FromJsonOverwrite(contents, Instance);
                }
                catch (Exception e)
                {
#if DEBUG_ANALYTIC && DEBUG_ANALYTIC_INTERNAL
                        LogError("Initialize " + e.Message);
#endif
                }
            }
        }

        Initialized = true;
        Loaded = false;
    }

    private string GetLocalConfigPath()
    {
        string path = $"{Application.persistentDataPath}/{LocalFile.LocalConfig}.json";
        return path;
    }

    // Load remote config when firebase is ready
    public void LoadRemoteConfig()
    {
#if FIREBASE_SDK
        _fbRemoteConfigInstance = FirebaseRemoteConfig.DefaultInstance;
        if (EnableRemoteSync)
        {
            FetchDataAsync();
        }
        else
        {
            EndLoad();
#if DEBUG_ANALYTIC && DEBUG_ANALYTIC_INTERNAL
                LogWarning("RemoteConfig Sync is disabled");
#endif
        }
#endif
    }

    public bool IsChina()
    {
        return false;
    }
    // End load remote config and process the final config
    public void EndLoad(bool isNew = false)
    {
        if (Loaded)
        {
            return;
        }
        try
        {
            Loaded = true;
            InitConfigValues();
            OnEndLoad?.Invoke();
            GameEvents.OnRemoteConfigLoaded?.Invoke();
        }
        catch (Exception ex)
        {
            CustomException.Fire("OnLoaded Trigger", ex.ToString());
        }
    }

    void FetchDataAsync()
    {
#if FIREBASE_SDK
        AnalyticsFTUE.LogEvent(EventName.FTUE_load_config);
        var setting = _fbRemoteConfigInstance.ConfigSettings;
        setting.MinimumFetchIntervalInMilliseconds = 0;
#if DEBUG_ANALYTIC && DEBUG_ANALYTIC_INTERNAL
            LogWarning("Fetching data admin...");
#endif
        _fbRemoteConfigInstance.SetConfigSettingsAsync(setting).ContinueWithOnMainThread(task =>
        {
            // handle completion
            System.Threading.Tasks.Task fetchTask = _fbRemoteConfigInstance.FetchAndActivateAsync();
            fetchTask.ContinueWithOnMainThread(FetchComplete);
        });
#endif
    }

    void FetchComplete(System.Threading.Tasks.Task fetchTask)
    {
#if FIREBASE_SDK
        if (Loaded)
        {
            return;
        }
#if DEBUG_ANALYTIC && DEBUG_ANALYTIC_INTERNAL
            if (fetchTask.IsCanceled)
            {
                LogWarning("Fetch canceled.");
            }
            else if (fetchTask.IsFaulted)
            {
                LogError("Fetch encountered an error.");
                OnLoadFailed?.Invoke();
            }
            else if (fetchTask.IsCompleted)
            {
                LogWarning("Fetch completed successfully!");
            }
#endif
        var info = _fbRemoteConfigInstance.Info;
        switch (info.LastFetchStatus)
        {
            case LastFetchStatus.Success:
                AnalyticsFTUE.LogEvent(EventName.FTUE_load_config_success);
                MergeAllKeys();
                SaveToPrefs();
#if DEBUG_ANALYTIC && DEBUG_ANALYTIC_INTERNAL
                    LogWarning($"Remote data loaded and ready (last fetch time {info.FetchTime}).");
#endif
                break;
            case LastFetchStatus.Failure:
                AnalyticsFTUE.LogEvent(EventName.FTUE_load_config_failed);
                switch (info.LastFetchFailureReason)
                {
                    case FetchFailureReason.Error:
#if DEBUG_ANALYTIC && DEBUG_ANALYTIC_INTERNAL
                            LogError("Fetch failed for unknown reason");
#endif
                        break;
                    case FetchFailureReason.Throttled:
#if DEBUG_ANALYTIC && DEBUG_ANALYTIC_INTERNAL
                            LogError("Fetch throttled until " + info.ThrottledEndTime);
#endif
                        break;
                }
                break;
            case LastFetchStatus.Pending:
#if DEBUG_ANALYTIC && DEBUG_ANALYTIC_INTERNAL
                    LogError("Latest Fetch call still pending.");
#endif
                break;
        }

        EndLoad(info.LastFetchStatus == LastFetchStatus.Success);
#endif
    }

    // Save config to PlayerPrefs
    private void SaveToPrefs()
    {
        FieldInfo[] list = Instance.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);
        foreach (FieldInfo item in list)
        {
            object fieldValue = item.GetValue(Instance);
            string itemName = prefix + item.Name;

            if (item.FieldType == typeof(string))
            {
                PlayerPrefs.SetString(itemName, fieldValue as string);

            }
            else if (item.FieldType == typeof(float))
            {
                PlayerPrefs.SetFloat(itemName, (float)fieldValue);

            }
            else if (item.FieldType == typeof(int))
            {
                PlayerPrefs.SetInt(itemName, (int)fieldValue);

            }
            else if (item.FieldType == typeof(bool))
            {
                PlayerPrefs.SetInt(itemName, (bool)fieldValue ? 1 : 0);

            }
            else if (item.FieldType == typeof(int[]))
            {
                int[] valueOri = (int[])fieldValue;
                if (valueOri != null && valueOri.Length > 0)
                {
                    PlayerPrefs.SetString(itemName, string.Join(",", valueOri));
                }

            }
            else if (item.FieldType == typeof(long))
            {
                long value = (long)fieldValue;
                PlayerPrefs.SetString(itemName, value.ToString());

            }
            else if (item.FieldType.IsClass)
            {
                PlayerPrefs.SetString(itemName, JsonUtility.ToJson(fieldValue));
            }
#if DEBUG_ANALYTIC && DEBUG_ANALYTIC_INTERNAL
                else
                {
                    LogWarning($"[SaveToPrefs] cannot process type {item.FieldType}");
                }
#endif
        }

        PlayerPrefs.Save();
    }

    // Load config from PlayerPrefs
    private void LoadFromPrefs()
    {
        FieldInfo[] list = Instance.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);
        foreach (FieldInfo item in list)
        {
            object itemValue = item.GetValue(Instance);
            string itemName = prefix + item.Name;

            if (item.FieldType == typeof(string))
            {
                item.SetValue(Instance, PlayerPrefs.GetString(itemName, itemValue as string));

            }
            else if (item.FieldType == typeof(float))
            {
                item.SetValue(Instance, PlayerPrefs.GetFloat(itemName, (float)itemValue));

            }
            else if (item.FieldType == typeof(int))
            {
                item.SetValue(Instance, PlayerPrefs.GetInt(itemName, (int)itemValue));

            }
            else if (item.FieldType == typeof(bool))
            {
                item.SetValue(Instance, PlayerPrefs.GetInt(itemName, (bool)itemValue ? 1 : 0) == 1);

            }
            else if (item.FieldType == typeof(int[]))
            {
                string defaultValue = PlayerPrefs.GetString(itemName);
                if (!string.IsNullOrEmpty(defaultValue))
                {
                    item.SetValue(Instance, GetIntArray(defaultValue));
                }

            }
            else if (item.FieldType == typeof(long))
            {
                string defaultValue = PlayerPrefs.GetString(itemName, itemValue as string);
                if (!string.IsNullOrEmpty(defaultValue))
                {
                    item.SetValue(Instance, long.Parse(defaultValue));
                }

            }
            else if (item.FieldType.IsClass)
            {
                string json = PlayerPrefs.GetString(itemName, "");
                if (!string.IsNullOrEmpty(json))
                {
                    var obj = item.GetValue(this);
                    JsonUtility.FromJsonOverwrite(json, obj);
                }
            }
#if DEBUG_ANALYTIC && DEBUG_ANALYTIC_INTERNAL
                else
                {
                    LogWarning($"[LoadFromPrefs] cannot process type {item.FieldType}");
                }
#endif
        }
    }


    //Merge remote config with local config
    private void MergeAllKeys()
    {
#if FIREBASE_SDK
        IEnumerable<string> keys = _fbRemoteConfigInstance.Keys;

        //Update new user params first
        string newUserParamKey = "NewUserParams";
        foreach (string key in keys)
        {
            if (key == newUserParamKey)
            {
                NewUserParams = _fbRemoteConfigInstance.GetValue(key).StringValue;
            }
        }
        foreach (string key in keys)
        {
            if (IsSkipUpdate(key))
            {
                continue;
            }

            string settings = "Settings";
            if (key.Contains(settings))
            {
                Dictionary<string, System.Object> jsonDict =
                    (Dictionary<string, System.Object>)Json.Deserialize(_fbRemoteConfigInstance.GetValue(key).StringValue);
                MergePreKeys(jsonDict, key.Replace(settings, ""));
            }
            else
            {
                try
                {
                    FieldInfo item = typeof(RemoteConfig).GetField(key);
                    if (item != null)
                    {
#if DEBUG_ANALYTIC && DEBUG_ANALYTIC_INTERNAL
                            string oldValue = item.GetValue(Instance).ToString();
#endif
                        if (item.FieldType == typeof(string))
                        {
                            item.SetValue(Instance, _fbRemoteConfigInstance.GetValue(key).StringValue);

                        }
                        else if (item.FieldType == typeof(float))
                        {
                            item.SetValue(Instance, (float)_fbRemoteConfigInstance.GetValue(key).DoubleValue);

                        }
                        else if (item.FieldType == typeof(int))
                        {
                            item.SetValue(Instance, (int)_fbRemoteConfigInstance.GetValue(key).LongValue);

                        }
                        else if (item.FieldType == typeof(bool))
                        {
                            if (_fbRemoteConfigInstance.GetValue(key).BooleanValue)
                            {
                                item.SetValue(Instance, true);
                            }
                            else
                            {
                                item.SetValue(Instance, false);
                            }
                        }
                        else if (item.FieldType == typeof(int[]))
                        {
                            item.SetValue(Instance, GetIntArray(_fbRemoteConfigInstance.GetValue(key).StringValue));
                        }
                        else if (item.FieldType == typeof(long))
                        {
                            item.SetValue(Instance, _fbRemoteConfigInstance.GetValue(key).LongValue);
                        }
                        else if (item.FieldType.IsClass)
                        {
                            string json = _fbRemoteConfigInstance.GetValue(key).StringValue;
                            if (!string.IsNullOrEmpty(json))
                            {
                                var obj = item.GetValue(this);
                                JsonUtility.FromJsonOverwrite(json, obj);
                            }
                        }
#if DEBUG_ANALYTIC && DEBUG_ANALYTIC_INTERNAL
                            else
                            {
                                LogWarning($"[MergeAllKeys] cannot process type {item.FieldType}");
                            }
#endif
#if DEBUG_ANALYTIC && DEBUG_ANALYTIC_INTERNAL
                            if (oldValue != item.GetValue(Instance).ToString())
                            {
                                LogWarning(
                                    $"{key}: Remote Value Updated: {oldValue}-->{item.GetValue(Instance)}");
                            }
#endif
                    }
#if DEBUG_ANALYTIC && DEBUG_ANALYTIC_INTERNAL
                        else
                        {

                            LogWarning($"Key {key} from Firebase not found in class RemoteConfig.cs!");

                        }
#endif

                }
                catch (Exception ex)
                {
#if DEBUG_ANALYTIC && DEBUG_ANALYTIC_INTERNAL
                        LogError("Invalid key: " + key + "--" + ex.Message);
#endif
                }
            }
        }
#endif
    }


    bool IsAdmin()
    {
        return false;
    }

    bool IsSkipUpdate(string key)
    {
        return !IsAdmin() && NewUserParams != null && NewUserParams.Contains(key);
    }

    private void MergePreKeys(Dictionary<string, System.Object> jsonDict, string pre)
    {
        foreach (KeyValuePair<string, System.Object> data in jsonDict)
        {
            if (IsSkipUpdate(pre + data.Key))
            {
                continue;
            }

            try
            {
                FieldInfo item = typeof(RemoteConfig).GetField(pre + data.Key);
                if (item != null)
                {
#if DEBUG_ANALYTIC && DEBUG_ANALYTIC_INTERNAL
                        string oldValue = item.GetValue(Instance).ToString();
#endif
                    if (item.FieldType == typeof(string))
                    {
                        item.SetValue(Instance, data.Value.ToString());

                    }
                    else if (item.FieldType == typeof(float))
                    {
                        item.SetValue(Instance, float.Parse(data.Value.ToString()));

                    }
                    else if (item.FieldType == typeof(int))
                    {
                        item.SetValue(Instance, int.Parse(data.Value.ToString()));

                    }
                    else if (item.FieldType == typeof(bool))
                    {
                        if (data.Value is bool dataValue)
                        {
                            item.SetValue(Instance, dataValue);
                        }
                        else
                        {
                            item.SetValue(Instance, int.Parse(data.Value.ToString()) == 1);
                        }
                    }
                    else if (item.FieldType == typeof(int[]))
                    {
                        item.SetValue(Instance, GetIntArray(data.Value.ToString()));

                    }
                    else if (item.FieldType == typeof(long))
                    {
                        item.SetValue(Instance, long.Parse(data.Value.ToString()));

                    }
                    else if (item.FieldType.IsClass)
                    {
                        string json = data.Value.ToString();

                        var obj = item.GetValue(this);
                        JsonUtility.FromJsonOverwrite(json, obj);
                    }
#if DEBUG_ANALYTIC && DEBUG_ANALYTIC_INTERNAL
                        else
                        {
                            LogWarning($"[MergePreKeys] cannot process type {item.FieldType}");
                        }
#endif

#if DEBUG_ANALYTIC && DEBUG_ANALYTIC_INTERNAL
                        if (oldValue != item.GetValue(Instance).ToString())
                        {
                            LogWarning(
                                $"{pre}{data.Key}: Remote Value Updated: {oldValue}-->{item.GetValue(Instance)}");
                        }
#endif
                }
#if DEBUG_ANALYTIC && DEBUG_ANALYTIC_INTERNAL
                    else
                    {
                        LogWarning($"Key {pre + data.Key} from Firebase not found in class RemoteConfig.cs!");
                    }
#endif
            }
            catch (Exception ex)
            {
#if DEBUG_ANALYTIC && DEBUG_ANALYTIC_INTERNAL
                    LogWarning($"Invalid key: {pre}{data.Key}:{data.Value}-{ex.Message}");
#endif
            }
        }
    }

    public static int[] GetIntArray(string value, char separator = ',', int[] def = null)
    {
        try
        {
            if (string.IsNullOrEmpty(value))
            {
                return def;
            }

            string[] str = value.Split(separator);
            int[] result = new int[str.Length];

            for (int i = 0; i < str.Length; i++)
            {
                result[i] = int.Parse(str[i]);
            }

            return result;
        }
        catch (Exception ex)
        {
#if DEBUG_ANALYTIC && DEBUG_ANALYTIC_INTERNAL
                LogError($"ex: {value}--{ex}");
#endif
            return def;
        }
    }

    public static float[] GetFloatArray(string value, char separator = ',', float[] def = null)
    {
        try
        {
            if (string.IsNullOrEmpty(value))
            {
                return def;
            }

            string[] str = value.Split(separator);
            float[] result = new float[str.Length];

            for (int i = 0; i < str.Length; i++)
            {
                result[i] = float.Parse(str[i]);
            }

            return result;
        }
        catch (Exception ex)
        {
#if DEBUG_ANALYTIC && DEBUG_ANALYTIC_INTERNAL
                LogError($"ex: {ex}");
#endif
            return def;
        }
    }

    //Slow function - Run one time
    public static string[] GetStringArray(string value)
    {
#if FIREBASE_SDK
        Dictionary<string, System.Object> jsonDict = (Dictionary<string, System.Object>)Json.Deserialize(value);
        string[] result = new string[jsonDict.Count];
        int i = 0;
        foreach (KeyValuePair<string, System.Object> data in jsonDict)
        {
            result[i] = data.Value.ToString();
            i++;
        }
        return result;
#else
        return new string[]{};
#endif
    }

    // Get Single T value by key
    public static T GetValueByKey<T>(string key)
    {
        var item = typeof(RemoteConfig).GetField(key);
        return (T)item.GetValue(Instance);
    }

    // Parse all parameters
    public static string ExportToString()
    {
        //string str = String.Empty;
        StringBuilder sb = new StringBuilder();
        var list = Instance.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);
        foreach (var item in list)
        {
            if (item.FieldType == typeof(string) || item.FieldType == typeof(float) || item.FieldType == typeof(int) || item.FieldType == typeof(bool))
            {

                string value = item.GetValue(Instance).ToString();
                if (value.Length > 300)
                {
                    sb.Append($"{item.Name}: <color='#FF0000'>{value.Substring(0, 300)}...</color>\n");
                }
                else
                {
                    sb.Append($"{item.Name}: <color='#FF0000'>{value}</color>\n");
                }
            }
        }
        return sb.ToString();
    }

    public List<FieldInfo> GetAllField()
    {
        List<FieldInfo> fieldInfos = new List<FieldInfo>();

        FieldInfo[] list = GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);
        foreach (FieldInfo item in list)
        {
            if (item.FieldType == typeof(string) ||
                item.FieldType == typeof(float) ||
                item.FieldType == typeof(int) ||
                item.FieldType == typeof(bool) ||
                item.FieldType == typeof(int[]) ||
                item.FieldType == typeof(long))
            {
                if (item.Name == "EnableRemoteSync")
                {
                    fieldInfos.Insert(0, item);

                }
                else
                {
                    fieldInfos.Add(item);
                }
            }
        }

        return fieldInfos;
    }

    #region Utils Functions

#if DEBUG_ANALYTIC
        static void Log(string content)
        {
            LogUtils.Log(LogTag, content);
        }

        static void LogError(string content)
        {
            LogUtils.LogError(LogTag, content);
        }

        static void LogWarning(string content)
        {
            LogUtils.LogWarning(LogTag, content);
        }
#endif

    #endregion
}
