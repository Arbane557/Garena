using UnityEngine;

namespace Template.Audio
{
    [CreateAssetMenu(menuName = "Template/Audio/Audio Clip", fileName = "AudioClip")]
    public class AudioClipSO : ScriptableObject
    {
        public string id = "clip-id";
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 1f;
        public bool loop;
    }
}
