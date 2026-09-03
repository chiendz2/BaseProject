using System;
using System.Globalization;
using TMPro;
using UnityEngine;

namespace GIKCore
{
    [DefaultExecutionOrder(-100)]
    public class UserDataManager : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI m_txtTime;
        private const string SaveKey = "GIKCore.UserData";

        private static UserData _data;

        public static UserDataManager Instance { get; private set; }

        private static UserData Data
        {
            get
            {
                if (_data == null)
                    Load();

                return _data;
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            Load();
        }

        private void OnApplicationPause(bool pause)
        {
            if (pause)
                Save();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
                Save();
        }

        private void OnApplicationQuit()
        {
            Save();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public static bool IsLoaded() => _data != null;

        public static int GetCoin() => Data.coin;

        public static void SetCoin(int value) => Data.coin = Mathf.Max(0, value);

        public static int GetCurrentLevel() => Data.currentLevel;

        public static void SetCurrentLevel(int value) => Data.currentLevel = Mathf.Max(1, value);

        public static bool GetSoundOn() => Data.soundOn;

        public static void SetSoundOn(bool value) => Data.soundOn = value;

        public static bool GetMusicOn() => Data.musicOn;

        public static void SetMusicOn(bool value) => Data.musicOn = value;

        public static bool GetVibrationOn() => Data.vibrationOn;

        public static void SetVibrationOn(bool value) => Data.vibrationOn = value;

        public static bool GetNoAds() => Data.noAds;

        public static void SetNoAds(bool value) => Data.noAds = value;

        public static int GetLanguage() => Data.language;

        public static void SetLanguage(int value) => Data.language = value;

        public static int GetDataVersion() => Data.dataVersion;

        public static string GetFirstOpenUtc() => Data.firstOpenUtc;

        public static string GetLastOpenUtc() => Data.lastOpenUtc;

        public static string GetUserPseudoId() => Data.userPseudoId;

        public static void SetUserPseudoId(string value) => Data.userPseudoId = value ?? string.Empty;

        public static string GetRawJson() => JsonUtility.ToJson(Data, true);

        public static void AddCoin(int amount)
        {
            if (amount <= 0)
                return;

            SetCoin(GetCoin() + amount);
        }

        public static bool TrySpendCoin(int amount)
        {
            if (amount <= 0)
                return false;

            if (GetCoin() < amount)
                return false;

            SetCoin(GetCoin() - amount);
            return true;
        }

        public static void Load()
        {
            _data = ReadFromPlayerPrefs();
            StampSession();
        }

        public static void Save()
        {
            if (_data == null)
                return;

            string json;

            try
            {
                json = JsonUtility.ToJson(_data);
            }
            catch (Exception e)
            {
                Debug.LogError("[UserDataManager] Cannot serialize user data: " + e.Message);
                return;
            }

            PlayerPrefs.SetString(SaveKey, json);
            PlayerPrefs.Save();
        }

        public static void ResetAll()
        {
            PlayerPrefs.DeleteKey(SaveKey);
            PlayerPrefs.Save();

            _data = new UserData();
            StampSession();
        }

        private static UserData ReadFromPlayerPrefs()
        {
            if (!PlayerPrefs.HasKey(SaveKey))
                return new UserData();

            var json = PlayerPrefs.GetString(SaveKey, string.Empty);

            if (string.IsNullOrEmpty(json))
                return new UserData();

            UserData parsed;

            try
            {
                parsed = JsonUtility.FromJson<UserData>(json);
            }
            catch (Exception e)
            {
                Debug.LogError("[UserDataManager] Corrupted user data, falling back to defaults: " + e.Message);
                return new UserData();
            }

            if (parsed == null)
                return new UserData();

            return Migrate(parsed);
        }

        private static UserData Migrate(UserData data)
        {
            if (data.dataVersion == UserData.CurrentVersion)
                return data;

            if (data.dataVersion < 1)
                data.currentLevel = Mathf.Max(1, data.currentLevel);

            data.dataVersion = UserData.CurrentVersion;
            return data;
        }

        private static void StampSession()
        {
            var nowUtc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture);

            if (string.IsNullOrEmpty(_data.firstOpenUtc))
                _data.firstOpenUtc = nowUtc;

            _data.lastOpenUtc = nowUtc;
        }
    }
}
