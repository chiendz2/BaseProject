using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

public enum SoundType
{
    bgm,
    click,
    new_popup,
    win,
    lose,
    merge,
    clearance,
    shuffle,
    unlock,
    Cook_LiftItem_Pop,
    Cook_LiftItem_Pop_2,
    Cook_LiftItem_Pop_3,
    Cook_LiftItem_Pop_4,
    Cook_PlaceItem_Sizzle,
    Cat_Step_Order,
    Order_Lose,
    Order_Lose_2,
    Alert_TimeOut,
    master_chef,
    smokin_hot,
    so_juicy,
    tasty,
    yummy,
    currency_pick_up,
    frozen_skew,
    fly_star,
    fly_piggycoin,
    clear_set,
    multiple_coin,
    more_time,
    win_streak,
    unlock_new_area,
    show_popup,
    hard_tag,
    order_complete,
}

[Serializable]
public class SoundVolume
{
    public SoundType Sound;
    public float Volume;
}

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;

    [SerializeField] private AudioSource _soundSource;
    [SerializeField] private AudioSource _musicSource;
    [SerializeField] private SoundVolume[] _volumes;

    private Dictionary<SoundType, AudioClip> _sfxes = new();
    private Dictionary<SoundType, float> _sfxTimes = new();
    private Dictionary<SoundType, float> _sfxVolumes = new();
    private List<AudioClip> _sfxSorts = new();

    private SoundType[] _selectItems = new SoundType[]
    {
        SoundType.Cook_LiftItem_Pop_4
    };

    private SoundType[] _orderLoses = new SoundType[]
    {
        SoundType.Order_Lose,
        SoundType.Order_Lose_2,
    };

    private void Awake()
    {
        Instance = this;

        GameEvents.OnMusicStateChanged += OnMusicStateChanged;
    }

    private void OnDestroy()
    {
        GameEvents.OnMusicStateChanged -= OnMusicStateChanged;
    }

    private void OnMusicStateChanged(bool value)
    {
        if (value) ResumeMusic();
        else PauseMusic();
    }

    private IEnumerator Start()
    {
        var handle = Addressables.LoadAssetsAsync<AudioClip>("Sounds", null);
        yield return handle;
        foreach (var audioClip in handle.Result)
        {
            if (audioClip.name.IndexOf("Combo") == 0)
            {
                _sfxSorts.Add(audioClip);
            }
            else if (Enum.TryParse(audioClip.name, out SoundType soundType))
            {
                _sfxes.Add(soundType, audioClip);
                _sfxTimes.Add(soundType, 0f);
            }
        }
        for (int i = 0; i < _volumes.Length; i++)
        {
            _sfxVolumes.Add(_volumes[i].Sound, _volumes[i].Volume);
        }

        _musicSource.clip = _sfxes[SoundType.bgm];
        _sfxSorts.Sort((a, b) => string.Compare(a.name, b.name));

        PlayMusic();
        GlobalValues.SFXReady = true;
    }

    public void PlayOnShot(SoundType type)
    {
        if (!_sfxes.ContainsKey(type)) return;
        if (!GlobalValues.SoundEnable) return;
        float currentTime = Time.unscaledTime;
        if (currentTime - _sfxTimes[type] < .1f)
        {
            return;
        }

        _sfxTimes[type] = currentTime;
        var volume = _sfxVolumes.ContainsKey(type) ? _sfxVolumes[type] : 1f;
        _soundSource.PlayOneShot(_sfxes[type], volume);

    }

    public void PlayCombo(AudioClip clip)
    {
        if (!GlobalValues.SoundEnable) return;
        _soundSource.PlayOneShot(clip);
    }

    public void PlayMusic()
    {
        _musicSource.DOKill();
        _musicSource.volume = GlobalValues.MusicEnable ? 1f : 0f;
        _musicSource.Play();
    }

    public void PauseMusic()
    {
        _musicSource.DOKill();
        _musicSource.DOFade(0f, .5f).SetUpdate(true);
    }

    public void ResumeMusic()
    {
        _musicSource.DOKill();
        _musicSource.DOFade(GlobalValues.MusicEnable ? 1f : 0f, .5f).SetUpdate(true);
    }

    public void SelectItem()
    {
        var i = UnityEngine.Random.Range(0, _selectItems.Length);
        PlayOnShot(_selectItems[i]);
    }

    public void OrderLose()
    {
        var i = UnityEngine.Random.Range(0, _orderLoses.Length);
        PlayOnShot(_orderLoses[i]);
    }

    public void Sort(int combo)
    {
        if (combo == 0) return;
        if (combo > 0 && combo <= 10)
        {
            PlayCombo(_sfxSorts[combo - 1]);
        }
        else
        {
            PlayCombo(_sfxSorts[_sfxSorts.Count - 1]);
        }
    }
}