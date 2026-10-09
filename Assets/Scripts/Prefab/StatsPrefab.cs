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

    internal void SetData(string diceTotal, Sprite diceOne, Sprite diceTwo, Sprite colorindex, bool isStar, bool isHighLited)
    {
        DiceTotal.text = diceTotal;
        DiceOne.sprite = diceOne;
        DiceTwo.sprite = diceTwo;
        if (HighBg) HighBg.SetActive(isHighLited);
        if (Star) Star.SetActive(isStar);

        WinColor.sprite = colorindex;
    }
}
