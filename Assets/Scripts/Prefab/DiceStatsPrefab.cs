
using UnityEngine;
using TMPro;
using UnityEngine.UI;
public class DiceStatsPrefab : MonoBehaviour
{
    [Header("Grid")]

    [SerializeField] internal UnityEngine.UI.Image WinColor;

    [SerializeField] internal TMP_Text DiceTotal;
    [SerializeField] internal GameObject Star;
    public int colorIndex;
    internal bool winner;
    internal string diceTotals;
    internal int WinStats;
    internal void SetData(string diceTotal, Sprite winColor, bool isStar)
    {
        DiceTotal.text = diceTotal;
        diceTotals = diceTotal;
        if (Star) Star.SetActive(isStar);

        WinColor.sprite = winColor;
        winner = isStar;
        WinStats = int.Parse(diceTotal ?? "0");
    }
    // internal void CopyFrom(StatsPrefab other)
    // {
    //     DiceTotal.text = other.DiceTotal.text;
    //     DiceOne.sprite = other.DiceOne.sprite;
    //     DiceTwo.sprite = other.DiceTwo.sprite;

    //     HighBg.SetActive(other.HighBg.activeSelf);
    //     Star.SetActive(other.Star.activeSelf);

    //     winColor = other.winColor;
    // }

}
