using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LevelSelector : MonoBehaviour
{
  [Serializable]
  private class LevelButton
  {
    public Button button;
    public TMP_Text minText;
    public TMP_Text maxText;
  }

  [Header("Panel")]
  [SerializeField] private RectTransform panel;
  [SerializeField] private float openY = 0f;
  [SerializeField] private float closedY = -762f;
  [SerializeField] private float slideDuration = 0.3f;
  [SerializeField] private Button openButton;
  // Full-screen button behind the panel; only active while the panel is open
  [SerializeField] private Button backdropButton;

  [Header("Levels")]
  // Same order as the server's levels array
  [SerializeField] private List<LevelButton> levels;

  [Header("Mode")]
  [SerializeField] private Button multiplayerButton;
  [SerializeField] private Button singlePlayerButton;
  [SerializeField] private Sprite selectedSprite;
  [SerializeField] private Sprite notSelectedSprite;
  [SerializeField] private TMP_Text modeText;

  [SerializeField] private SocketIOManager socketManager;
  [SerializeField] private GameManager gameManager;
  [SerializeField] private UiManager uiManager;

  private bool isOpen;

  private void Awake()
  {
    if (openButton) openButton.onClick.AddListener(Toggle);
    if (backdropButton) backdropButton.onClick.AddListener(Close);
    multiplayerButton.onClick.AddListener(() => RequestMode("multiple"));
    singlePlayerButton.onClick.AddListener(() => RequestMode("single"));

    for (int i = 0; i < levels.Count; i++)
    {
      int index = i;
      levels[i].button.onClick.AddListener(() => OnLevelClicked(index));
    }

    panel.anchoredPosition = new Vector2(panel.anchoredPosition.x, closedY);
    if (backdropButton) backdropButton.gameObject.SetActive(false);
    ShowMode(false);
  }

  internal void Refresh(string currentLevel)
  {
    List<string> levelKeys = socketManager.initialData.levels;
    for (int i = 0; i < levels.Count; i++)
    {
      List<int> chips = i < levelKeys.Count ? gameManager.ChipsForLevel(levelKeys[i]) : null;
      bool available = chips != null && chips.Count > 0;
      levels[i].button.gameObject.SetActive(available);
      if (!available) continue;

      bool isCurrent = levelKeys[i] == currentLevel;
      levels[i].button.interactable = !isCurrent;
      levels[i].minText.text = uiManager.FormatNumber(chips[0]);
      levels[i].maxText.text = uiManager.FormatNumber(chips[chips.Count - 1]);
      levels[i].minText.color = levels[i].maxText.color = isCurrent ? Color.yellow : Color.white;
    }
  }

  internal void ShowMode(bool single)
  {
    if (modeText) modeText.text = single ? "Single Mode" : "Multiple Mode";
    singlePlayerButton.interactable = !single;
    multiplayerButton.interactable = single;
    singlePlayerButton.image.sprite = single ? selectedSprite : notSelectedSprite;
    multiplayerButton.image.sprite = single ? notSelectedSprite : selectedSprite;
  }

  private void OnLevelClicked(int index)
  {
    string level = socketManager.initialData.levels[index];
    if (level == gameManager.currentRoom) return;

    socketManager.SwitchLevel(level);
    Close();
  }

  // The mode UI only changes once the server confirms the switch
  private void RequestMode(string mode)
  {
    socketManager.SendModeSelection(mode);
    Close();
  }

  private void Toggle()
  {
    if (isOpen) Close();
    else Open();
  }

  private void Open() => Slide(true);

  private void Close() => Slide(false);

  private void Slide(bool open)
  {
    isOpen = open;
    if (backdropButton) backdropButton.gameObject.SetActive(open);
    panel.DOKill();
    panel.DOAnchorPosY(open ? openY : closedY, slideDuration).SetEase(open ? Ease.OutCubic : Ease.InCubic);
  }
}
