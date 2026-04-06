using UnityEngine;
using System.Collections;

namespace Misc.Services
{
    public class MusicManager : MonoBehaviour
    {
        [SerializeField] private AudioClip _musicClip;
        [SerializeField] private float _fadeDuration = 0.5f;

        private AudioSource _audioSource;
        private Coroutine _fadeCoroutine;

        private static MusicManager _instance;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.clip = _musicClip;
            _audioSource.loop = true;
            _audioSource.playOnAwake = false;

            UpdateMusicState();
        }

        private void OnEnable()
        {
            View.UI.Menu.OptionsScreen.OnSoundStateChanged += UpdateMusicState;
        }

        private void OnDisable()
        {
            View.UI.Menu.OptionsScreen.OnSoundStateChanged -= UpdateMusicState;
        }

        private void UpdateMusicState()
        {
            bool isSoundOn = PlayerPrefs.GetInt(PlayerPrefsKeys.SoundOn, 1) == 1;

            if (_fadeCoroutine != null)
                StopCoroutine(_fadeCoroutine);

            _fadeCoroutine = StartCoroutine(isSoundOn ? FadeIn() : FadeOut());
        }

        private IEnumerator FadeIn()
        {
            if (!_audioSource.isPlaying)
            {
                _audioSource.volume = 0f;
                _audioSource.Play();
            }

            float startVolume = _audioSource.volume;
            float time = 0f;

            while (time < _fadeDuration)
            {
                _audioSource.volume = Mathf.Lerp(startVolume, 1f, time / _fadeDuration);
                time += Time.unscaledDeltaTime;
                yield return null;
            }

            _audioSource.volume = 1f;
        }

        private IEnumerator FadeOut()
        {
            float startVolume = _audioSource.volume;
            float time = 0f;

            while (time < _fadeDuration)
            {
                _audioSource.volume = Mathf.Lerp(startVolume, 0f, time / _fadeDuration);
                time += Time.unscaledDeltaTime;
                yield return null;
            }

            _audioSource.volume = 0f;
            _audioSource.Stop();
        }
    }
}