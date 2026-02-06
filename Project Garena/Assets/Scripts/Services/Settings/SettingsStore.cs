using UnityEngine;

namespace Template.Core
{
    public static class SettingsStore
    {
        private const string MasterKey = "settings.master";
        private const string SfxKey = "settings.sfx";
        private const string BgmKey = "settings.bgm";
        private const string BrightnessKey = "settings.brightness";

        public static float MasterVolume
        {
            get => PlayerPrefs.GetFloat(MasterKey, 1f);
            set => PlayerPrefs.SetFloat(MasterKey, Mathf.Clamp01(value));
        }

        public static float SfxVolume
        {
            get => PlayerPrefs.GetFloat(SfxKey, 1f);
            set => PlayerPrefs.SetFloat(SfxKey, Mathf.Clamp01(value));
        }

        public static float BgmVolume
        {
            get => PlayerPrefs.GetFloat(BgmKey, 1f);
            set => PlayerPrefs.SetFloat(BgmKey, Mathf.Clamp01(value));
        }

        public static float Brightness
        {
            get => PlayerPrefs.GetFloat(BrightnessKey, 1f);
            set => PlayerPrefs.SetFloat(BrightnessKey, Mathf.Clamp01(value));
        }

        public static void Save()
        {
            PlayerPrefs.Save();
        }
    }
}
