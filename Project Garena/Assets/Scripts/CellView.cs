using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

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
    private Vector2 baseOffset;
    public float moveTweenSeconds = 0.12f;
    public Ease moveEase = Ease.OutQuad;
    public float popScale = 0.12f;
    public float popDuration = 0.18f;
    public float selectPulseScale = 0.06f;
    public float selectPulseDuration = 0.12f;

    private bool hadEntity;
    private bool wasSelected;
    private string lastEntityId;
    private int lastTraitCount;

    public void Init(System.Action onClick)
    {
        btn = GetComponent<Button>();
        if (btn != null) btn.onClick.AddListener(() => onClick?.Invoke());
        rootRect = GetComponent<RectTransform>();

        if (mainIcon != null)
        {
            mainIcon.raycastTarget = false;
            var c = mainIcon.GetComponent<Canvas>();
            if (c == null) c = mainIcon.gameObject.AddComponent<Canvas>();
            c.overrideSorting = true;
            c.sortingOrder = 10;
        }

        if (traitIconRow != null)
        {
            var rowCanvas = traitIconRow.GetComponent<Canvas>();
            if (rowCanvas == null) rowCanvas = traitIconRow.gameObject.AddComponent<Canvas>();
            rowCanvas.overrideSorting = true;
            rowCanvas.sortingOrder = 20;
        }
    }

    public void SetCell(BoxEntity e, bool selected, bool isZone, bool inFireAura, bool inIceAura, Vector2Int cellPos, Vector2Int fromPos)
    {
        // Background / outline
        outline.enabled = selected;

        // zone tint (simple)
        if (isZone) background.color = new Color(0.1f, 0.3f, 0.1f, 0.4f);
        else if (inFireAura) background.color = new Color(0.35f, 0.08f, 0.04f, 1f);
        else if (inIceAura) background.color = new Color(0.08f, 0.18f, 0.35f, 1f);
        else background.color = new Color(0.12f, 0.12f, 0.12f, 1f);

        // Clear
        mainIcon.enabled = false;
        foreach (Transform c in traitIconRow) Destroy(c.gameObject);

        if (e == null)
        {
            hadEntity = false;
            lastEntityId = null;
            lastTraitCount = 0;
            wasSelected = selected;
            return;
        }

        bool isAnchor = e.anchor == cellPos;

        if (!isAnchor)
        {
            hadEntity = true;
            lastEntityId = e.id;
            lastTraitCount = e.traits != null ? e.traits.Count : 0;
            wasSelected = selected;
            return;
        }

        if (e.isTraitTile)
        {
            mainIcon.enabled = true;
            mainIcon.sprite = TraitSprite(e.tileTrait);
            ApplySize(e.size);
            AnimateMove(cellPos, fromPos);
            return;
        }

        mainIcon.enabled = true;
        mainIcon.sprite = BoxSprite(e.subType);
        ApplySize(e.size);
        AnimateMove(cellPos, fromPos);

        foreach (var t in e.traits)
        {
            var img = Instantiate(traitIconPrefab, traitIconRow);
            img.sprite = TraitSprite(t);
            img.enabled = true;
        }

        // Pop on first reveal or entity change
        if (!hadEntity || lastEntityId != e.id)
        {
            var rt = mainIcon.rectTransform;
            rt.DOKill();
            rt.localScale = Vector3.one;
            rt.DOPunchScale(Vector3.one * popScale, popDuration, 10, 0.6f);
        }

        // Pulse trait row when trait count increases
        int traitCount = e.traits != null ? e.traits.Count : 0;
        if (lastEntityId == e.id && traitCount > lastTraitCount && traitIconRow != null)
        {
            var tr = traitIconRow as RectTransform;
            if (tr != null)
            {
                tr.DOKill();
                tr.localScale = Vector3.one;
                tr.DOPunchScale(Vector3.one * 0.15f, 0.2f, 10, 0.7f);
            }
        }

        // Selection pulse
        if (selected && !wasSelected && rootRect != null)
        {
            rootRect.DOKill();
            rootRect.localScale = Vector3.one;
            rootRect.DOPunchScale(Vector3.one * selectPulseScale, selectPulseDuration, 10, 0.7f);
        }

        hadEntity = true;
        lastEntityId = e.id;
        lastTraitCount = traitCount;
        wasSelected = selected;

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
        float offsetX = (size.x > 1) ? (cellSize.x + spacing.x) * 0.5f : 0f;
        float offsetY = (size.y > 1) ? -(cellSize.y + spacing.y) * 0.5f : 0f;
        baseOffset = new Vector2(offsetX, offsetY);
        rt.anchoredPosition = baseOffset;
    }

    void AnimateMove(Vector2Int cellPos, Vector2Int fromPos)
    {
        var rt = mainIcon.rectTransform;
        rt.DOKill();
        rt.anchoredPosition = baseOffset;
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
