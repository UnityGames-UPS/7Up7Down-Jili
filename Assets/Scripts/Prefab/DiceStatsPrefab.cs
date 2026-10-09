using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class DiceStatsPrefab : MonoBehaviour
{
    [Header("Grid")]

    [SerializeField] internal Image WinColor;

    [SerializeField] internal TMP_Text DiceTotal;
    [SerializeField] internal GameObject Star;

    internal void SetData(int diceTotal, Sprite winColor, bool isStar)
    {
        DiceTotal.text = diceTotal.ToString();
        if (Star) Star.SetActive(isStar);

        WinColor.sprite = winColor;
        WinColor.enabled = true;
    }

    // Stays active so the grid layout keeps its slot
    internal void Clear()
    {
        DiceTotal.text = "";
        if (Star) Star.SetActive(false);
        WinColor.enabled = false;
    }
}
