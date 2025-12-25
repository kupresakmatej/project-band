using UnityEngine;
using UnityEngine.EventSystems;

public class CardHoverHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler //, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private int cardIndex;
    private CardUIBehaviour cardUIBehaviour;
    private bool isHovered = false;
    private bool isAnimating = false;

    private Vector2 originalPosition; // Store the original position for resetting
    private Quaternion originalRotation; // Store the original rotation for resetting
    private Vector2 dragOffset;       // Store the offset
    private RectTransform rectTransform;
    private Canvas canvas;

    public void Initialize(int index, CardUIBehaviour uiBehaviour)
    {
        cardIndex = index;
        cardUIBehaviour = uiBehaviour;
        rectTransform = GetComponent<RectTransform>();
        canvas = uiBehaviour.GetCanvas(); // Reference canvas for drag operations
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (isHovered || isAnimating || cardUIBehaviour == null) return;

        isHovered = true;
        cardUIBehaviour.OnCardHovered(cardIndex);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!isHovered || isAnimating || cardUIBehaviour == null) return;

        isHovered = false;
        cardUIBehaviour.OnCardHoverExit(cardIndex);
    }

    public void SetAnimationState(bool animating)
    {
        isAnimating = animating;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (isAnimating || cardUIBehaviour == null) return;
        cardUIBehaviour.OnCardClicked(cardIndex); // Trigger click selection
    }

    /* Commented out drag-and-drop functionality as requested
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (isAnimating || rectTransform == null) return;

        originalPosition = rectTransform.anchoredPosition; // Save position
        originalRotation = rectTransform.rotation; // Save rotation
        // Calculate and store the offset from the card's position to the mouse position when dragging starts
        Vector2 initialLocalPosition;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out initialLocalPosition);

        dragOffset = rectTransform.anchoredPosition - initialLocalPosition;

        cardUIBehaviour.OnCardDragStart(cardIndex);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (rectTransform == null || canvas == null) return;

        Vector2 dragPosition;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out dragPosition);

        // Apply the drag offset to the new drag position
        rectTransform.anchoredPosition = dragPosition + dragOffset;
        rectTransform.rotation = Quaternion.Euler(0f, 0f, 0f);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (rectTransform == null) return;

        bool droppedOnTarget = cardUIBehaviour.TryDropCard(cardIndex, rectTransform.position);

        if (!droppedOnTarget)
        {
            rectTransform.anchoredPosition = originalPosition; // Reset to original position
            rectTransform.rotation = originalRotation;
        }

        cardUIBehaviour.OnCardDragEnd(cardIndex);
    }
    */
}