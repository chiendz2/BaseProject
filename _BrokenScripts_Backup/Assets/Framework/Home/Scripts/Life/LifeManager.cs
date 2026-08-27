using Inwave;
using System;
using UnityEngine;

namespace GameBase
{
    public class LifeManager : MonoBehaviour
    {
        public static LifeManager Instance;

        private LifeConfig _config;
        private double _lastTimeRegenLife;
        private bool _isLoaded;

        private double LastSystemTimeRegenLife
        {
            get => double.Parse(PlayerPrefs.GetString("lstre", "0"));
            set
            {
                _lastTimeRegenLife = value;
                PlayerPrefs.SetString("lstre", value.ToString());
            }
        }

        private DateTime LastDateTimeRegenLife
        {
            get => DateTime.Parse(PlayerPrefs.GetString("ldtre", KeyPrefs.BeginTime));
            set => PlayerPrefs.SetString("ldtre", value.ToString());
        }

        public double LastTimeRegenLife => _lastTimeRegenLife;

        private void Awake()
        {
            Instance = this;

            GameEvents.OnRemoteConfigLoaded += OnRemoteConfigLoaded;
            GameEvents.OnLifeUpdated += OnLifeUpdated;
            GameEvents.OnGameInterrupted += OnGameInterrupted;
            GameEvents.OnGameFinish += OnGameFinish;
        }

        private void OnDestroy()
        {
            GameEvents.OnRemoteConfigLoaded -= OnRemoteConfigLoaded;
            GameEvents.OnLifeUpdated -= OnLifeUpdated;
            GameEvents.OnGameInterrupted -= OnGameInterrupted;
            GameEvents.OnGameFinish -= OnGameFinish;
        }

        private void Update()
        {
            if (_isLoaded && GamePrefs.Life < _config.MaxLife
                && SystemTime.Seconds - _lastTimeRegenLife > _config.CooldownSecs)
            {
                AddLife(1, Location.Home, ParameterValue.regen);
                UpdateLastTimeRegen(0);
            }
        }

        private void OnRemoteConfigLoaded()
        {
            _isLoaded = true;
            _lastTimeRegenLife = LastSystemTimeRegenLife;
            _config = RemoteConfig.Instance.LifeConfig;

            if (_lastTimeRegenLife == 0)
            {
                AddLife(_config.MaxLife, Location.Home, ParameterValue.regen);
                UpdateLastTimeRegen(0);
            }
            else
            {
                double elapsedSecs = SystemTime.Seconds - _lastTimeRegenLife;
                if (elapsedSecs > 0)
                {
                    UpdateByElapsedSecs(elapsedSecs);
                }
                else
                {
                    elapsedSecs = (DateTime.Now - LastDateTimeRegenLife).TotalSeconds;
                    if (elapsedSecs > 0)
                    {
                        UpdateByElapsedSecs(elapsedSecs);
                    }
                    else
                    {
                        UpdateLastTimeRegen(0);
                    }
                }
            }
        }

        private void UpdateByElapsedSecs(double elapsedSecs)
        {
            int added = Mathf.Min((int)(elapsedSecs / _config.CooldownSecs), _config.MaxLife - GamePrefs.Life);
            AddLife(added, Location.Home, ParameterValue.regen);

            var offsetSeconds = elapsedSecs % _config.CooldownSecs;
            UpdateLastTimeRegen(-offsetSeconds);
        }

        private void UpdateLastTimeRegen(double offsetSeconds)
        {
            LastSystemTimeRegenLife = SystemTime.Seconds + offsetSeconds;
            LastDateTimeRegenLife = DateTime.Now.AddSeconds(offsetSeconds);
        }

        private void OnLifeUpdated(int current, int changed, bool vfx)
        {
            if (changed < 0 && current == _config.MaxLife - 1) UpdateLastTimeRegen(0);
        }

        private void OnGameInterrupted()
        {
            AddLife(- _config.PlayCost, Location.Game, GlobalValues.FailManner);
        }

        private void OnGameFinish(bool isWin)
        {
            if (!isWin)
            {
                AddLife(- _config.PlayCost, Location.Game, GlobalValues.FailManner);
            }
        }

        public void AddLife(int added, string location, string manner, bool playVfx = false)
        {
            if (GamePrefs.IsUnlimitedLife) return;

            if (added > 0)
            {
                var current = GamePrefs.Life;
                if (current < _config.MaxLife)
                {
                    var newLife = Mathf.Min(current + added, _config.MaxLife);
                    added = newLife - current;
                    GamePrefs.Life = newLife;
                    GameEvents.OnLifeUpdated?.Invoke(GamePrefs.Life, added, playVfx);
                    AnalyticsResouces.InCome(location, manner, ResourceType.Life, ParameterValue.life, added);
                }
            }
            else if (GamePrefs.Life > 0)
            {
                GamePrefs.Life += added;
                GameEvents.OnLifeUpdated?.Invoke(GamePrefs.Life, added, false);
                AnalyticsResouces.OutCome(location, manner, ResourceType.Life, -added);
            }
        }

        public void ClaimUnlimitedLife(int value, string location, string manner)
        {
            var current = GamePrefs.Life;
            if (current < _config.MaxLife)
            {
                var added = _config.MaxLife - current;
                AddLife(added, location, manner);
            }
            GamePrefs.TimeUnlimitedLife = Math.Max(GamePrefs.TimeUnlimitedLife, SystemTime.Seconds) + value;
        }
    }
}