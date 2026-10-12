using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

// The purple border shared by leaderboard avatars and bet options
public static class BorderHighlight
{
  internal static void Show(Image border, BorderHighlightSettings settings)
  {
    if (!border) return;

    DOTween.Kill(border);
    border.gameObject.SetActive(true);
    SetAlpha(border, 0f);
    border.rectTransform.localScale = Vector3.one * settings.startScale;

    Sequence seq = DOTween.Sequence().SetTarget(border);
    seq.Append(border.DOFade(1f, settings.inDuration));
    seq.Join(border.rectTransform.DOScale(1f, settings.inDuration).SetEase(settings.inEase));
    seq.AppendCallback(() =>
    {
      // Endless, so it cannot live inside the sequence
      border.DOFade(settings.pulseMinAlpha, settings.pulseDuration)
        .SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetTarget(border);
    });
  }

  // Fades out from wherever the border is, even part-way through its scale down
  internal static void Hide(Image border, BorderHighlightSettings settings, Action onHidden = null)
  {
    if (!border || !border.gameObject.activeSelf) return;

    DOTween.Kill(border);
    border.rectTransform.localScale = Vector3.one;
    border.DOFade(0f, settings.outDuration).SetTarget(border).OnComplete(() =>
    {
      border.gameObject.SetActive(false);
      onHidden?.Invoke();
    });
  }

  internal static void HideInstant(Image border)
  {
    if (!border) return;

    DOTween.Kill(border);
    border.rectTransform.localScale = Vector3.one;
    SetAlpha(border, 0f);
    border.gameObject.SetActive(false);
  }

  static void SetAlpha(Image border, float alpha)
  {
    Color color = border.color;
    color.a = alpha;
    border.color = color;
  }
}

[Serializable]
public class BorderHighlightSettings
{
  [Header("In")]
  [SerializeField] internal float startScale = 1.2f;
  [SerializeField] internal float inDuration = 0.3f;
  [SerializeField] internal Ease inEase = Ease.OutQuad;

  [Header("Pulse")]
  [Range(0f, 1f)] [SerializeField] internal float pulseMinAlpha = 0f;
  [SerializeField] internal float pulseDuration = 0.5f;

  [Header("Out")]
  [SerializeField] internal float outDuration = 0.2f;
}
