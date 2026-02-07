using UnityEngine;
using DG.Tweening;

public class ScreenShake : MonoBehaviour
{
    private static ScreenShake instance;
    private Transform cam;
    private Vector3 basePos;

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
        }
        if (cam != null) basePos = cam.localPosition;
    }

    public static void Shake(float duration = 0.12f, float strength = 6f, int vibrato = 18)
    {
        if (instance == null)
        {
            var go = new GameObject("ScreenShake");
            instance = go.AddComponent<ScreenShake>();
        }
        instance.DoShake(duration, strength, vibrato);
    }

    void DoShake(float duration, float strength, int vibrato)
    {
        CacheCamera();
        if (cam == null) return;

        cam.DOKill();
        cam.localPosition = basePos;
        cam.DOShakePosition(duration, strength, vibrato, 90f, false, true)
            .OnComplete(() => cam.localPosition = basePos);
    }
}
