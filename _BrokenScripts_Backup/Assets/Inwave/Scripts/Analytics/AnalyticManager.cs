
using Firebase.Analytics;
using Facebook.Unity;
using FoodMaster;
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class AnalyticData
{
    public int LastLevel;
    public int LevelMax;
    public int TotalHeart;
    public int TotalBackGame;
    public int ValueBackGame;
    public int SessionBack;
    public int TotalGoldOutCome;
    public int TotalGoldInCome;
    public int IapCount;
    public int IapRev;
    public int TotalWatchedInterAds;
    public int TotalWatchedRewardAds;
    public int Day;
    public int DayLogin;
    public int GameCount;
    public int GameCountByLevel;
    public float PlayTime;
    public float PlayTimeAds;
    public int TotalStar;
    public int TotalVip;
    public int TotalArea;
    public string CurrentArea;

    public int GetPlayTime => (int)PlayTime;

    public void NewData()
    {
        LastLevel = 0;
        LevelMax = 0;
        TotalHeart = 5;
        TotalGoldOutCome = 0;
        TotalGoldInCome = 0;
        IapCount = 0;
        IapRev = 0;
        TotalWatchedInterAds = 0;
        TotalWatchedRewardAds = 0;
        Day = 0;
        DayLogin = 0;
        PlayTime = 0;
        PlayTimeAds = 0;
        GameCount = 0;
        GameCountByLevel = 0;
        TotalStar = 0;
        TotalVip = 0;
        TotalArea = 0;
        CurrentArea = string.Empty;
    }
}

public class AnalyticManager : Singleton<AnalyticManager>
{
    public static Action OnIapCount;
    public static Action<int> OnTotalWatchedInterAds;
    public static Action<int> OnTotalWatchedRewardAds;
    public static Action<float> OnUpdatePlayTimeAds;

    public AnalyticData Data;
        
    private int _currentGold;
    private int _playtimeCache = 0;
    private int _playtimeAdsCache = 0;
    private int _lastFTUEMins = 0;
    private float _timeStartLevel = 0f;

    private static DateTime startDate;
    private static DateTime today;
    private const string KeySave = "data_analytic";
    private bool _isLoad = false;
    private bool _hasFireNoSpace;

    private int goldCache;
    private int starCache;
    private int piggyBankCache;
    private int ticketCache;
    public float TimeStartLevel
    {
        get => _timeStartLevel;
        set => _timeStartLevel = value;
    }

    public int GoldCache
    {
        get => goldCache;
        set => goldCache = value;
    }

    public int StarCache
    {
        get => starCache;
        set => starCache = value;
    }
    
    public int PiggyBankCache
    {
        get => piggyBankCache;
        set => piggyBankCache = value;
    }

    public int TicketCache
    {
        get => ticketCache;
        set => ticketCache = value;
    }

    protected override void Awake()
    {
        base.Awake();

        InitializeFacebook();
        Init();
        _currentGold = GamePrefs.Gold;

        OnIapCount += UpdateIapCount;
        OnTotalWatchedInterAds += UpdateTotalWatchedInterAds;
        OnTotalWatchedRewardAds += UpdateTotalWatchedRewardAds;
        OnUpdatePlayTimeAds += UpdatePlayTimeAds;
        GameEvents.OnGoldUpdated += OnGoldUpdated;

        GameEvents.OnGameStart += OnGameStart;
        GameEvents.OnGameFinish += OnGameFinish;
        GameEvents.OnGameInterrupted += OnGameInterrupted;
        GameEvents.OnSortCountUpdated += OnSortCountUpdated;
        GameEvents.OnLevelShelfUnlocked += OnLevelShelfUnlocked;
        GameEvents.OnHintNoSpace += OnHintNoSpace;
    }

    protected void OnDestroy()
    {
        OnIapCount -= UpdateIapCount;
        OnTotalWatchedInterAds -= UpdateTotalWatchedInterAds;
        OnTotalWatchedRewardAds -= UpdateTotalWatchedRewardAds;
        OnUpdatePlayTimeAds -= UpdatePlayTimeAds;
        GameEvents.OnGoldUpdated -= OnGoldUpdated;

        GameEvents.OnGameStart -= OnGameStart;
        GameEvents.OnGameFinish -= OnGameFinish;
        GameEvents.OnGameInterrupted -= OnGameInterrupted;
        GameEvents.OnSortCountUpdated -= OnSortCountUpdated;
        GameEvents.OnLevelShelfUnlocked -= OnLevelShelfUnlocked;
        GameEvents.OnHintNoSpace -= OnHintNoSpace;
    }

    private void Start()
    {
        LogUserProperties();
        AnalyticsFTUE.PlayGameDay(Data.DayLogin);
        AnalyticsFTUE.PlayGameSession(Data.SessionBack);

        _isLoad = true;

        goldCache = GamePrefs.Gold;
        starCache = GamePrefs.Star;
        piggyBankCache = GamePrefs.PiggyBankProgress;
        ticketCache = GamePrefs.TicketRacing;
    }

