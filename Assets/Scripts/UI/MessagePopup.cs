using DG.Tweening;
using TMPro;
using UnityEngine;

public class MessagePopup : MonoBehaviour
{
  [SerializeField] private CanvasGroup canvasGroup;
  [SerializeField] private TMP_Text messageText;
  [SerializeField] private float fadeDuration = 0.2f;
  [SerializeField] private float hideAfterSeconds = 2f;

  private Tween fadeTween;
  private Tween hideTimer;
  private bool shown;

  void Awake()
  {
    // Must never eat clicks meant for the bet views
    canvasGroup.blocksRaycasts = false;
    canvasGroup.alpha = 0f;
  }

  // Repeated calls only refresh the text and the idle timer, so spamming never re-fades
  internal void Show(string message)
  {
    messageText.text = message;
    gameObject.SetActive(true);

    hideTimer?.Kill();
    hideTimer = DOVirtual.DelayedCall(hideAfterSeconds, Hide);

    if (shown) return;
    shown = true;
    fadeTween?.Kill();
    fadeTween = canvasGroup.DOFade(1f, fadeDuration);
  }

  internal void Hide()
  {
    hideTimer?.Kill();
    if (!shown) return;

    shown = false;
    fadeTween?.Kill();
    fadeTween = canvasGroup.DOFade(0f, fadeDuration).OnComplete(() => gameObject.SetActive(false));
  }

  void OnDisable()
  {
    hideTimer?.Kill();
    fadeTween?.Kill();
    shown = false;
    if(canvasGroup) canvasGroup.alpha = 0f;
  }
}
