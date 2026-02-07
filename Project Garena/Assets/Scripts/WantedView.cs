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
    public TMP_Text narrativeText; // short flavor line

    [Header("Sprites")]
    public Sprite breadSprite, knifeSprite, waterSprite;
    public Sprite fireSprite, iceSprite, sentientSprite;

    private string lastPopupName;
    private string lastPopupLine;

    public void SetWanted(ItemSubType subType, TraitType requiredTrait, float timeLeft, float timeTotal)
    {
        SetWanted(subType, new List<TraitType> { requiredTrait }, timeLeft, timeTotal, null, null);
    }

    public void SetWanted(ItemSubType subType, IReadOnlyList<TraitType> requiredTraits, float timeLeft, float timeTotal)
    {
        SetWanted(subType, requiredTraits, timeLeft, timeTotal, null, null);
    }

    public void SetWanted(ItemSubType subType, IReadOnlyList<TraitType> requiredTraits, float timeLeft, float timeTotal, string flavorLine)
    {
        SetWanted(subType, requiredTraits, timeLeft, timeTotal, null, flavorLine);
    }

    public void SetWanted(ItemSubType subType, IReadOnlyList<TraitType> requiredTraits, float timeLeft, float timeTotal, string customerName, string flavorLine)
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
                ? "JUST"
                : string.Join("+", requiredTraits.Select(t => t.ToString().ToUpper()));
            labelText.text = $"{traitLabel} {subType.ToString().ToUpper()}";
        }
        if (timerText != null) timerText.text = $"{Mathf.CeilToInt(timeLeft)}s";
        string narrative = string.IsNullOrWhiteSpace(flavorLine)
            ? BuildNarrative(subType, requiredTraits)
            : flavorLine;
        if (narrativeText != null) narrativeText.text = narrative;
        if (customerName != lastPopupName || narrative != lastPopupLine)
        {
            PopUp.Write(customerName, narrative);
            lastPopupName = customerName;
            lastPopupLine = narrative;
        }

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

    string BuildNarrative(ItemSubType subType, IReadOnlyList<TraitType> traits)
    {
        if (traits == null || traits.Count == 0)
        {
            return $"I need a {ItemName(subType)}. Fast.";
        }

        var adj = string.Join(" and ", traits.Select(TraitAdjective));
        var target = TraitTarget(traits);
        return $"I need a {adj} {ItemName(subType)} to face {target}.";
    }

    string ItemName(ItemSubType subType) => subType switch
    {
        ItemSubType.Bread => "bread",
        ItemSubType.Knife => "sword",
        ItemSubType.WaterBottle => "potion",
        _ => "item"
    };

    string TraitAdjective(TraitType t) => t switch
    {
        TraitType.Fire => "fiery",
        TraitType.Ice => "frozen",
        TraitType.Sentient => "haunted",
        TraitType.Haunted => "haunted",
        _ => t.ToString().ToLower()
    };

    string TraitTarget(IReadOnlyList<TraitType> traits)
    {
        if (traits.Contains(TraitType.Fire)) return "slimes";
        if (traits.Contains(TraitType.Ice)) return "sparks";
        if (traits.Contains(TraitType.Haunted)) return "ghosts";
        return "trouble";
    }
}
