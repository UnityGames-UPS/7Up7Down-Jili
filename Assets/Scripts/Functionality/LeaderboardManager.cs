using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LeaderboardManager : MonoBehaviour
{
  // Rank 1 first
  [SerializeField] private List<LeaderboardAvatar> slots;
  // Shown only while at least one avatar is
  [SerializeField] private GameObject title;
  // The player takes one at random; the rest are shared out among the leaderboard
  [SerializeField] private List<Sprite> avatarSprites;
  [SerializeField] private BorderHighlightSettings highlightSettings = new BorderHighlightSettings();

  internal event Action<string> AvatarClicked;

  internal Sprite PlayerAvatar { get; private set; }
  internal BorderHighlightSettings HighlightSettings => highlightSettings;

  // Only users currently on the board, so a sprite frees up when its user drops off
  private readonly Dictionary<string, Sprite> avatarByUser = new Dictionary<string, Sprite>();

  void Awake()
  {
    if (avatarSprites.Count > 0) PlayerAvatar = avatarSprites[UnityEngine.Random.Range(0, avatarSprites.Count)];

    foreach (var slot in slots)
    {
      slot.Clicked += s => AvatarClicked?.Invoke(s.Username);
      slot.Clear();
    }
    if (title) title.SetActive(false);
  }

  internal void SetWinners(List<Winner> winners, string playerName)
  {
    List<Winner> shown = winners == null
        ? new List<Winner>()
        : winners.OrderBy(w => w.rank).Take(slots.Count).ToList();

    foreach (string gone in avatarByUser.Keys.Where(u => shown.All(w => w.username != u)).ToList())
      avatarByUser.Remove(gone);

    if (title) title.SetActive(shown.Count > 0);

    for (int i = 0; i < slots.Count; i++)
    {
      if (i >= shown.Count)
      {
        slots[i].Clear();
        continue;
      }

      Winner winner = shown[i];
      // A user can move rank, and the border belongs to whoever was here before
      if (slots[i].Username != winner.username) slots[i].Clear();
      slots[i].SetData(winner.username, winner.totalWins.ToString("0.##"), AvatarFor(winner.username, playerName));
    }
  }

  internal bool TryGetChipAnchor(string username, out Transform anchor)
  {
    LeaderboardAvatar slot = FindSlot(username);
    anchor = slot ? slot.ChipAnchor : null;
    return slot;
  }

  internal void ShowHighlight(string username)
  {
    LeaderboardAvatar slot = FindSlot(username);
    if (slot) slot.ShowHighlight(highlightSettings);
  }

  internal void HideHighlight()
  {
    foreach (var slot in slots) slot.HideHighlight(highlightSettings);
  }

  LeaderboardAvatar FindSlot(string username)
  {
    if (string.IsNullOrEmpty(username)) return null;
    return slots.FirstOrDefault(s => s.Username == username);
  }

  Sprite AvatarFor(string username, string playerName)
  {
    if (username == playerName) return PlayerAvatar;
    if (avatarByUser.TryGetValue(username, out Sprite kept)) return kept;

    List<Sprite> others = avatarSprites.Where(s => s != PlayerAvatar).ToList();
    List<Sprite> free = others.Where(s => !avatarByUser.ContainsValue(s)).ToList();
    // Too few sprites to keep everyone distinct: repeat one rather than show the player's
    List<Sprite> pool = free.Count > 0 ? free : others;
    if (pool.Count == 0) return PlayerAvatar;

    Sprite picked = pool[UnityEngine.Random.Range(0, pool.Count)];
    avatarByUser[username] = picked;
    return picked;
  }
}
