using UnityEngine;
using UnityEngine.UI;

public class BufferView : MonoBehaviour
{
    public Image[] slots; // size 3
    public Sprite breadSprite, knifeSprite, waterSprite;

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
        }
    }
}
