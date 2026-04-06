using UnityEngine;

namespace Misc.Services
{
    public class SoundManager : MonoBehaviour
    {
        public static SoundManager Instance { get; private set; }

        private AudioSource _audioSource;

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.playOnAwake = false;
            _audioSource.spatialBlend = 0.0f;
        }

        public void PlayClick(AudioClip clip)
        {
            if (clip != null)
                _audioSource.PlayOneShot(clip);
        }
    }
}