    private void InitializeFacebook()
    {
        try
        {
            if (FB.IsInitialized)
            {
                OnFacebookInitialized();
                return;
            }

            FB.Init(OnFacebookInitialized);
        }
        catch (Exception exception)
        {
            Debug.LogError($"[Analytics][Facebook] Initialization failed: {exception}");
        }
    }

    private void OnFacebookInitialized()
    {
        if (!FB.IsInitialized)
        {
            Debug.LogError("[Analytics][Facebook] SDK initialization did not complete.");
            return;
        }

        FB.ActivateApp();
        AnalyticHelper.SetFacebookReady();
    }

    private void Init()
    {
        if (PlayerPrefs.HasKey(KeySave))
        {
            var jsonData = PlayerPrefs.GetString(KeySave);

            Data = JsonUtility.FromJson<AnalyticData>(jsonData);
        }
        else
        {
            Data = new AnalyticData();
            Data.NewData();
        }

        SetStartDate();
        GetDay();

        Data.SessionBack++;
    }

    private void OnGoldUpdated(int value)
    {
        int changedAmount = value - _currentGold;
        if (changedAmount > 0)
        {
            Data.TotalGoldInCome += changedAmount;
            UpdateUserProperty(UserProperty.total_cash_income, Data.TotalGoldInCome);
            AnalyticHelper.LogEvent(EventName.total_cash_income, new Dictionary<string, object>
            {
                { ParameterName.value, changedAmount },
                { ParameterName.total, Data.TotalGoldInCome }
            });
        }
        else if (changedAmount < 0)
        {
            int spentAmount = -changedAmount;
            Data.TotalGoldOutCome += spentAmount;
            UpdateUserProperty(UserProperty.total_cash_outcome, Data.TotalGoldOutCome);
            AnalyticHelper.LogEvent(EventName.total_cash_outcome, new Dictionary<string, object>
            {
                { ParameterName.value, spentAmount },
                { ParameterName.total, Data.TotalGoldOutCome }
            });
        }
        _currentGold = value;
    }

    private void OnLifeUpdated(int value)
    {
        Data.TotalHeart = value;
        UpdateUserProperty(UserProperty.current_heart, Data.TotalHeart);
    }

    private void OnSortCountUpdated()
    {
        _hasFireNoSpace = false;
        if (GlobalValues.CurrentLevelStage.SortCount % 5 == 0)
        {
            AnalyticsProduct.SolvedPuzzle();
        }
    }

    private void OnLevelShelfUnlocked(LevelShelf shelf)
    {
        _hasFireNoSpace = false;
    }

    private void OnHintNoSpace()
    {
        if (!_hasFireNoSpace)
        {
            _hasFireNoSpace = true;
            AnalyticsProduct.LevelNoSpace();
        }
    }

    private void OnGameInterrupted()
    {
        Data.LastLevel = Data.LevelMax;
        UpdateUserProperty(UserProperty.last_level, Data.LastLevel);
        AnalyticsProduct.LevelInterrupted();
    }

    private void OnGameFinish(bool isWin)
    {
        Data.LastLevel = Data.LevelMax;
        UpdateUserProperty(UserProperty.last_level, Data.LastLevel);

        if (isWin)
        {
            AnalyticsProduct.LevelWin();
        }
        else
        {
            AnalyticsProduct.LevelFail();
        }
    }

    private void OnGameStart()
    {
        int currentLevel = GamePrefs.Level;
        if (Data.LevelMax == currentLevel)
        {
            Data.GameCountByLevel++;
        }
        else
        {
            Data.GameCountByLevel = 1;
        }

        Data.LevelMax = currentLevel;
        Data.GameCount++;
        Data.TotalStar = GamePrefs.Star;
        //Data.TotalVip ????
        Data.TotalArea = GamePrefs.UnlockedAreaCount;
        Data.CurrentArea = GamePrefs.CurrentArea;
        UpdateUserProperty(UserProperty.level_max, Data.LevelMax);
        UpdateUserProperty(UserProperty.accumulated_game_count, Data.GameCount);
        UpdateUserProperty(UserProperty.game_count, Data.GameCountByLevel);
        UpdateUserProperty(UserProperty.total_star, Data.TotalStar);
        UpdateUserProperty(UserProperty.total_vip, Data.TotalVip);
        UpdateUserProperty(UserProperty.total_area, Data.TotalArea);
        UpdateUserProperty(UserProperty.current_area, Data.CurrentArea);
        AnalyticsProduct.LevelStart();
    }

    public static void UpdateUserProperty(string userPropertyName, int value)
    {
        FirebaseAnalytics.SetUserProperty(userPropertyName, value.ToString());
    }

    public static void UpdateUserProperty(string userPropertyName, string value)
    {
        FirebaseAnalytics.SetUserProperty(userPropertyName, value);
    }

    private void UpdateIapCount()
    {
        Data.IapCount++;
        UpdateUserProperty(UserProperty.iap_count, Data.IapCount);
    }

    public void UpdateIapRev(int addedValue)
    {
        Data.IapRev += addedValue;
        UpdateUserProperty(UserProperty.iap_rev, Data.IapRev);
    }

