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
        CacheCamera();
    }

    void CacheCamera()
    {
        if (cam == null)
        {
            cam = Camera.main != null ? Camera.main.transform : null;
            if (cam == null)
            {
                var cameras = FindObjectsOfType<Camera>();
                if (cameras != null && cameras.Length > 0)
                {
                    var active = cameras.FirstOrDefault(c => c != null && c.enabled);
                    cam = (active != null ? active : cameras[0]).transform;
                }
            }
            if (cam == null)
            {
                var canvas = FindObjectOfType<Canvas>();
                uiTarget = canvas != null ? canvas.transform : null;
            }
        }
        if (cam != null) basePos = cam.localPosition;
        else if (uiTarget != null) basePos = uiTarget.localPosition;
    }

    public static void Shake(float duration = 0.12f, float strength = 6f, int vibrato = 18)
    {
        if (instance == null)
        {
            var go = new GameObject("ScreenShake");
            instance = go.AddComponent<ScreenShake>();
        }
        if (instance.feedbackPlayer != null)
        {
            instance.feedbackPlayer.PlayFeedbacks();
            return;
        }
        instance.DoShake(duration, strength, vibrato);
    }

    void DoShake(float duration, float strength, int vibrato)
    {
        CacheCamera();
        if (cam == null && uiTarget == null) return;

        var target = cam != null ? cam : uiTarget;
        target.DOKill();
        target.localPosition = basePos;
        target.DOShakePosition(duration, strength, vibrato, 90f, false, true)
            .SetUpdate(true)
            .OnComplete(() => target.localPosition = basePos);
    }
}
