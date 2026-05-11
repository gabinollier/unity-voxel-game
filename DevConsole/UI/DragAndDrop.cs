using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class DragAndDrop : MonoBehaviour, IDragHandler, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] RectTransform rectTransformToMove;
    [SerializeField] bool clampToScreen;
    [SerializeField] Vector2 clampToScreenPadding;

    // constant offset between the mouse position and the rectTranformToMove position
    Vector2 _offset;
    Vector2 _canvasMin;
    Vector2 _canvasMax;
    Camera _camera;

    private void Start()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        RectTransform canvasRT = canvas.GetComponent<RectTransform>();
        _camera = Camera.main;
        _canvasMax = _camera.WorldToScreenPoint(canvasRT.TransformPoint(canvasRT.rect.max));
        _canvasMin = _camera.WorldToScreenPoint(canvasRT.TransformPoint(canvasRT.rect.min));
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        _offset = (Vector2)rectTransformToMove.localPosition - eventData.position;
    }

    public void OnDrag(PointerEventData eventData)
    {
        rectTransformToMove.localPosition = clampToScreen ? ClampToScreen(eventData.position + _offset) : eventData.position + _offset;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
    }

    private Vector2 ClampToScreen(Vector2 position)
    {
        Vector2 size = _camera.WorldToScreenPoint(rectTransformToMove.rect.size * rectTransformToMove.lossyScale);

        Vector2 min = new Vector2(_canvasMin.x + size.x / 2f + clampToScreenPadding.x, _canvasMin.y + size.y / 2f + clampToScreenPadding.y);
        Vector2 max = new Vector2(_canvasMax.x - size.x / 2f - clampToScreenPadding.x, _canvasMax.y - size.y / 2f - clampToScreenPadding.y);

        return new Vector2(
            Mathf.Clamp(position.x, min.x, max.x),
            Mathf.Clamp(position.y, min.y, max.y)
        );
    }
}
