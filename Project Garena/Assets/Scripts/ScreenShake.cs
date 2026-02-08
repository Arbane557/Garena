using UnityEngine;
using DG.Tweening;
using System.Linq;
using MoreMountains.Feedbacks;

public class ScreenShake : MonoBehaviour
{
    private static ScreenShake instance;
    private Transform cam;
    private Vector3 basePos;
    public MMF_Player feedbackPlayer;
    private Transform uiTarget;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public static void Shake(float duration = 0.12f, float strength = 6f, int vibrato = 18)
    {
        if (instance.feedbackPlayer != null)
        {
            instance.feedbackPlayer.PlayFeedbacks();
            return;
        }
    }
}
