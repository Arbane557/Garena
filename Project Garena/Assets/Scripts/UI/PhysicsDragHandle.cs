using UnityEngine;
using UnityEngine.EventSystems;

public class PhysicsDragHandle : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
{
    private GameManager gm;
    private Vector2Int cellPos;
    private RectTransform bagBoundary;
    private float autoSubmitBand = 0.12f;

    private RectTransform dragRoot;
    private Rigidbody2D dragRb;
    private Vector2 pointerOffset;
    private bool hover;

    public void Init(GameManager manager, Vector2Int pos, RectTransform boundary, float band)
    {
        gm = manager;
        cellPos = pos;
        bagBoundary = boundary;
        autoSubmitBand = band;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        Debug.Log($"[Drag] Begin on {name} (hover={hover})");
        if (gm == null || !gm.IsPhysicsMode) return;
        var entity = gm.GetEntityAtPos(cellPos);
        if (entity == null) return;

        dragRb = GetComponentInParent<Rigidbody2D>();
        if (dragRb == null) return;
        dragRoot = dragRb.GetComponent<RectTransform>();
        if (dragRoot == null) return;

        var parent = dragRoot.parent as RectTransform;
        if (parent == null) return;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, eventData.position, eventData.pressEventCamera, out var localPoint))
            return;

        pointerOffset = (Vector2)dragRoot.localPosition - localPoint;
        dragRb.simulated = false;
        dragRb.linearVelocity = Vector2.zero;
        dragRb.angularVelocity = 0f;
    }

    public void OnDrag(PointerEventData eventData)
    {
        Debug.Log($"[Drag] Move on {name}");
        if (dragRoot == null) return;
        var parent = dragRoot.parent as RectTransform;
        if (parent == null) return;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, eventData.position, eventData.pressEventCamera, out var localPoint))
            return;

        dragRoot.localPosition = localPoint + pointerOffset;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        Debug.Log($"[Drag] End on {name}");
        if (gm == null || dragRoot == null) return;

        var entity = gm.GetEntityAtPos(cellPos);
        if (entity != null && gm.IsInAutoSubmitZoneWorld(dragRoot.position))
        {
            gm.AutoCorrectSubmit(entity);
            dragRoot = null;
            dragRb = null;
            return;
        }

        if (dragRb != null) dragRb.simulated = true;
        dragRoot = null;
        dragRb = null;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        hover = true;
        Debug.Log($"[Drag] PointerEnter {name}");
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hover = false;
        Debug.Log($"[Drag] PointerExit {name}");
    }
}
