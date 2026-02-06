using System.Collections.Generic;
using UnityEngine;
using Template.Core;

namespace Template.Audio
{
    public class AudioManager : MonoBehaviour, IGameService
    {
        [Header("Resources Paths")]
        [SerializeField] private string sfxResourcesPath = "Audio/SFX";
        [SerializeField] private string bgmResourcesPath = "Audio/BGM";

        [Header("SFX Pool")]
        [SerializeField] private int initialSfxSources = 8;
        [SerializeField] private bool expandSfxSources = true;

        private readonly Dictionary<string, AudioClipSO> sfx = new Dictionary<string, AudioClipSO>();
        private readonly Dictionary<string, AudioClipSO> bgm = new Dictionary<string, AudioClipSO>();
        private readonly List<AudioSource> sfxSources = new List<AudioSource>();
        private AudioSource bgmSource;
        private string currentBgmId;

        public void Initialize()
        {
            LoadResources();
            EnsureSources();
        }

        public void Dispose()
        {
        }

        private void LoadResources()
        {
            sfx.Clear();
            bgm.Clear();

            foreach (var clip in Resources.LoadAll<AudioClipSO>(sfxResourcesPath))
            {
                if (clip != null && !string.IsNullOrWhiteSpace(clip.id))
                {
                    sfx[clip.id] = clip;
                }
            }

            foreach (var clip in Resources.LoadAll<AudioClipSO>(bgmResourcesPath))
            {
                if (clip != null && !string.IsNullOrWhiteSpace(clip.id))
                {
                    bgm[clip.id] = clip;
                }
            }
        }

        private void EnsureSources()
        {
            if (bgmSource == null)
            {
                var go = new GameObject("BGM_Source");
                go.transform.SetParent(transform, false);
                bgmSource = go.AddComponent<AudioSource>();
                bgmSource.loop = true;
            }

            while (sfxSources.Count < initialSfxSources)
            {
                sfxSources.Add(CreateSfxSource());
            }
        }

        private AudioSource CreateSfxSource()
        {
            var go = new GameObject("SFX_Source");
            go.transform.SetParent(transform, false);
            var src = go.AddComponent<AudioSource>();
            src.loop = false;
            return src;
        }

        public void PlaySfx(string id)
        {
            if (!sfx.TryGetValue(id, out var data) || data.clip == null) return;
            var src = GetAvailableSfxSource();
            src.clip = data.clip;
            src.volume = data.volume;
            src.loop = data.loop;
            src.Play();
        }

        public void PlayBgm(string id)
        {
            if (!bgm.TryGetValue(id, out var data) || data.clip == null) return;
            if (currentBgmId == id && bgmSource.isPlaying) return;
            currentBgmId = id;
            bgmSource.clip = data.clip;
            bgmSource.volume = data.volume;
            bgmSource.loop = data.loop;
            bgmSource.Play();
        }

        public void StopBgm()
        {
            currentBgmId = null;
            if (bgmSource != null) bgmSource.Stop();
        }

        public void PauseBgm()
        {
            if (bgmSource != null) bgmSource.Pause();
        }

        public void ResumeBgm()
        {
            if (bgmSource != null) bgmSource.UnPause();
        }

        private AudioSource GetAvailableSfxSource()
        {
            for (var i = 0; i < sfxSources.Count; i++)
            {
                if (!sfxSources[i].isPlaying)
                {
                    return sfxSources[i];
                }
            }

            if (expandSfxSources)
            {
                var src = CreateSfxSource();
                sfxSources.Add(src);
                return src;
            }

            return sfxSources[0];
        }
    }
}
