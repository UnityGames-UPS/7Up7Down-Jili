using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LeaderboardAvatar : MonoBehaviour
{
  // Switched off while this rank is empty; this object when unset
  [SerializeField] private GameObject root;
  [SerializeField] private Image avatarImage;
  [SerializeField] private TMP_Text winningsText;
  [SerializeField] private Button button;
  [SerializeField] private Image highlightBorder;
  // Where this user's chips leave from and return to; this object when unset
  [SerializeField] private Transform chipAnchor;

  internal event Action<LeaderboardAvatar> Clicked;

  internal string Username { get; private set; }
  internal Transform ChipAnchor => chipAnchor ? chipAnchor : transform;

  GameObject Root => root ? root : gameObject;

  void Awake()
  {
    if (button)
    {
      button.onClick.RemoveAllListeners();
      button.onClick.AddListener(() => Clicked?.Invoke(this));
    }
    BorderHighlight.HideInstant(highlightBorder);
  }

  void OnDestroy()
  {
    if (highlightBorder) DOTween.Kill(highlightBorder);
  }

  internal void SetData(string username, string winnings, Sprite avatar)
  {
    Username = username;
    if (avatarImage) avatarImage.sprite = avatar;
    if (winningsText) winningsText.text = winnings;
    Root.SetActive(true);
  }

  internal void Clear()
  {
    Username = null;
    BorderHighlight.HideInstant(highlightBorder);
    Root.SetActive(false);
  }

  internal void ShowHighlight(BorderHighlightSettings settings) => BorderHighlight.Show(highlightBorder, settings);

  internal void HideHighlight(BorderHighlightSettings settings) => BorderHighlight.Hide(highlightBorder, settings);
}
