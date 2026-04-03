using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StatsPrefab : MonoBehaviour
{
    [Header("Grid")]

    [SerializeField] internal Image WinColor;
    [SerializeField] internal GameObject HighBg;

    [SerializeField] internal TMP_Text DiceTotal;
    [SerializeField] internal Image DiceOne;
    [SerializeField] internal Image DiceTwo;
    [SerializeField] internal GameObject Star;
    internal int WinStats;
    internal string winner;

    internal void SetData(string diceTotal, Sprite diceOne, Sprite diceTwo, Sprite colorindex, bool isStar, bool isHighLited)
    {
        DiceTotal.text = diceTotal;
        DiceOne.sprite = diceOne;
        DiceTwo.sprite = diceTwo;
        if (HighBg) HighBg.SetActive(isHighLited);
        if (Star) Star.SetActive(isStar);

        WinColor.sprite = colorindex;
        // WinStats = int.Parse(diceTotal);
    }
    internal void CopyFrom(StatsPrefab other)
    {
        DiceTotal.text = other.DiceTotal.text;
        DiceOne.sprite = other.DiceOne.sprite;
        DiceTwo.sprite = other.DiceTwo.sprite;

        HighBg.SetActive(other.HighBg.activeSelf);
        Star.SetActive(other.Star.activeSelf);

        WinColor.sprite = other.WinColor.sprite;
    }

}

