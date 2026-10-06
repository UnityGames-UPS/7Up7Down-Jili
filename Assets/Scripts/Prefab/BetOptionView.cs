using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class BetOptionView : MonoBehaviour
{
  [SerializeField] private Button button;
  [SerializeField] private GameObject dimOverlay;

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

  [Header("Animations")]
  [SerializeField] private ImageAnimation winAnimation;
  [SerializeField] private ImageAnimation leaderboardHighlight;

  internal event Action<BetOptionView> Clicked;

  internal string BetKey { get; private set; }
  internal string BetType { get; private set; }
  internal int PlayerBet { get; private set; }
  internal int OpponentBet { get; private set; }

  internal bool HasPlayerBet => PlayerBet > 0;
  internal ChipReference PlayerReferenceChip => new ChipReference(playerReferenceChip.rectTransform, playerReferenceChipText);
  internal ChipReference OpponentReferenceChip => new ChipReference(opponentReferenceChip.rectTransform, opponentReferenceChipText);

  void Awake()
  {
    if (button)
    {
      button.onClick.RemoveAllListeners();
      button.onClick.AddListener(() => Clicked?.Invoke(this));
    }
    ClearAllBets();
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

  internal void ClearPlayerBet()
  {
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
    if (dimOverlay) dimOverlay.SetActive(dimmed);
  }

  internal void PlayWin()
  {
    if (!winAnimation) return;
    winAnimation.gameObject.SetActive(true);
    winAnimation.StopAnimation();
    winAnimation.StartAnimation();
  }

  internal void StopWin()
  {
    if (winAnimation) winAnimation.StopAnimation();
  }

  internal void ShowLeaderboardHighlight()
  {
    if (leaderboardHighlight) leaderboardHighlight.gameObject.SetActive(true);
  }

  static void ShowReferenceChip(Image chip, TMP_Text label, int total, Sprite sprite)
  {
    if (label) label.text = total.ToString();
    if (!chip) return;

    chip.sprite = total > 0 ? sprite : null;
    chip.gameObject.SetActive(total > 0);
  }
}
