using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class BetOptionView : MonoBehaviour
{
  [SerializeField] private Button button;
  [SerializeField] private GameObject dimOverlay;
  [SerializeField] private float dimFade = 0.2f;

  [Header("Labels")]
  [SerializeField] private TMP_Text nameText;
  [SerializeField] private TMP_Text payoutText;
  [SerializeField] private GameObject playerBetLabel;
  [SerializeField] private TMP_Text playerBetText;

  [Header("Reference Chips")]
  [SerializeField] private Image playerReferenceChip;
  [SerializeField] private TMP_Text playerReferenceChipText;
  [SerializeField] private Image opponentReferenceChip;
  [SerializeField] private TMP_Text opponentReferenceChipText;

  [Header("Win Layer")]
  // Moved onto the shared win layer while it plays, so it draws above the table
  [SerializeField] private RectTransform winLayer;
  [SerializeField] private Image winBorder;
  [SerializeField] private RectTransform winText;
  [SerializeField] private ImageAnimation winAnimation;

  [Header("Animations")]
  // [SerializeField] private ImageAnimation leaderboardHighlight;
  // Where this option's bonus multiplier badge sits; the option itself when unset
  [SerializeField] private Transform bonusAnchor;

  internal event Action<BetOptionView> Clicked;

  internal string BetKey { get; private set; }
  internal string BetType { get; private set; }
  internal int PlayerBet { get; private set; }
  internal int OpponentBet { get; private set; }

  internal bool HasPlayerBet => PlayerBet > 0;
  internal ChipReference PlayerReferenceChip => new ChipReference(playerReferenceChip.rectTransform, playerReferenceChipText);
  internal Transform BonusAnchor => bonusAnchor ? bonusAnchor : transform;
  internal ChipReference OpponentReferenceChip => new ChipReference(opponentReferenceChip.rectTransform, opponentReferenceChipText);

  private Transform winLayerHome;
  private int winLayerHomeIndex;
  private CanvasGroup dimGroup;
  private bool isDimmed;
  private Sequence shrink;
  private Vector3 playerChipScale = Vector3.one;
  private Vector3 opponentChipScale = Vector3.one;

  void Awake()
  {
    if (playerReferenceChip) playerChipScale = playerReferenceChip.rectTransform.localScale;
    if (opponentReferenceChip) opponentChipScale = opponentReferenceChip.rectTransform.localScale;
    if (dimOverlay)
    {
      // Fades the overlay as a whole, whatever alpha its image was authored with
      dimGroup = dimOverlay.GetComponent<CanvasGroup>();
      if (!dimGroup) dimGroup = dimOverlay.AddComponent<CanvasGroup>();
      dimGroup.alpha = 0f;
      dimOverlay.SetActive(false);
    }
    if (button)
    {
      button.onClick.RemoveAllListeners();
      button.onClick.AddListener(() => Clicked?.Invoke(this));
    }
    ClearAllBets();
    if (winLayer) winLayer.gameObject.SetActive(false);
  }

  void OnDestroy()
  {
    DOTween.Kill(this);
    shrink?.Kill();
    if (dimGroup) dimGroup.DOKill();
  }

  internal void Setup(string betKey, string betType, string label, List<int> payout)
  {
    BetKey = betKey;
    BetType = betType;
    if (nameText) nameText.text = label;
    if (payoutText) payoutText.text = payout[0] + " : " + payout[1];
  }

  internal void AddPlayerBet(int delta)
  {
    PlayerBet = Mathf.Max(0, PlayerBet + delta);
    if (playerBetText) playerBetText.text = PlayerBet.ToString();
    if (playerBetLabel) playerBetLabel.SetActive(HasPlayerBet);
  }

  internal void AddOpponentBet(int delta)
  {
    OpponentBet = Mathf.Max(0, OpponentBet + delta);
  }

  // Safe to call any time: the reference chip always mirrors the current total
  internal void ShowPlayerChip(Sprite sprite) =>
    ShowReferenceChip(playerReferenceChip, playerReferenceChipText, PlayerBet, sprite);

  internal void ShowOpponentChip(Sprite sprite) =>
    ShowReferenceChip(opponentReferenceChip, opponentReferenceChipText, OpponentBet, sprite);

  // The reference chip shows a payout without it counting as a bet
  internal void ShowPlayerWin(int amount, Sprite sprite) =>
    ShowReferenceChip(playerReferenceChip, playerReferenceChipText, amount, sprite);

  internal void ShowOpponentWin(int amount, Sprite sprite) =>
    ShowReferenceChip(opponentReferenceChip, opponentReferenceChipText, amount, sprite);

  // Losing chips shrink away before the option is cleared
  internal void ShrinkBets(float duration, Action onCleared)
  {
    StopShrink();

    bool playerShown = playerReferenceChip && playerReferenceChip.gameObject.activeSelf;
    bool opponentShown = opponentReferenceChip && opponentReferenceChip.gameObject.activeSelf;
    if (!playerShown && !opponentShown)
    {
      ClearAllBets();
      onCleared?.Invoke();
      return;
    }

    Sequence seq = DOTween.Sequence();
    if (playerShown) seq.Join(playerReferenceChip.rectTransform.DOScale(0f, duration).SetEase(Ease.InBack));
    if (opponentShown) seq.Join(opponentReferenceChip.rectTransform.DOScale(0f, duration).SetEase(Ease.InBack));

    shrink = seq;
    seq.OnComplete(() =>
    {
      shrink = null;
      ClearAllBets();
      onCleared?.Invoke();
    });
  }

  void StopShrink()
  {
    shrink?.Kill();
    shrink = null;
    if (playerReferenceChip) playerReferenceChip.rectTransform.localScale = playerChipScale;
    if (opponentReferenceChip) opponentReferenceChip.rectTransform.localScale = opponentChipScale;
  }

  internal void ClearPlayerBet()
  {
    StopShrink();
    PlayerBet = 0;
    if (playerBetText) playerBetText.text = "0";
    if (playerBetLabel) playerBetLabel.SetActive(false);
    ShowPlayerChip(null);
  }

  internal void ClearAllBets()
  {
    ClearPlayerBet();
    OpponentBet = 0;
    ShowOpponentChip(null);
  }

  internal void SetDimmed(bool dimmed)
  {
    if (!dimGroup || dimmed == isDimmed) return;
    isDimmed = dimmed;

    dimGroup.DOKill();
    if (dimmed) dimOverlay.SetActive(true);
    dimGroup.DOFade(dimmed ? 1f : 0f, dimFade).OnComplete(() =>
    {
      if (!dimmed) dimOverlay.SetActive(false);
    });
  }

  internal void PlayWin(Transform animLayer, WinAnimationSettings settings)
  {
    if (!winLayer) return;
    StopWin();

    winLayerHome = winLayer.parent;
    winLayerHomeIndex = winLayer.GetSiblingIndex();
    if (animLayer) winLayer.SetParent(animLayer, true);
    winLayer.gameObject.SetActive(true);

    if (winText) winText.localScale = Vector3.zero;

    Sequence seq = DOTween.Sequence().SetTarget(this);

    if (winAnimation)
    {
      winAnimation.gameObject.SetActive(true);
      winAnimation.Play(() =>
      {
        winAnimation.gameObject.SetActive(false);
        // Finishes the pop first if the animation is the shorter of the two
        if (seq.IsActive()) seq.Complete(true);
        if (winText) winText.DOScale(0f, settings.textHideDuration).SetEase(settings.textHideEase).SetTarget(this);
      });
    }

    if (!winBorder) return;

    SetBorderAlpha(0f);
    winBorder.rectTransform.localScale = Vector3.one * settings.borderStartScale;

    seq.Append(winBorder.DOFade(1f, settings.borderInDuration));
    seq.Join(winBorder.rectTransform.DOScale(1f, settings.borderInDuration).SetEase(settings.borderInEase));
    seq.AppendCallback(() =>
    {
      // Endless, so it cannot live inside the sequence
      winBorder.DOFade(settings.pulseMinAlpha, settings.pulseDuration)
        .SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetTarget(this);
    });
    if (winText) seq.Append(winText.DOScale(1f, settings.textPopDuration).SetEase(settings.textPopEase));
  }

  internal void StopWin()
  {
    DOTween.Kill(this);
    if (winAnimation) winAnimation.StopAnimation();
    if (!winLayer) return;

    if (winLayerHome)
    {
      winLayer.SetParent(winLayerHome, true);
      winLayer.SetSiblingIndex(winLayerHomeIndex);
      winLayerHome = null;
    }
    winLayer.gameObject.SetActive(false);
  }

  void SetBorderAlpha(float alpha)
  {
    Color color = winBorder.color;
    color.a = alpha;
    winBorder.color = color;
  }

  // internal void ShowLeaderboardHighlight()
  // {
  //   if (leaderboardHighlight) leaderboardHighlight.gameObject.SetActive(true);
  // }

  static void ShowReferenceChip(Image chip, TMP_Text label, int total, Sprite sprite)
  {
    if (label) label.text = total.ToString();
    if (!chip) return;

    chip.sprite = total > 0 ? sprite : null;
    chip.gameObject.SetActive(total > 0);
  }
}

// Shared tuning for every option's win layer; lives on GameManager
[Serializable]
public class WinAnimationSettings
{
  [Header("Border In")]
  public float borderStartScale = 1.3f;
  public float borderInDuration = 0.35f;
  public Ease borderInEase = Ease.OutQuad;

  [Header("Border Pulse")]
  [Range(0f, 1f)] public float pulseMinAlpha = 0f;
  public float pulseDuration = 0.5f;

  [Header("Win Text Pop")]
  public float textPopDuration = 0.3f;
  public Ease textPopEase = Ease.OutBack;
  // Played when the win image animation finishes
  public float textHideDuration = 0.2f;
  public Ease textHideEase = Ease.InBack;
}
