using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PopUp : MonoBehaviour
{
    public TMP_Text text;
    public CanvasGroup canvasGroup;
    public float charDelay = 0.03f;
    public float holdSeconds = 1.5f;

    private static PopUp instance;
    private readonly Queue<string> queue = new Queue<string>();
    private Coroutine runner;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        EnsureUI();
        HideInstant();
    }

    public static void Write(string message)
    {
        if (string.IsNullOrEmpty(message)) return;
        if (instance == null) CreateAuto();
        instance.Enqueue(message);
    }

    private void Enqueue(string message)
    {
        queue.Enqueue(message);
        if (runner == null) runner = StartCoroutine(RunQueue());
    }

    private IEnumerator RunQueue()
    {
        while (queue.Count > 0)
        {
            string msg = queue.Dequeue();
            yield return TypeMessage(msg);
            yield return new WaitForSeconds(holdSeconds);
            HideInstant();
        }

        runner = null;
    }

    private IEnumerator TypeMessage(string msg)
    {
        EnsureUI();
        canvasGroup.alpha = 1f;
        text.text = "";

        for (int i = 0; i < msg.Length; i++)
        {
            text.text += msg[i];
            yield return new WaitForSeconds(charDelay);
        }
    }

    private void HideInstant()
    {
        if (canvasGroup != null) canvasGroup.alpha = 0f;
        if (text != null) text.text = "";
    }

    private void EnsureUI()
    {
        if (text != null && canvasGroup != null) return;

        var canvas = GetComponentInChildren<Canvas>();
        if (canvas == null)
        {
            var cgo = new GameObject("PopUpCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            cgo.transform.SetParent(transform, false);
            canvas = cgo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = cgo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(800, 600);
        }

        var panel = new GameObject("PopUpPanel", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        panel.transform.SetParent(canvas.transform, false);
        var rt = (RectTransform)panel.transform;
        rt.anchorMin = new Vector2(0.5f, 0.1f);
        rt.anchorMax = new Vector2(0.5f, 0.1f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(520, 60);
        panel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.7f);
        canvasGroup = panel.GetComponent<CanvasGroup>();

        var tgo = new GameObject("Text", typeof(RectTransform), typeof(TMP_Text));
        tgo.transform.SetParent(panel.transform, false);
        var trt = (RectTransform)tgo.transform;
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = new Vector2(12, 8);
        trt.offsetMax = new Vector2(-12, -8);
        text = tgo.GetComponent<TMP_Text>();
        text.text = "";
        text.fontSize = 20;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.color = Color.white;
    }

    private static void CreateAuto()
    {
        var go = new GameObject("PopUp", typeof(PopUp));
        instance = go.GetComponent<PopUp>();
    }
}
