using System;
using System.Text;
using DG.Tweening;
using TMPro;
using UnityEngine;

public class BonusPrefab : MonoBehaviour
{
  // index of the 'X' in the text's sprite asset; digits are 0-9
  private const int XSpriteIndex = 10;

  // Each animation is its own child object; only one is active at a time
  [SerializeField] private ImageAnimation BgAnimation;
  [SerializeField] private ImageAnimation GlowAnimation;
  [SerializeField] private ImageAnimation RotationAnimation;
  [SerializeField] private TMP_Text MultiplierText;

  private readonly StringBuilder spriteTags = new StringBuilder();
  private CanvasGroup group;

  CanvasGroup Group
  {
    get
    {
      if (!group) group = GetComponent<CanvasGroup>();
      if (!group) group = gameObject.AddComponent<CanvasGroup>();
      return group;
    }
  }

  // Invisible while it waits for its turn in a staggered reveal
  internal void Hide() => Group.alpha = 0f;

  internal void Show(int multiplier, BonusAnimationSettings settings)
  {
    ShowOnly(BgAnimation);
    BgAnimation.Play();

    spriteTags.Clear();
    foreach (char digit in multiplier.ToString()) AppendSprite(digit - '0');
    AppendSprite(XSpriteIndex);

    MultiplierText.text = spriteTags.ToString();

    Group.DOFade(1f, settings.fadeInDuration).SetTarget(this);
    MultiplierText.rectTransform.localScale = Vector3.zero;
    MultiplierText.rectTransform.DOScale(1f, settings.textPopDuration).SetEase(settings.textPopEase).SetTarget(this);
  }

  // Glow loops, then the rotation; onDone fires after the rotation's last frame
  internal void PlayWin(BonusAnimationSettings settings, Action onDone)
  {
    FinishReveal();

    if (!GlowAnimation || settings.glowLoops <= 0)
    {
      PlayRotation(settings, onDone);
      return;
    }

    ShowOnly(GlowAnimation);
    int played = 0;
    Action onLoop = null;
    onLoop = () =>
    {
      if (++played < settings.glowLoops) GlowAnimation.Play(onLoop);
      else PlayRotation(settings, onDone);
    };
    GlowAnimation.Play(onLoop);
  }

  internal void Dismiss(BonusAnimationSettings settings, Action onDone)
  {
    FinishReveal();
    transform.DOScale(0f, settings.loseShrinkDuration).SetEase(Ease.InBack).SetTarget(this)
      .OnComplete(() => onDone?.Invoke());
  }

  internal void ResetState()
  {
    DOTween.Kill(this);
    foreach (var animation in new[] { BgAnimation, GlowAnimation, RotationAnimation })
    {
      if (animation) animation.StopAnimation();
    }
    MultiplierText.rectTransform.localScale = Vector3.one;
    MultiplierText.rectTransform.localRotation = Quaternion.identity;
    Group.alpha = 1f;
  }

  void PlayRotation(BonusAnimationSettings settings, Action onDone)
  {
    if (!RotationAnimation)
    {
      onDone?.Invoke();
      return;
    }

    ShowOnly(RotationAnimation);
    RectTransform text = MultiplierText.rectTransform;
    bool shrinking = false;

    // Driven by sprite frames, not time, so the text stays locked to the coin
    RotationAnimation.Play(frame =>
    {
      float turned = Mathf.InverseLerp(settings.rotationStartFrame, settings.rotationEndFrame, frame);
      text.localEulerAngles = settings.textRotation * turned;

      // 0 means the frame has not been set yet
      if (shrinking || settings.scaleDownStartFrame <= 0 || frame < settings.scaleDownStartFrame) return;
      shrinking = true;
      text.DOScale(0f, settings.textScaleDownDuration).SetEase(Ease.Linear).SetTarget(this);
    }, onDone);
  }

  void FinishReveal()
  {
    DOTween.Kill(this);
    Group.alpha = 1f;
    MultiplierText.rectTransform.localScale = Vector3.one;
  }

  void ShowOnly(ImageAnimation shown)
  {
    foreach (var animation in new[] { BgAnimation, GlowAnimation, RotationAnimation })
    {
      if (animation) animation.gameObject.SetActive(animation == shown);
    }
  }

  void AppendSprite(int index)
  {
    spriteTags.Append("<sprite=").Append(index).Append('>');
  }
}
