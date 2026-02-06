using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CellView : MonoBehaviour
{
    public Image background;
    public Image mainIcon;
    public Transform traitIconRow;
    public Image traitIconPrefab;

    public Outline outline;

    // Assign sprites in Inspector
    public Sprite breadSprite;
    public Sprite knifeSprite;
    public Sprite waterBottleSprite;

    public Sprite fireSprite;
    public Sprite iceSprite;
    public Sprite sentientSprite;
    public Sprite hauntedSprite;

    private Button btn;

    public void Init(System.Action onClick)
    {
        btn = GetComponent<Button>();
        if (btn != null) btn.onClick.AddListener(() => onClick?.Invoke());
    }

    public void SetCell(BoxEntity e, bool selected, bool isZone)
    {
        // Background / outline
        outline.enabled = selected;

        // zone tint (simple)
        if (isZone) background.color = new Color(0.1f, 0.3f, 0.1f, 0.4f);
        else background.color = new Color(0.12f, 0.12f, 0.12f, 1f);

        // Clear
        mainIcon.enabled = false;
        foreach (Transform c in traitIconRow) Destroy(c.gameObject);

        if (e == null) return;

        mainIcon.enabled = true;
        mainIcon.sprite = BoxSprite(e.subType);

        foreach (var t in e.traits)
        {
            var img = Instantiate(traitIconPrefab, traitIconRow);
            img.sprite = TraitSprite(t);
            img.enabled = true;
        }
    }

    Sprite BoxSprite(ItemSubType st)
    {
        return st switch
        {
            ItemSubType.Bread => breadSprite,
            ItemSubType.Knife => knifeSprite,
            ItemSubType.WaterBottle => waterBottleSprite,
            _ => null
        };
    }

    Sprite TraitSprite(TraitType tt)
    {
        return tt switch
        {
            TraitType.Fire => fireSprite,
            TraitType.Ice => iceSprite,
            TraitType.Sentient => sentientSprite,
            TraitType.Haunted => hauntedSprite,
            _ => null
        };
    }
}
