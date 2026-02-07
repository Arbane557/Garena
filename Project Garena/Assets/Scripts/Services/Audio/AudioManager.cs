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

        private sealed class AudioEntry
        {
            public AudioClip clip;
            public float volume;
            public bool loop;
        }

        private readonly Dictionary<string, AudioEntry> sfx = new Dictionary<string, AudioEntry>();
        private readonly Dictionary<string, AudioEntry> bgm = new Dictionary<string, AudioEntry>();
        private readonly List<AudioSource> sfxSources = new List<AudioSource>();
        private AudioSource bgmSource;
        private string currentBgmId;
        private float masterVolume = 1f;
        private float sfxVolume = 1f;
        private float bgmVolume = 1f;

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
                if (clip == null) continue;
                AddEntry(sfx, clip.id, clip.clip, clip.volume, clip.loop, overwrite: true);
            }

            foreach (var clip in Resources.LoadAll<AudioClipSO>(bgmResourcesPath))
            {
                if (clip == null) continue;
                AddEntry(bgm, clip.id, clip.clip, clip.volume, clip.loop, overwrite: true);
            }

            foreach (var clip in Resources.LoadAll<AudioClip>(sfxResourcesPath))
            {
                if (clip == null) continue;
                AddEntry(sfx, clip.name, clip, 1f, loop: false, overwrite: false);
            }

            foreach (var clip in Resources.LoadAll<AudioClip>(bgmResourcesPath))
            {
                if (clip == null) continue;
                AddEntry(bgm, clip.name, clip, 1f, loop: true, overwrite: false);
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
            src.volume = data.volume * sfxVolume * masterVolume;
            src.loop = data.loop;
            src.Play();
        }

        public void PlayBgm(string id)
        {
            if (!bgm.TryGetValue(id, out var data) || data.clip == null) return;
            if (currentBgmId == id && bgmSource.isPlaying) return;
            currentBgmId = id;
            bgmSource.clip = data.clip;
            bgmSource.volume = data.volume * bgmVolume * masterVolume;
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

        public void SetMasterVolume(float value)
        {
            masterVolume = Mathf.Clamp01(value);
            ApplyVolumes();
        }

        public void SetSfxVolume(float value)
        {
            sfxVolume = Mathf.Clamp01(value);
            ApplyVolumes();
        }

        public void SetBgmVolume(float value)
        {
            bgmVolume = Mathf.Clamp01(value);
            ApplyVolumes();
        }

        public void ApplyVolumes()
        {
            if (bgmSource != null && bgm.TryGetValue(currentBgmId ?? "", out var data) && data != null)
            {
                bgmSource.volume = data.volume * bgmVolume * masterVolume;
            }
        }

        private void AddEntry(
            Dictionary<string, AudioEntry> map,
            string id,
            AudioClip clip,
            float volume,
            bool loop,
            bool overwrite)
        {
            if (clip == null) return;
            if (string.IsNullOrWhiteSpace(id)) id = clip.name;
            if (string.IsNullOrWhiteSpace(id)) return;
            if (!overwrite && map.ContainsKey(id)) return;

            map[id] = new AudioEntry
            {
                clip = clip,
                volume = Mathf.Clamp01(volume),
                loop = loop
            };
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
