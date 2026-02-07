using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class DitherTransition : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Image overlayImage;

    [Header("Timing")]
    [SerializeField] private float fadeOutTime = 0.35f;
    [SerializeField] private float fadeInTime = 0.35f;

    [Header("Shader")]
    [SerializeField] private string progressProperty = "_Progress";

    private Material runtimeMat;
    private Coroutine running;

    private void Awake()
    {
        if (overlayImage == null)
        {
            Debug.LogError("DitherTransition: overlayImage not assigned.");
            enabled = false;
            return;
        }

        if (overlayImage.material == null)
        {
            Debug.LogError("DitherTransition: overlayImage has no material.");
            enabled = false;
            return;
        }

        // Important: clone material so we don't edit shared asset
        runtimeMat = Instantiate(overlayImage.material);
        overlayImage.material = runtimeMat;
        overlayImage.canvasRenderer.SetMaterial(runtimeMat, 0);

        // Make this object a root object so DontDestroyOnLoad works
        if (transform.parent != null)
        {
            transform.SetParent(null, true);
        }
        DontDestroyOnLoad(gameObject);

        overlayImage.raycastTarget = true;

        SetProgress(0f);
        overlayImage.SetMaterialDirty();

        TransitionTo("Gameplay");
    }

    public void TransitionTo(string sceneName)
    {
        if (running != null) StopCoroutine(running);
        running = StartCoroutine(CoTransition(sceneName));
    }

    private IEnumerator CoTransition(string sceneName)
    {
        yield return AnimateProgress(0f, 1f, fadeOutTime);

        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        while (!op.isDone) yield return null;

        yield return AnimateProgress(1f, 0f, fadeInTime);

        running = null;
    }

    private IEnumerator AnimateProgress(float from, float to, float duration)
    {
        duration = Mathf.Max(0.001f, duration);

        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float u = Mathf.Clamp01(t / duration);

            // smoothstep easing
            u = u * u * (3f - 2f * u);

            SetProgress(Mathf.Lerp(from, to, u));
            yield return null;
        }

        SetProgress(to);
    }

    private void SetProgress(float v)
    {
        if (runtimeMat != null && runtimeMat.HasProperty(progressProperty))
        {
            runtimeMat.SetFloat(progressProperty, v);
            overlayImage.SetMaterialDirty();
        }
        else if (runtimeMat != null)
        {
            Debug.LogWarning($"DitherTransition: Material missing property '{progressProperty}'.");
        }
    }
}
