using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class BonusManager : GenericObjectPool<BonusPrefab>
{
  [Header("Reveal Animation")]
  [SerializeField] private float spawnStagger = 0.1f;
  // Badge scale on the three main bets; side bets keep the prefab's size
  [SerializeField] private float mainBetScale = 1.3f;
  [SerializeField] private BonusAnimationSettings settings = new BonusAnimationSettings();

  private readonly Dictionary<Transform, BonusPrefab> badgeByAnchor = new Dictionary<Transform, BonusPrefab>();

  // order staggers badges revealed in the same batch
  internal void Show(Transform anchor, int multiplier, int order, bool isMainBet)
  {
    BonusPrefab badge = GetFromPool();
    badge.transform.position = anchor.position;
    badge.transform.localScale = Vector3.one * (isMainBet ? mainBetScale : 1f);
    badge.Hide();
    badgeByAnchor[anchor] = badge;

    DOVirtual.DelayedCall(order * spawnStagger, () => badge.Show(multiplier, settings)).SetTarget(badge);
  }

  // Once the result is known: a winning badge plays out, a losing one shrinks away
  internal void Resolve(Transform anchor, bool won)
  {
    if (!badgeByAnchor.TryGetValue(anchor, out BonusPrefab badge)) return;
    badgeByAnchor.Remove(anchor);

    if (won) badge.PlayWin(settings, () => ReturnToPool(badge));
    else badge.Dismiss(settings, () => ReturnToPool(badge));
  }

  internal override void ReturnToPool(BonusPrefab badge)
  {
    badge.ResetState();
    base.ReturnToPool(badge);
  }

  internal override void ReturnAllItemsToPool()
  {
    foreach (BonusPrefab badge in ItemsInUse) badge.ResetState();
    badgeByAnchor.Clear();
    base.ReturnAllItemsToPool();
  }
}

// Tuning shared by every bonus badge; frame numbers index the rotation animation's sprite list
[Serializable]
public class BonusAnimationSettings
{
  [Header("Reveal")]
  public float fadeInDuration = 0.1f;
  public float textPopDuration = 0.25f;
  public Ease textPopEase = Ease.OutBack;

  [Header("Lost")]
  public float loseShrinkDuration = 0.25f;

  [Header("Won")]
  public int glowLoops = 2;
  public int rotationStartFrame;
  public int rotationEndFrame;
  // Total turn of the text between those two frames
  public Vector3 textRotation = new Vector3(0f, 720f, 0f);
  public int scaleDownStartFrame;
  public float textScaleDownDuration = 0.15f;
}
