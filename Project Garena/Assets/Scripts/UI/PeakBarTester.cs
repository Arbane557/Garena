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

    [Header("Dimensions")]
    public float barWidth = 420f;
    public float barHeight = 18f;

    [Header("Values (0..100)")]
    [Range(0f, 100f)] public float L_hp;
    [Range(0f, 100f)] public float L_weight;
    [Range(0f, 100f)] public float L_heat;
    [Range(0f, 100f)] public float L_cold;

    public bool autoClamp = true;

    void OnValidate()
    {
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

    void SetSegment(Image img, float value)
    {
        if (img == null) return;
        float pct = Mathf.Clamp01(value / 100f);
        var le = img.GetComponent<LayoutElement>();
        if (le != null)
        {
            le.preferredWidth = barWidth * pct;
            le.minWidth = 0f;
            le.preferredHeight = barHeight;
        }
        var rt = img.rectTransform;
        if (rt != null)
        {
            rt.sizeDelta = new Vector2(barWidth * pct, barHeight);
        }
    }
}
