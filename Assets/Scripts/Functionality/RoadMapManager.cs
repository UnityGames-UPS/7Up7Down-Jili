using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RoadMapManager : MonoBehaviour
{
  [Header("Popup")]
  [SerializeField] private CanvasGroup popupGroup;
  [SerializeField] private Button openButton;
  [SerializeField] private Button openButton2;
  [SerializeField] private Button closeButton;
  [SerializeField] private Button backgroundButton;
  [SerializeField] private float fadeDuration = 0.2f;
  [SerializeField] private AudioManager audioManager;

  [Header("Top Strip")]
  // Fills from the last slot upward: the last slot is the newest result
  [SerializeField] private List<StatsPrefab> topStats;

  [Header("Grids")]
  [SerializeField] private DiceStatsPrefab cellPrefab;
  // Both parents lay their children out column by column, top to bottom
  [SerializeField] private Transform streakGridParent;
  [SerializeField] private Transform allGridParent;
  [SerializeField] private int rows = 7;
  [SerializeField] private int columns = 15;

  [Header("Sprites")]
  [SerializeField] private List<Sprite> diceSprites;
  // 2-6, 7, 8-12
  [SerializeField] private List<Sprite> resultColors;

  [Header("Percentages")]
  [SerializeField] private TMP_Text twoSixPercentage;
  [SerializeField] private TMP_Text sevenPercentage;
  [SerializeField] private TMP_Text eightTwelvePercentage;
  [SerializeField] private TMP_Text totalRounds;

  // Oldest first
  private readonly List<DiceData> results = new List<DiceData>();
  private readonly List<DiceStatsPrefab> streakCells = new List<DiceStatsPrefab>();
  private readonly List<DiceStatsPrefab> allCells = new List<DiceStatsPrefab>();

  private int Capacity => rows * columns;

  private void Awake()
  {
    if (openButton) openButton.onClick.AddListener(Open);
    if (openButton2) openButton2.onClick.AddListener(Open);
    if (closeButton) closeButton.onClick.AddListener(Close);
    if (backgroundButton) backgroundButton.onClick.AddListener(Close);

    popupGroup.alpha = 0f;
    popupGroup.blocksRaycasts = false;
    popupGroup.gameObject.SetActive(false);
  }

  internal void Load(List<string> stats)
  {
    results.Clear();
    if (stats != null)
    {
      int invalid = 0;
      string firstInvalid = null;
      foreach (string statJson in stats)
      {
        DiceData data = JsonUtility.FromJson<DiceData>(statJson);
        // The server can send 0/0 entries for rounds that have no dice result
        if (data != null && IsDie(data.dice1) && IsDie(data.dice2))
        {
          results.Add(data);
        }
        else
        {
          invalid++;
          if (firstInvalid == null) firstInvalid = statJson;
        }
      }

      if (invalid > 0)
        Debug.LogError("[JOIN_LEVEL] stats: skipped " + invalid + " of " + stats.Count
            + " entries with dice outside 1-6, e.g. " + firstInvalid);
    }

    TrimToCapacity();
    Render();
  }

  internal void Add(DiceData result)
  {
    results.Add(result);
    TrimToCapacity();
    Render();
  }

  internal bool TryGetLast(out DiceData result)
  {
    result = results.Count > 0 ? results[results.Count - 1] : null;
    return result != null;
  }

  private void Open()
  {
    if (audioManager) audioManager.PlayButtonAudio();
    popupGroup.DOKill();
    popupGroup.gameObject.SetActive(true);
    popupGroup.blocksRaycasts = true;
    popupGroup.DOFade(1f, fadeDuration);
  }

  private void Close()
  {
    if (audioManager) audioManager.PlayButtonAudio();
    popupGroup.DOKill();
    // Stops blocking clicks as soon as the close starts, not when the fade ends
    popupGroup.blocksRaycasts = false;
    popupGroup.DOFade(0f, fadeDuration).OnComplete(() => popupGroup.gameObject.SetActive(false));
  }

  private void Render()
  {
    SpawnCells(streakCells, streakGridParent);
    SpawnCells(allCells, allGridParent);

    RenderTopStrip();
    RenderAllGrid();
    RenderStreakGrid();
    RenderPercentages();
  }

  private void RenderTopStrip()
  {
    for (int age = 0; age < topStats.Count; age++)
    {
      StatsPrefab slot = topStats[topStats.Count - 1 - age];
      int index = results.Count - 1 - age;
      slot.gameObject.SetActive(index >= 0);
      if (index < 0) continue;

      DiceData result = results[index];
      int total = result.dice1 + result.dice2;
      slot.SetData(total.ToString(), diceSprites[result.dice1 - 1], diceSprites[result.dice2 - 1],
          resultColors[Category(total)], result.isBonus, age == 0);
    }
  }

  private void RenderAllGrid()
  {
    for (int i = 0; i < allCells.Count; i++)
    {
      if (i < results.Count) SetCell(allCells[i], results[i]);
      else allCells[i].Clear();
    }
  }

  // Newest streak is the left column, newest result on top of each column
  private void RenderStreakGrid()
  {
    foreach (DiceStatsPrefab cell in streakCells) cell.Clear();

    int column = 0;
    int row = 0;
    int category = -1;
    for (int i = results.Count - 1; i >= 0; i--)
    {
      int resultCategory = Category(results[i].dice1 + results[i].dice2);
      bool isFirst = i == results.Count - 1;
      if (!isFirst && (resultCategory != category || row == rows))
      {
        column++;
        row = 0;
      }
      if (column == columns) break;

      category = resultCategory;
      SetCell(streakCells[column * rows + row], results[i]);
      row++;
    }
  }

  private void RenderPercentages()
  {
    int[] counts = new int[3];
    foreach (DiceData result in results) counts[Category(result.dice1 + result.dice2)]++;

    int total = results.Count;
    if (twoSixPercentage) twoSixPercentage.text = Percent(counts[0], total);
    if (sevenPercentage) sevenPercentage.text = Percent(counts[1], total);
    if (eightTwelvePercentage) eightTwelvePercentage.text = Percent(counts[2], total);
    if (totalRounds) totalRounds.text = "Calculated from last " + total + " rounds.";
  }

  private void SetCell(DiceStatsPrefab cell, DiceData result)
  {
    int total = result.dice1 + result.dice2;
    cell.SetData(total, resultColors[Category(total)], result.isBonus);
  }

  private void SpawnCells(List<DiceStatsPrefab> cells, Transform parent)
  {
    while (cells.Count < Capacity)
    {
      DiceStatsPrefab cell = Instantiate(cellPrefab, parent);
      cell.Clear();
      cells.Add(cell);
    }
  }

  private void TrimToCapacity()
  {
    if (results.Count > Capacity) results.RemoveRange(0, results.Count - Capacity);
  }

  private static bool IsDie(int value) => value >= 1 && value <= 6;

  // 0 = 2-6, 1 = 7, 2 = 8-12
  private static int Category(int total) => total < 7 ? 0 : total == 7 ? 1 : 2;

  private static string Percent(int count, int total) =>
      (total == 0 ? 0f : count * 100f / total).ToString("0") + "%";
}
