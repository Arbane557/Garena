using UnityEngine;
using UnityEngine.UI;

public class PeakBarTester : MonoBehaviour
{
    [Header("Segments (Images)")]
    public Image energyFill;
    public Image hpLockFill;
    public Image weightLockFill;
    public Image heatLockFill;
    public Image coldLockFill;

    public float barWidth = 420f;
    public float barHeight = 18f;
    public bool vertical = false;

    [Range(0f, 100f)] public float L_hp;
    [Range(0f, 100f)] public float L_weight;
    [Range(0f, 100f)] public float L_heat;
    [Range(0f, 100f)] public float L_cold;

    public bool autoClamp = true;

    void Awake()
    {
        AutoAssignIfMissing();
        EnsureLayout();
    }

    void OnValidate()
    {
        AutoAssignIfMissing();
        EnsureLayout();
        UpdateBar();
    }

    void Update()
    {
        UpdateBar();
    }

    void UpdateBar()
    {
        float total = L_hp + L_weight + L_heat + L_cold;
        if (autoClamp && total > 100f)
        {
            float scale = 100f / total;
            L_hp *= scale;
            L_weight *= scale;
            L_heat *= scale;
            L_cold *= scale;
            total = 100f;
        }

        float green = Mathf.Max(0f, 100f - total);

        SetSegment(energyFill, green);
        SetSegment(heatLockFill, L_heat);
        SetSegment(coldLockFill, L_cold);
        SetSegment(hpLockFill, L_hp);
        SetSegment(weightLockFill, L_weight);
    }

    void AutoAssignIfMissing()
    {
        if (energyFill == null) energyFill = FindImage("Energy");
        if (heatLockFill == null) heatLockFill = FindImage("Hot");
        if (coldLockFill == null) coldLockFill = FindImage("Ice");
        if (hpLockFill == null) hpLockFill = FindImage("HP");
        if (weightLockFill == null) weightLockFill = FindImage("Weight");
    }

    Image FindImage(string name)
    {
        var t = transform.Find(name);
        if (t == null) return null;
        return t.GetComponent<Image>();
    }

    void EnsureLayout()
    {
        var rt = GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.sizeDelta = new Vector2(barWidth, barHeight);
        }
        var h = GetComponent<HorizontalLayoutGroup>();
        var v = GetComponent<VerticalLayoutGroup>();
        if (vertical)
        {
            if (h != null) DestroyImmediate(h);
            if (v == null) v = gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = 0f;
            v.childAlignment = TextAnchor.UpperCenter;
            v.childControlWidth = false;
            v.childControlHeight = false;
            v.childForceExpandWidth = false;
            v.childForceExpandHeight = false;
        }
        else
        {
            if (v != null) DestroyImmediate(v);
            if (h == null) h = gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 0f;
            h.childAlignment = TextAnchor.MiddleLeft;
            h.childControlWidth = false;
            h.childControlHeight = false;
            h.childForceExpandWidth = false;
            h.childForceExpandHeight = false;
        }
    }

    void SetSegment(Image img, float value)
    {
        if (img == null) return;
        float pct = Mathf.Clamp01(value / 100f);
        var le = img.GetComponent<LayoutElement>();
        if (le != null)
        {
            le.preferredWidth = vertical ? barWidth : barWidth * pct;
            le.minWidth = 0f;
            le.preferredHeight = vertical ? barHeight * pct : barHeight;
        }
        else
        {
            le = img.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = vertical ? barWidth : barWidth * pct;
            le.preferredHeight = vertical ? barHeight * pct : barHeight;
        }
        var rt = img.rectTransform;
        if (rt != null)
        {
            rt.sizeDelta = vertical
                ? new Vector2(barWidth, barHeight * pct)
                : new Vector2(barWidth * pct, barHeight);
        }
    }
}
