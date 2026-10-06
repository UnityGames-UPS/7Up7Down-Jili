using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;

// Bounces a button the moment it is pressed; does nothing while it is not interactable
[RequireComponent(typeof(Button))]
public class ButtonAnimator : MonoBehaviour, IPointerDownHandler
{
    [SerializeField] private float popScale = 1.1f;
    [SerializeField] private float popTime = 0.1f;

    private Button button;
    private Vector3 originalScale;
    private Tween bounce;

    private void Awake()
    {
        button = GetComponent<Button>();
        originalScale = transform.localScale;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!button.interactable) return;

        bounce?.Kill();
        transform.localScale = originalScale;
        bounce = DOTween.Sequence()
            .Append(transform.DOScale(originalScale * popScale, popTime).SetEase(Ease.OutBack))
            .Append(transform.DOScale(originalScale, popTime));
    }

    // A button hidden mid-bounce must not come back enlarged
    private void OnDisable()
    {
        bounce?.Kill();
        transform.localScale = originalScale;
    }
}
