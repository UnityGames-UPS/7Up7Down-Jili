using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class ChipManager : GenericObjectPool<Chip>
{
  [Header("Sprites")]
  // One per denomination, lowest first
  [SerializeField] private List<Sprite> playerChipSprites;
  [SerializeField] private Sprite opponentChipSprite;

  [Header("Points")]
  [SerializeField] private Transform opponentOrigin;
  [SerializeField] private Transform dealerPoint;

  [Header("Bet Animation")]
  [SerializeField] private float spawnHeight = 80f;
  [SerializeField] private float spawnScale = 1.2f;
  [SerializeField] private float dropDuration = 0.25f;
  [SerializeField] private float opponentFlyDuration = 0.4f;
  [SerializeField] private float opponentFadeIn = 0.02f;
  [SerializeField] private float settleDuration = 0.1f;
  [SerializeField] private float chipStagger = 0.05f;

  [Header("Payout Animation")]
  [SerializeField] private float dealerPayDuration = 1f;
  [SerializeField] private float collectDuration = 0.6f;
  [SerializeField] private float loseShrinkDuration = 0.3f;

  internal float LoseShrinkDuration => loseShrinkDuration;
  internal float CollectDuration => collectDuration;
  internal Transform OpponentOrigin => opponentOrigin;

  private List<int> denominations = new List<int>();

  internal void SetDenominations(List<int> roomChips)
  {
    denominations = new List<int>(roomChips);
    denominations.Sort();
  }

  internal Sprite GetPlayerSprite(int index) => playerChipSprites[index];

  // Player chips use the largest denomination that fits; opponents share one sprite
  internal Sprite GetSprite(int amount, bool isPlayer)
  {
    if (!isPlayer) return opponentChipSprite;

    int index = 0;
    for (int i = 0; i < denominations.Count && amount >= denominations[i]; i++)
      index = i;

    return playerChipSprites[Mathf.Min(index, playerChipSprites.Count - 1)];
  }

  internal List<int> BreakIntoChips(int amount)
  {
    List<int> chips = new List<int>();
    for (int i = denominations.Count - 1; i >= 0; i--)
    {
      while (amount >= denominations[i])
      {
        amount -= denominations[i];
        chips.Add(denominations[i]);
      }
    }
    return chips;
  }

  internal void DropPlayerChips(ChipReference reference, int amount, Action onLanded)
  {
    PlaceChips(reference, amount, true, reference.Rect.position, dropDuration, dropDuration, onLanded);
  }

  // Leaves from the bettor's leaderboard avatar when they have one
  internal void FlyOpponentChips(ChipReference reference, int amount, Transform origin, Action onLanded)
  {
    Vector3 start = (origin ? origin : opponentOrigin).position;
    PlaceChips(reference, amount, false, start, opponentFlyDuration, opponentFadeIn, onLanded);
  }

  // Payouts travel as one chip carrying the whole amount
  internal void PayFromDealer(ChipReference reference, int amount, bool isPlayer, Action onLanded)
  {
    Chip chip = Spawn(amount, isPlayer, reference, dealerPoint.position);
    MoveAndReturn(chip, reference.Rect.position, dealerPayDuration, Ease.InQuad, onLanded);
  }

  internal void CollectToPlayer(ChipReference reference, Vector3 target, int amount, bool isPlayer, Action onArrived = null)
  {
    Chip chip = Spawn(amount, isPlayer, reference, reference.Rect.position);
    MoveAndReturn(chip, target, collectDuration, Ease.OutQuad, onArrived);
  }

  internal override void ReturnToPool(Chip chip)
  {
    DOTween.Kill(chip);
    chip.SetAlpha(1f);
    base.ReturnToPool(chip);
  }

  internal override void ReturnAllItemsToPool()
  {
    foreach (Chip chip in ItemsInUse)
    {
      DOTween.Kill(chip);
      chip.SetAlpha(1f);
    }
    base.ReturnAllItemsToPool();
  }

  void PlaceChips(ChipReference reference, int amount, bool isPlayer, Vector3 start, float travelDuration, float fadeDuration, Action onLanded)
  {
    List<int> chips = BreakIntoChips(amount);
    for (int i = 0; i < chips.Count; i++)
    {
      Chip chip = Spawn(chips[i], isPlayer, reference, start);
      if (isPlayer) chip.Rect.localPosition += Vector3.up * spawnHeight;

      chip.Rect.localScale = Vector3.one * spawnScale;
      chip.SetAlpha(0f);

      Sequence seq = DOTween.Sequence().SetTarget(chip);
      seq.AppendInterval(i * chipStagger);
      seq.Append(chip.Rect.DOMove(reference.Rect.position, travelDuration).SetEase(Ease.OutQuad));
      seq.Join(DOVirtual.Float(0f, 1f, fadeDuration, chip.SetAlpha));
      seq.Append(chip.Rect.DOScale(1f, settleDuration));
      seq.OnComplete(() =>
      {
        onLanded?.Invoke();
        ReturnToPool(chip);
      });
    }
  }

  Chip Spawn(int amount, bool isPlayer, ChipReference reference, Vector3 position)
  {
    Chip chip = GetFromPool();
    chip.SetData(GetSprite(amount, isPlayer), amount.ToString(), Mathf.Max(0, denominations.IndexOf(amount)));
    chip.MatchReference(reference);
    chip.Rect.position = position;
    chip.Rect.localScale = Vector3.one;
    chip.SetAlpha(1f);
    return chip;
  }

  void MoveAndReturn(Chip chip, Vector3 target, float duration, Ease ease, Action onArrived)
  {
    chip.Rect.DOMove(target, duration).SetEase(ease).SetTarget(chip).OnComplete(() =>
    {
      onArrived?.Invoke();
      ReturnToPool(chip);
    });
  }
}
