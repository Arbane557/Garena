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
    public Sprite ghostSprite;

    public Sprite fireSprite;
    public Sprite iceSprite;
    public Sprite sentientSprite;
    public Sprite hauntedSprite;

    private Button btn;
    private RectTransform rootRect;

    public void Init(System.Action onClick)
    {
        btn = GetComponent<Button>();
        if (btn != null) btn.onClick.AddListener(() => onClick?.Invoke());
        rootRect = GetComponent<RectTransform>();
    }

    public void SetCell(BoxEntity e, bool selected, bool isZone, Vector2Int cellPos)
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

        bool isAnchor = e.anchor == cellPos;

        if (e.isTraitTile)
        {
            if (isAnchor)
            {
                mainIcon.enabled = true;
                mainIcon.sprite = TraitSprite(e.tileTrait);
                ApplySize(e.size);
            }
            return;
        }

        if (isAnchor)
        {
            mainIcon.enabled = true;
            mainIcon.sprite = BoxSprite(e.subType);
            ApplySize(e.size);
        }

        if (isAnchor)
        {
            foreach (var t in e.traits)
            {
                var img = Instantiate(traitIconPrefab, traitIconRow);
                img.sprite = TraitSprite(t);
                img.enabled = true;
            }
        }

    }

    void ApplySize(Vector2Int size)
    {
        if (rootRect == null) rootRect = GetComponent<RectTransform>();
        var cellSize = rootRect != null ? rootRect.rect.size : new Vector2(40, 40);
        var spacing = Vector2.zero;
        var grid = GetComponentInParent<GridLayoutGroup>();
        if (grid != null)
        {
            cellSize = grid.cellSize;
            spacing = grid.spacing;
        }
        var rt = mainIcon.rectTransform;
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        float w = cellSize.x * size.x + spacing.x * (size.x - 1);
        float h = cellSize.y * size.y + spacing.y * (size.y - 1);
        rt.sizeDelta = new Vector2(w, h);
        rt.anchoredPosition = Vector2.zero;
    }

    Sprite BoxSprite(ItemSubType st)
    {
        return st switch
        {
            ItemSubType.Bread => breadSprite,
            ItemSubType.Knife => knifeSprite,
            ItemSubType.WaterBottle => waterBottleSprite,
            ItemSubType.Ghost => ghostSprite,
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
