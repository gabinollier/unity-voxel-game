using UnityEngine;
using UnityEngine.EventSystems;

public class ResizeCorner : MonoBehaviour, IDragHandler, IPointerDownHandler
{
    enum CornerPosition
    {
        TopRight,
        BottomRight,
        BottomLeft,
        TopLeft
    }


    [SerializeField] private CornerPosition cornerPosition;
    [SerializeField] private Vector2 minSize, maxSize;
    [SerializeField] private RectTransform targetRectTransform;

    Vector2 _startPosition;
    Vector2 _startSize;
    Vector2 _coef;

    public void OnPointerDown(PointerEventData eventData)
    {
        _startSize = targetRectTransform.sizeDelta;
        _startPosition = eventData.position;

        _coef =
            cornerPosition == CornerPosition.TopRight ? Vector2.one :
            cornerPosition == CornerPosition.BottomRight ? new Vector2(1, -1) :
            cornerPosition == CornerPosition.BottomLeft ? new Vector2(-1, -1) :
            new Vector2(-1, 1);
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 totalDelta = eventData.position - _startPosition;

        targetRectTransform.sizeDelta = new Vector2(
            Mathf.Clamp(_startSize.x + totalDelta.x * _coef.x, minSize.x, maxSize.x),
            Mathf.Clamp(_startSize.y + totalDelta.y * _coef.y, minSize.y, maxSize.y));
    }

}
