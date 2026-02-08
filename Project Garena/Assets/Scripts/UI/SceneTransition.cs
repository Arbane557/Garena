using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneTransition : MonoBehaviour
{
    private static SceneTransition instance;

    [Header("Overlay")]
    public Color overlayColor = Color.black;
    public Sprite[] ditherFrames;
    public float ditherFps = 24f;
    public float fadeOutTime = 0.35f;
    public float fadeInTime = 0.35f;
    public float maxAlpha = 1f;

    private Image overlayImage;
    private Coroutine running;
    private CanvasGroup canvasGroup;

    public static void TransitionTo(string sceneName)
    {
        if (instance == null)
        {
            var go = new GameObject("SceneTransition");
            instance = go.AddComponent<SceneTransition>();
        }
        instance.StartTransition(sceneName);
    }

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
        EnsureOverlay();
    }

    void EnsureOverlay()
    {
        if (overlayImage != null) return;

        var canvasGo = new GameObject("TransitionCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999;

        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(800, 600);

        var imgGo = new GameObject("TransitionOverlay", typeof(RectTransform), typeof(Image));
        imgGo.transform.SetParent(canvasGo.transform, false);
        overlayImage = imgGo.GetComponent<Image>();
        overlayImage.raycastTarget = true;
        overlayImage.color = overlayColor;

        var rt = overlayImage.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        canvasGroup = overlayImage.gameObject.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
    }

    void StartTransition(string sceneName)
    {
        EnsureOverlay();
        if (running != null) StopCoroutine(running);
        running = StartCoroutine(CoTransition(sceneName));
    }

    IEnumerator CoTransition(string sceneName)
    {
        yield return AnimateProgress(0f, 1f, fadeOutTime);

        var op = SceneManager.LoadSceneAsync(sceneName);
        while (!op.isDone) yield return null;

        yield return AnimateProgress(1f, 0f, fadeInTime);
        running = null;
    }

    IEnumerator AnimateProgress(float from, float to, float duration)
    {
        duration = Mathf.Max(0.001f, duration);
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float u = Mathf.Clamp01(t / duration);
            u = u * u * (3f - 2f * u);
            SetProgress(Mathf.Lerp(from, to, u));
            yield return null;
        }
        SetProgress(to);
    }

    void SetProgress(float v)
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = Mathf.Clamp01(v) * maxAlpha;
        }
        if (ditherFrames != null && ditherFrames.Length > 0 && overlayImage != null)
        {
            int idx = Mathf.Clamp(Mathf.RoundToInt(v * (ditherFrames.Length - 1)), 0, ditherFrames.Length - 1);
            overlayImage.sprite = ditherFrames[idx];
            overlayImage.SetNativeSize();
            var rt = overlayImage.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
