using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

namespace Template.Core
{
    public class MenuUIBuilder : MonoBehaviour
    {
        [Header("Menu Buttons")]
        public bool includePlay;
        public bool includeQuit;
        public bool includeResume;
        public bool isPauseMenu;

        [Header("Gameplay Scene Name")]
        public string gameplaySceneName = "Gameplay";

        private Canvas canvas;
        private GameObject panel;
        private Image brightnessOverlay;

        void Awake()
        {
            BuildUI();
            ApplySettings();
        }

        void Update()
        {
            if (isPauseMenu)
            {
                var kb = Keyboard.current;
                if (kb != null && kb.escapeKey.wasPressedThisFrame)
                {
                    TogglePause();
                }
            }
        }

        private void BuildUI()
        {
            canvas = GetOrCreateCanvas();
            EnsureEventSystem();
            panel = CreatePanel(canvas.transform, "MenuPanel");

            var layout = panel.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 10;
            layout.padding = new RectOffset(20, 20, 20, 20);

            var fitter = panel.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            CreateTitle(panel.transform, "INTERNAL INSTABILITY");

            if (includePlay)
            {
                CreateButton(panel.transform, "Play", () =>
                {
                    SceneManager.LoadScene(gameplaySceneName);
                });
            }

            if (includeResume)
            {
                CreateButton(panel.transform, "Resume", TogglePause);
            }

            CreateHeader(panel.transform, "Settings");
            CreateSlider(panel.transform, "Brightness", SettingsStore.Brightness, v =>
            {
                SettingsStore.Brightness = v;
                ApplyBrightness();
            });
            CreateSlider(panel.transform, "Master", SettingsStore.MasterVolume, v =>
            {
                SettingsStore.MasterVolume = v;
                ApplyAudio();
            });
            CreateSlider(panel.transform, "SFX", SettingsStore.SfxVolume, v =>
            {
                SettingsStore.SfxVolume = v;
                ApplyAudio();
            });
            CreateSlider(panel.transform, "BGM", SettingsStore.BgmVolume, v =>
            {
                SettingsStore.BgmVolume = v;
                ApplyAudio();
            });

            if (includeQuit)
            {
                CreateButton(panel.transform, "Quit", Application.Quit);
            }

            brightnessOverlay = CreateBrightnessOverlay(canvas.transform);

            if (isPauseMenu)
            {
                panel.SetActive(false);
            }
        }

        private void TogglePause()
        {
            bool next = !panel.activeSelf;
            panel.SetActive(next);
            if (next)
            {
                Time.timeScale = 0f;
            }
            else
            {
                Time.timeScale = 1f;
            }
        }

        private void ApplySettings()
        {
            ApplyBrightness();
            ApplyAudio();
        }

        private void ApplyBrightness()
        {
            if (brightnessOverlay == null) return;
            float b = SettingsStore.Brightness;
            var c = brightnessOverlay.color;
            c.a = Mathf.Clamp01(1f - b);
            brightnessOverlay.color = c;
            SettingsStore.Save();
        }

        private void ApplyAudio()
        {
            var audio = ServiceHub.Get<Template.Audio.AudioManager>();
            if (audio != null)
            {
                audio.SetMasterVolume(SettingsStore.MasterVolume);
                audio.SetSfxVolume(SettingsStore.SfxVolume);
                audio.SetBgmVolume(SettingsStore.BgmVolume);
            }
            SettingsStore.Save();
        }

        private Canvas GetOrCreateCanvas()
        {
            var existing = GetComponentInChildren<Canvas>();
            if (existing != null) return existing;

            var go = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.transform.SetParent(transform, false);
            var c = go.GetComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(800, 600);
            return c;
        }

        private void EnsureEventSystem()
        {
            if (FindObjectOfType<EventSystem>() != null) return;
            var es = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            DontDestroyOnLoad(es);
        }

        private static GameObject CreatePanel(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(320, 0);
            go.GetComponent<Image>().color = new Color(0.05f, 0.05f, 0.05f, 0.85f);
            return go;
        }

        private static void CreateTitle(Transform parent, string text)
        {
            var t = CreateText(parent, text, 20, FontStyle.Bold);
            t.alignment = TextAnchor.MiddleCenter;
        }

