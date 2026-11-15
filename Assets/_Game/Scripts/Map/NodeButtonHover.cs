using UnityEngine;
using UnityEngine.EventSystems;

public class NodeButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    [Header("Hover Settings")]
    [SerializeField] private float hoverScale = 1.2f;
    [SerializeField] private float scaleSpeed = 8f;

    private Vector3 originalScale;
    private Vector3 targetScale;

    private void Awake()
    {
        originalScale = transform.localScale;
        targetScale = originalScale;
    }

    private void Update()
    {
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.unscaledDeltaTime * scaleSpeed);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        Grow();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Shrink();
    }

    public void OnSelect(BaseEventData eventData)
    {
        Grow();
    }

    public void OnDeselect(BaseEventData eventData)
    {
        Shrink();
    }

    private void Grow()
    {
        targetScale = originalScale * hoverScale;
    }

    private void Shrink()
    {
        targetScale = originalScale;
    }
}
