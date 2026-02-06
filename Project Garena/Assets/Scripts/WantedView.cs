using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class WantedView : MonoBehaviour
{
    [Header("uGUI")]
    public Image itemIcon;
    public Image traitIcon;
    public Transform traitIconRow;
    public Image traitIconPrefab;

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
        SetWanted(subType, new List<TraitType> { requiredTrait }, timeLeft, timeTotal);
    }

    public void SetWanted(ItemSubType subType, IReadOnlyList<TraitType> requiredTraits, float timeLeft, float timeTotal)
    {
        if (itemIcon != null) itemIcon.sprite = ItemSprite(subType);

        if (traitIconRow != null && traitIconPrefab != null)
        {
            foreach (Transform c in traitIconRow) Destroy(c.gameObject);
            for (int i = 0; i < requiredTraits.Count; i++)
            {
                var img = Instantiate(traitIconPrefab, traitIconRow);
                img.sprite = TraitSprite(requiredTraits[i]);
                img.enabled = true;
            }
        }
        else if (traitIcon != null)
        {
            traitIcon.sprite = (requiredTraits.Count > 0) ? TraitSprite(requiredTraits[0]) : null;
        }

        if (labelText != null)
        {
            var traitLabel = (requiredTraits.Count == 0)
                ? "NONE"
                : string.Join("+", requiredTraits.Select(t => t.ToString().ToUpper()));
            labelText.text = $"{traitLabel} {subType.ToString().ToUpper()}";
        }
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