        private static void CreateHeader(Transform parent, string text)
        {
            var t = CreateText(parent, text, 14, FontStyle.Bold);
            t.alignment = TextAnchor.MiddleCenter;
        }

        private static Text CreateText(Transform parent, string text, int size, FontStyle style)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            t.text = text;
            t.fontSize = size;
            t.fontStyle = style;
            t.color = Color.white;
            t.alignment = TextAnchor.MiddleCenter;
            return t;
        }

        private static void CreateButton(Transform parent, string label, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject(label + "Button", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = new Vector2(240, 36);
            go.GetComponent<Image>().color = new Color(0.15f, 0.15f, 0.15f, 1f);
            var btn = go.GetComponent<Button>();
            btn.onClick.AddListener(onClick);
            var t = CreateText(go.transform, label, 14, FontStyle.Bold);
            t.rectTransform.anchorMin = Vector2.zero;
            t.rectTransform.anchorMax = Vector2.one;
            t.rectTransform.offsetMin = Vector2.zero;
            t.rectTransform.offsetMax = Vector2.zero;
        }

        private static void CreateSlider(Transform parent, string label, float value, UnityEngine.Events.UnityAction<float> onChanged)
        {
            var row = new GameObject(label + "Row", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            row.transform.SetParent(parent, false);
            var h = row.GetComponent<HorizontalLayoutGroup>();
            h.childAlignment = TextAnchor.MiddleCenter;
            h.spacing = 8;
            h.childForceExpandWidth = false;

            var text = CreateText(row.transform, label, 12, FontStyle.Normal);
            text.rectTransform.sizeDelta = new Vector2(80, 20);

            var sliderGo = new GameObject(label + "Slider", typeof(RectTransform), typeof(Slider));
            sliderGo.transform.SetParent(row.transform, false);
            var slider = sliderGo.GetComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = value;
            slider.onValueChanged.AddListener(onChanged);
            var rt = (RectTransform)sliderGo.transform;
            rt.sizeDelta = new Vector2(140, 20);

            var bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(sliderGo.transform, false);
            var bgImg = bg.GetComponent<Image>();
            bgImg.color = new Color(0.2f, 0.2f, 0.2f, 1f);
            bgImg.type = Image.Type.Sliced;
            var bgRt = (RectTransform)bg.transform;
            bgRt.anchorMin = new Vector2(0, 0.25f);
            bgRt.anchorMax = new Vector2(1, 0.75f);
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;

            var fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(sliderGo.transform, false);
            var fillAreaRt = (RectTransform)fillArea.transform;
            fillAreaRt.anchorMin = new Vector2(0, 0.25f);
            fillAreaRt.anchorMax = new Vector2(1, 0.75f);
            fillAreaRt.offsetMin = new Vector2(5, 0);
            fillAreaRt.offsetMax = new Vector2(-5, 0);

            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(fillArea.transform, false);
            var fillImg = fill.GetComponent<Image>();
            fillImg.color = new Color(0.6f, 0.8f, 0.2f, 1f);
            var fillRt = (RectTransform)fill.transform;
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;

            var handleSlideArea = new GameObject("Handle Slide Area", typeof(RectTransform));
            handleSlideArea.transform.SetParent(sliderGo.transform, false);
            var handleAreaRt = (RectTransform)handleSlideArea.transform;
            handleAreaRt.anchorMin = Vector2.zero;
            handleAreaRt.anchorMax = Vector2.one;
            handleAreaRt.offsetMin = new Vector2(5, 0);
            handleAreaRt.offsetMax = new Vector2(-5, 0);

            var handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handle.transform.SetParent(handleSlideArea.transform, false);
            var handleImg = handle.GetComponent<Image>();
            handleImg.color = Color.white;
            var handleRt = (RectTransform)handle.transform;
            handleRt.sizeDelta = new Vector2(12, 12);

            slider.fillRect = fillRt;
            slider.handleRect = handleRt;
            slider.targetGraphic = handleImg;
        }

        private static Image CreateBrightnessOverlay(Transform parent)
        {
            var go = new GameObject("BrightnessOverlay", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var img = go.GetComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0f);
            img.raycastTarget = false;
            return img;
        }
    }
}
