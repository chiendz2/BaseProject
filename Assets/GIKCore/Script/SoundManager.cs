using UnityEngine;

namespace GIKCore
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-95)]
    public class SoundManager : MonoBehaviour
    {
        private const string LogTag = "[SoundManager]";

        public static SoundManager Instance { get; private set; }

        public static bool IsSoundOn => UserDataManager.GetSoundOn();

        public static bool IsMusicOn => UserDataManager.GetMusicOn();

        [Header("Sources")]
        [Tooltip("One-shot sound effects play through this source.")]
        [SerializeField] private AudioSource _sfxSource;

        [Tooltip("Background music plays through this source.")]
        [SerializeField] private AudioSource _musicSource;

        [Header("Volume")]
        [Range(0f, 1f)]
        [Tooltip("Master multiplier applied on top of the per-call volume of every sound effect.")]
        [SerializeField] private float _sfxVolume = 1f;

        [Range(0f, 1f)]
        [Tooltip("Master volume for background music.")]
        [SerializeField] private float _musicVolume = 1f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (_sfxSource == null || _musicSource == null)
                Debug.LogError(LogTag + " Sfx or music AudioSource is not assigned.");

            if (_musicSource != null)
                _musicSource.volume = _musicVolume;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public static void PlaySfx(AudioClip clip, float volumeScale = 1f)
        {
            if (Instance == null)
            {
                Debug.LogError(LogTag + " No SoundManager in the scene.");
                return;
            }

            Instance.DoPlaySfx(clip, volumeScale);
        }

        public static void PlayMusic(AudioClip clip, bool loop = true)
        {
            if (Instance == null)
            {
                Debug.LogError(LogTag + " No SoundManager in the scene.");
                return;
            }

            Instance.DoPlayMusic(clip, loop);
        }

        public static void StopMusic()
        {
            if (Instance == null)
                return;

            Instance.DoStopMusic();
        }

        public static void SetSoundOn(bool value)
        {
            UserDataManager.SetSoundOn(value);

            if (Instance != null)
                Instance.DoApplySoundOn(value);
        }

        public static void SetMusicOn(bool value)
        {
            UserDataManager.SetMusicOn(value);

            if (Instance != null)
                Instance.DoApplyMusicOn(value);
        }

        private void DoPlaySfx(AudioClip clip, float volumeScale)
        {
            if (clip == null || _sfxSource == null)
                return;

            if (!UserDataManager.GetSoundOn())
                return;

            _sfxSource.PlayOneShot(clip, Mathf.Clamp01(volumeScale) * _sfxVolume);
        }

        private void DoPlayMusic(AudioClip clip, bool loop)
        {
            if (clip == null || _musicSource == null)
                return;

            if (_musicSource.clip == clip && _musicSource.isPlaying)
                return;

            _musicSource.clip = clip;
            _musicSource.loop = loop;
            _musicSource.volume = _musicVolume;

            if (UserDataManager.GetMusicOn())
                _musicSource.Play();
        }

        private void DoStopMusic()
        {
            if (_musicSource == null)
                return;

            _musicSource.Stop();
            _musicSource.clip = null;
        }

        private void DoApplySoundOn(bool value)
        {
            if (value || _sfxSource == null)
                return;

            _sfxSource.Stop();
        }

        private void DoApplyMusicOn(bool value)
        {
            if (_musicSource == null)
                return;

            if (!value)
            {
                _musicSource.Pause();
                return;
            }

            if (_musicSource.clip != null && !_musicSource.isPlaying)
                _musicSource.Play();
        }
    }
}
