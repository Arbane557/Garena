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
    public Image fxOverlay;

    [Header("Background")]
    public bool useInspectorColors = true;
    public Color normalBgColor = new Color(0.12f, 0.12f, 0.12f, 1f);
    public Color zoneBgColor = new Color(0.1f, 0.3f, 0.1f, 0.4f);
    public Color IceAuraColor;
    public Color FireAuraColor;
    public Color HauntedAuraColor = new Color(0.45f, 0.1f, 0.6f, 1f);
    public Color GhostAuraColor = new Color(0.45f, 0.1f, 0.6f, 1f);

    // Assign sprites in Inspector
    public Sprite breadSprite;
    public Sprite knifeSprite;
    public Sprite waterBottleSprite;
    public Sprite ghostSprite;

    public Sprite fireSprite;
    public Sprite iceSprite;
    public Sprite sentientSprite;
    public Sprite hauntedSprite;

    [Header("Trait Icon Sprites")]
    public Sprite traitFireSprite;
    public Sprite traitIceSprite;
    public Sprite traitSentientSprite;
    public Sprite traitHauntedSprite;

    [Header("Frame Animations")]
    public Sprite[] ghostFrames;
    public float ghostFps = 10f;
    public Sprite[] fireFrames;
    public float fireFps = 10f;
    public Sprite[] iceFrames;
    public float iceFps = 10f;

    private Button btn;
    private RectTransform rootRect;
    private Vector2 baseOffset;
    public float moveTweenSeconds = 0.12f;
    public Ease moveEase = Ease.OutQuad;
    public float popScale = 0.12f;
    public float popDuration = 0.18f;
    public float selectPulseScale = 0.06f;
    public float selectPulseDuration = 0.12f;
    [Header("Submit Tween")]
    public float submitTweenUp = 24f;
    public float submitTweenDuration = 0.2f;
    public float submitPunchScale = 0.2f;

    private bool hadEntity;
    private bool wasSelected;
    private string lastEntityId;
    private int lastTraitCount;
    private FrameAnimator frameAnimator;

    public void Init(System.Action onClick)
    {
        btn = GetComponent<Button>();
        if (btn != null) btn.onClick.AddListener(() => onClick?.Invoke());
        rootRect = GetComponent<RectTransform>();

        if (background == null)
        {
            background = GetComponent<Image>();
        }
        if (background == null)
        {
            background = gameObject.AddComponent<Image>();
            background.color = new Color(0f, 0f, 0f, 0f);
        }
        background.raycastTarget = true;
        var cg = GetComponent<CanvasGroup>();
        if (cg != null)
        {
            cg.blocksRaycasts = true;
            cg.interactable = true;
        }

        if (mainIcon != null)
        {
            mainIcon.raycastTarget = false;
            var c = mainIcon.GetComponent<Canvas>();
            if (c == null) c = mainIcon.gameObject.AddComponent<Canvas>();
            c.overrideSorting = true;
            c.sortingOrder = 10;
            frameAnimator = mainIcon.GetComponent<FrameAnimator>();
            if (frameAnimator == null) frameAnimator = mainIcon.gameObject.AddComponent<FrameAnimator>();
        }

        if (traitIconRow != null)
        {
            var rowCanvas = traitIconRow.GetComponent<Canvas>();
            if (rowCanvas == null) rowCanvas = traitIconRow.gameObject.AddComponent<Canvas>();
            rowCanvas.overrideSorting = true;
            rowCanvas.sortingOrder = 20;
        }

        EnsureFxOverlay();
    }

    void EnsureFxOverlay()
    {
        if (fxOverlay != null) return;
        var go = new GameObject("FxOverlay", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(transform, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        fxOverlay = go.GetComponent<Image>();
        fxOverlay.raycastTarget = false;
        fxOverlay.color = new Color(1f, 1f, 1f, 0f);
        fxOverlay.enabled = false;
    }

    public void PlaySplash(Color color, float inSeconds, float holdSeconds, float outSeconds, float delaySeconds)
    {
        EnsureFxOverlay();
        fxOverlay.DOKill();
        fxOverlay.enabled = true;
        var c = new Color(color.r, color.g, color.b, 0f);
        fxOverlay.color = c;
        var seq = DOTween.Sequence();
        if (delaySeconds > 0f) seq.AppendInterval(delaySeconds);
        seq.Append(fxOverlay.DOColor(new Color(color.r, color.g, color.b, 1f), inSeconds));
        seq.AppendInterval(holdSeconds);
        seq.Append(fxOverlay.DOColor(c, outSeconds));
        seq.OnComplete(() => fxOverlay.enabled = false);
    }

    public void PlayFlash(Color color, float onSeconds, float offSeconds, float onSeconds2)
    {
        EnsureFxOverlay();
        fxOverlay.DOKill();
        fxOverlay.enabled = true;
        var cOn = new Color(color.r, color.g, color.b, 1f);
        var cOff = new Color(color.r, color.g, color.b, 0f);
        fxOverlay.color = cOff;
        var seq = DOTween.Sequence();
        seq.Append(fxOverlay.DOColor(cOn, 0.02f));
        seq.AppendInterval(onSeconds);
        seq.Append(fxOverlay.DOColor(cOff, 0.02f));
        seq.AppendInterval(offSeconds);
        seq.Append(fxOverlay.DOColor(cOn, 0.02f));
        seq.AppendInterval(onSeconds2);
        seq.Append(fxOverlay.DOColor(cOff, 0.02f));
        seq.OnComplete(() => fxOverlay.enabled = false);
    }

    public void SetCell(BoxEntity e, bool selected, bool isZone, bool inFireAura, bool inIceAura, bool inHauntedAura, bool inGhostAura, Vector2Int cellPos, Vector2Int fromPos)
    {
        outline.enabled = selected;

        if (useInspectorColors && background != null)
        {
            background.color = isZone ? zoneBgColor : normalBgColor;
        }
        if (inFireAura) background.color = FireAuraColor;
        else if (inIceAura) background.color = IceAuraColor;
        else if (inGhostAura) background.color = GhostAuraColor;
        else if (inHauntedAura) background.color = HauntedAuraColor;

        mainIcon.enabled = false;
        foreach (Transform c in traitIconRow) Destroy(c.gameObject);

        if (e == null)
        {
            if (frameAnimator != null) frameAnimator.Stop();
            mainIcon.sprite = null;
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
            ApplySize(e.size);
            AnimateMove(cellPos, fromPos);
            PlayTraitAnimation(e.tileTrait);
            return;
        }

        mainIcon.enabled = true;
        ApplySize(e.size);
        AnimateMove(cellPos, fromPos);
        PlayItemAnimation(e.subType);

        foreach (var t in e.traits)
        {
            var img = Instantiate(traitIconPrefab, traitIconRow);
            img.sprite = TraitSprite(t);
            img.enabled = true;
        }

        if (!hadEntity || lastEntityId != e.id)
        {
            var rt = mainIcon.rectTransform;
            rt.DOKill();
            rt.localScale = Vector3.one;
            rt.DOPunchScale(Vector3.one * popScale, popDuration, 10, 0.6f);
        }

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
            _ => null
        };
    }

    Sprite TraitSprite(TraitType tt)
    {
        return tt switch
        {
            TraitType.Fire => traitFireSprite != null ? traitFireSprite : fireSprite,
            TraitType.Ice => traitIceSprite != null ? traitIceSprite : iceSprite,
            TraitType.Sentient => traitSentientSprite != null ? traitSentientSprite : sentientSprite,
            TraitType.Haunted => traitHauntedSprite != null ? traitHauntedSprite : hauntedSprite,
            _ => null
        };
    }

    public void PlaySubmitTween(GameObject effectPrefab, float effectLifeSeconds)
    {
        if (mainIcon == null) return;
        var rt = mainIcon.rectTransform;
        rt.DOKill();
        var startPos = rt.anchoredPosition;
        rt.DOPunchScale(Vector3.one * submitPunchScale, submitTweenDuration, 8, 0.7f);
        rt.DOLocalMoveY(startPos.y + submitTweenUp, submitTweenDuration).SetEase(Ease.OutQuad)
            .OnComplete(() => rt.anchoredPosition = startPos);

        if (effectPrefab != null)
        {
            var fx = Object.Instantiate(effectPrefab, transform);
            var fxRt = fx.transform as RectTransform;
            if (fxRt != null) fxRt.anchoredPosition = Vector2.zero;
            Object.Destroy(fx, Mathf.Max(0.1f, effectLifeSeconds));
        }
    }

    void PlayItemAnimation(ItemSubType subType)
    {
        if (frameAnimator == null) return;
        if (subType == ItemSubType.Ghost && ghostFrames != null && ghostFrames.Length > 0)
        {
            frameAnimator.Play(ghostFrames, ghostFps);
            return;
        }
        frameAnimator.Stop();
        mainIcon.sprite = BoxSprite(subType);
    }

    void PlayTraitAnimation(TraitType t)
    {
        if (frameAnimator == null) return;
        if (t == TraitType.Fire && fireFrames != null && fireFrames.Length > 0)
        {
            frameAnimator.Play(fireFrames, fireFps);
            return;
        }
        if (t == TraitType.Ice && iceFrames != null && iceFrames.Length > 0)
        {
            frameAnimator.Play(iceFrames, iceFps);
            return;
        }
        frameAnimator.Stop();
        mainIcon.sprite = TraitSprite(t);
    }
}
