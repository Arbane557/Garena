using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WantedView : MonoBehaviour
{
    [Header("uGUI")]
    public Image itemIcon;
    public Image traitIcon;

    // Optional: fill bar for time left
    public Image timerFill;

    [Header("TMP")]
    public TMP_Text labelText;   // e.g. "ICE KNIFE"
    public TMP_Text timerText;   // e.g. "9s"

    [Header("Sprites")]
    public Sprite breadSprite, knifeSprite, waterSprite;
    public Sprite fireSprite, iceSprite, sentientSprite;

    public void SetWanted(ItemSubType subType, TraitType requiredTrait, float timeLeft, float timeTotal)
    {
        if (itemIcon != null) itemIcon.sprite = ItemSprite(subType);
        if (traitIcon != null) traitIcon.sprite = TraitSprite(requiredTrait);

        if (labelText != null) labelText.text = $"{requiredTrait.ToString().ToUpper()} {subType.ToString().ToUpper()}";
        if (timerText != null) timerText.text = $"{Mathf.CeilToInt(timeLeft)}s";

        if (timerFill != null)
        {
            float t = (timeTotal <= 0f) ? 0f : Mathf.Clamp01(timeLeft / timeTotal);
            timerFill.fillAmount = t;
        }
    }

    Sprite ItemSprite(ItemSubType st) => st switch
    {
        ItemSubType.Bread => breadSprite,
        ItemSubType.Knife => knifeSprite,
        ItemSubType.WaterBottle => waterSprite,
        _ => null
    };

    Sprite TraitSprite(TraitType t) => t switch
    {
        TraitType.Fire => fireSprite,
        TraitType.Ice => iceSprite,
        TraitType.Sentient => sentientSprite,
        _ => null
    };
}
