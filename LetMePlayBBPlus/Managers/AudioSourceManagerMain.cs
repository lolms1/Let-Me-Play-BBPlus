using UnityEngine;

namespace LetMePlayBBPlus
{
    public class AudioSourceManagerMain : MonoBehaviour
    {
        private AudioSource musicSource;

        public static AudioSourceManagerMain Instance { get; private set; }

        private AudioSource GetAudioSource()
        {
            if (musicSource == null)
            {
                GameObject musicObj = new GameObject("AudioSourceManagerMain");
                musicSource = musicObj.AddComponent<AudioSource>();
                musicSource.loop = false;
                musicSource.ignoreListenerPause = true;
                musicSource.playOnAwake = false;
                musicSource.volume = 1f;
            }
            return musicSource;
        }

        public void PlayMusic(SoundObject sound)
        {
            if (sound == null || sound.soundClip == null) return;

            musicSource = GetAudioSource();
            musicSource.clip = sound.soundClip;
            musicSource.Play();
        }

        public void PauseMusic()
        {
            if (musicSource != null && musicSource.isPlaying)
            {
                musicSource.Pause();
            }
        }

        public void ResumeMusic()
        {
            if (musicSource != null && !musicSource.isPlaying && musicSource.time > 0f)
            {
                musicSource.UnPause();
            }
        }

        public void StopMusic()
        {
            if (musicSource != null)
            {
                musicSource.Stop();
            }
        }
    }
}