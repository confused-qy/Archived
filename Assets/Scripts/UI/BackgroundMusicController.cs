using System.Collections;
using UnityEngine;

namespace EmployeeHandbook.UI
{
    [RequireComponent(typeof(AudioSource))]
    public class BackgroundMusicController : MonoBehaviour
    {
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip musicClip;
        [SerializeField] private bool playOnStart = true;
        [SerializeField] private bool loop = true;
        [SerializeField] private float volume = 0.5f;
        [SerializeField] private float fadeInDuration = 0.8f;
        [SerializeField] private float defaultFadeOutDuration = 1f;

        private Coroutine fadeRoutine;

        private void Awake()
        {
            SetupAudioSource();
        }

        private void Start()
        {
            if (playOnStart)
                Play();
        }

        public void Play()
        {
            SetupAudioSource();

            if (musicClip != null)
                audioSource.clip = musicClip;

            if (audioSource.clip == null)
                return;

            audioSource.loop = loop;
            audioSource.volume = fadeInDuration > 0f ? 0f : volume;

            if (!audioSource.isPlaying)
                audioSource.Play();

            StartFade(audioSource.volume, volume, fadeInDuration, false);
        }

        public void Stop()
        {
            StopWithFade(defaultFadeOutDuration);
        }

        public void StopWithFade(float duration)
        {
            SetupAudioSource();

            if (!audioSource.isPlaying)
                return;

            StartFade(audioSource.volume, 0f, duration, true);
        }

        private void SetupAudioSource()
        {
            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();

            audioSource.playOnAwake = false;
            audioSource.loop = loop;
        }

        private void StartFade(float fromVolume, float toVolume, float duration, bool stopWhenDone)
        {
            if (fadeRoutine != null)
                StopCoroutine(fadeRoutine);

            fadeRoutine = StartCoroutine(FadeRoutine(fromVolume, toVolume, duration, stopWhenDone));
        }

        private IEnumerator FadeRoutine(float fromVolume, float toVolume, float duration, bool stopWhenDone)
        {
            if (duration <= 0f)
            {
                audioSource.volume = toVolume;
                if (stopWhenDone)
                    audioSource.Stop();

                fadeRoutine = null;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                audioSource.volume = Mathf.Lerp(fromVolume, toVolume, t);
                yield return null;
            }

            audioSource.volume = toVolume;
            if (stopWhenDone)
                audioSource.Stop();

            fadeRoutine = null;
        }
    }
}