    private void UpdateTotalWatchedInterAds(int value)
    {
        Data.TotalWatchedInterAds = value;
        UpdateUserProperty(UserProperty.total_watched_inter_ads, Data.TotalWatchedInterAds);
    }

    private void UpdateTotalWatchedRewardAds(int value)
    {
        Data.TotalWatchedRewardAds = value;
        UpdateUserProperty(UserProperty.total_watched_reward_ads, Data.TotalWatchedRewardAds);
    }

    private void OnSave()
    {
        if (!_isLoad) return;

        string data = JsonUtility.ToJson(Data);

        PlayerPrefs.SetString(KeySave, data);

        PlayerPrefs.Save();

        //Debug.Log("OnSave " + data);
    }

    private void SetStartDate()
    {
        if (PlayerPrefs.HasKey("DateInitialized")) //if we have the start date saved, we'll use that
            startDate = System.Convert.ToDateTime(PlayerPrefs.GetString("DateInitialized"));
        else //otherwise...
        {
            startDate = System.DateTime.Now; //save the start date ->
            PlayerPrefs.SetString("DateInitialized", startDate.ToString());
        }
    }
    private void GetDay()
    {
        Data.ValueBackGame = 0;

        var dayPass = DayPass;
        if (Data.Day != dayPass)
        {
            Data.Day = dayPass;
            Data.DayLogin++;
            Data.TotalBackGame = 0;
        }
    }

    public static string GetDaysPassed()
    {
        today = System.DateTime.Now;

        //days between today and start date -->
        System.TimeSpan elapsed = today.Subtract(startDate);

        double days = elapsed.TotalDays;

        return days.ToString("0");
    }

    public int DayPass => int.Parse(GetDaysPassed());

    private void OnApplicationPause(bool isPause)
    {
        OnSave();
        if (!isPause && _isLoad)
        {
            if (FB.IsInitialized)
                FB.ActivateApp();

            Data.ValueBackGame++;
            Data.TotalBackGame++;
        }
    }

    private void OnApplicationQuit()
    {
        OnSave();
    }

    private void Update()
    {
        Data.PlayTime += Time.deltaTime;
        Data.PlayTimeAds += Time.deltaTime;
        var iPlayTime = (int)Data.PlayTime;
        var iPlayTimeAds = (int)Data.PlayTimeAds;


        if (iPlayTime != _playtimeCache)
        {
            _playtimeCache = iPlayTime;
            UpdateUserProperty(UserProperty.time_in_app_no_ads, _playtimeCache);
        }
        if (iPlayTimeAds != _playtimeAdsCache)
        {
            _playtimeAdsCache = iPlayTimeAds;
            UpdateUserProperty(UserProperty.time_in_app, _playtimeAdsCache);
        }

        var iPlayTimeMins = iPlayTime / 60;
        if (iPlayTimeMins != _lastFTUEMins)
        {
            _lastFTUEMins = iPlayTimeMins;
            switch (iPlayTimeMins)
            {
                case 1:
                case 3:
                case 5:
                case 10:
                case 15:
                case 20:
                case 30:
                    AnalyticsFTUE.LogEvent(string.Format(EventName.FTUE_user_playtime_mins, iPlayTimeMins));
                    break;
            }
        }
    }

    public void UpdatePlayTimeAds(float adsDuration)
    {
        Data.PlayTimeAds += adsDuration;
        _playtimeAdsCache = (int)Data.PlayTimeAds;
        UpdateUserProperty(UserProperty.time_in_app, _playtimeAdsCache);
    }

    public void LogUserProperties()
    {
        UpdateUserProperty(UserProperty.last_level, Data.LastLevel);
        UpdateUserProperty(UserProperty.level_max, Data.LevelMax);
        UpdateUserProperty(UserProperty.total_cash_income, Data.TotalGoldInCome);
        UpdateUserProperty(UserProperty.total_cash_outcome, Data.TotalGoldOutCome);
        UpdateUserProperty(UserProperty.iap_count, Data.IapCount);
        UpdateUserProperty(UserProperty.iap_rev, Data.IapRev);
        UpdateUserProperty(UserProperty.total_watched_inter_ads, Data.TotalWatchedInterAds);
        UpdateUserProperty(UserProperty.total_watched_reward_ads, Data.TotalWatchedRewardAds);
        UpdateUserProperty(UserProperty.day_diff, Data.Day);
        UpdateUserProperty(UserProperty.playtime, Data.GetPlayTime);
        UpdateUserProperty(UserProperty.time_in_app, (int)Data.PlayTimeAds);
        UpdateUserProperty(UserProperty.time_in_app_no_ads, (int)Data.PlayTime);
        UpdateUserProperty(UserProperty.accumulated_game_count, Data.GameCount);
        UpdateUserProperty(UserProperty.game_count, Data.GameCountByLevel);
        UpdateUserProperty(UserProperty.current_heart, Data.TotalHeart);
        UpdateUserProperty(UserProperty.total_star, Data.TotalStar);
        UpdateUserProperty(UserProperty.total_vip, Data.TotalVip);
        UpdateUserProperty(UserProperty.total_area, Data.TotalArea);
        UpdateUserProperty(UserProperty.current_area, Data.CurrentArea);
    }
}
