using UnityEngine;
using UnityEngine.UI;

public class BufferView : MonoBehaviour
{
    public Image[] slots; // size 3
    public Sprite breadSprite, knifeSprite, waterSprite;
    private Vector2[] baseSizes;
    private Quaternion[] baseRotations;

    private void Awake()
    {
        if (slots == null) return;

        baseSizes = new Vector2[slots.Length];
        baseRotations = new Quaternion[slots.Length];
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null) continue;
            var rt = slots[i].rectTransform;
            baseSizes[i] = rt.sizeDelta;
            baseRotations[i] = rt.localRotation;
        }
    }

    public void Set(ItemSubType[] items)
    {
        if (slots == null) return;

        for (int i = 0; i < slots.Length; i++)
        {
            if (i >= items.Length) { slots[i].enabled = false; continue; }

            slots[i].enabled = true;
            slots[i].sprite = items[i] switch
            {
                ItemSubType.Bread => breadSprite,
                ItemSubType.Knife => knifeSprite,
                ItemSubType.WaterBottle => waterSprite,
                _ => null
            };

            ApplySizeAndRotation(i, items[i]);
        }
    }

    private void ApplySizeAndRotation(int index, ItemSubType item)
    {
        if (index < 0 || index >= slots.Length) return;
        if (slots[index] == null) return;

        var rt = slots[index].rectTransform;
        var baseSize = (baseSizes != null && index < baseSizes.Length) ? baseSizes[index] : rt.sizeDelta;
        var baseRot = (baseRotations != null && index < baseRotations.Length) ? baseRotations[index] : rt.localRotation;

        switch (item)
        {
            case ItemSubType.Knife:
                rt.sizeDelta = new Vector2(baseSize.x * 2f, baseSize.y);
                rt.localRotation = baseRot;
                break;
            case ItemSubType.WaterBottle:
                rt.sizeDelta = baseSize;
                rt.localRotation = baseRot;
                break;
            default:
                rt.sizeDelta = baseSize;
                rt.localRotation = baseRot;
                break;
        }
    }
}
