using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class FrameAnimator : MonoBehaviour
{
    public Sprite[] frames;
    public float fps = 10f;
    public bool loop = true;

    private Image img;
    private float timer;
    private int index;
    private bool playing;

    void Awake()
    {
        img = GetComponent<Image>();
    }

    void Update()
    {
        if (!playing || frames == null || frames.Length == 0 || fps <= 0f) return;
        timer += Time.deltaTime;
        float frameTime = 1f / fps;
        while (timer >= frameTime)
        {
            timer -= frameTime;
            index++;
            if (index >= frames.Length)
            {
                if (loop) index = 0;
                else { index = frames.Length - 1; playing = false; }
            }
            img.sprite = frames[index];
        }
    }

    public void Play(Sprite[] newFrames, float newFps)
    {
        frames = newFrames;
        fps = Mathf.Max(1f, newFps);
        index = 0;
        timer = 0f;
        playing = frames != null && frames.Length > 0;
        if (playing) img.sprite = frames[0];
    }

    public void Stop()
    {
        playing = false;
        frames = null;
        timer = 0f;
        index = 0;
    }
}

