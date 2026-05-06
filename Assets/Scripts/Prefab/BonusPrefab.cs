using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEngine;
using UnityEngine.UI;

public class BonusPrefab : MonoBehaviour
{
    [SerializeField] private List<Sprite> NumbersSprites;
    [SerializeField] private GameObject NumbersPatrent;
    [SerializeField] private ImageAnimation BgAnimatio;
    [SerializeField] private List<Image> Numbers;



    public void SetNumberWithX(int value)
    {
        BgAnimatio.StartAnimation();
        // Convert number to string
        string numberStr = value.ToString();

        int totalLength = numberStr.Length + 1; // +1 for 'X'

        // Safety check
        if (totalLength > Numbers.Count)
        {
            Debug.LogWarning("Not enough UI slots for number display!");
            return;
        }

        // Disable all first
        for (int i = 0; i < Numbers.Count; i++)
        {
            Numbers[i].gameObject.SetActive(false);
        }

        // Set number digits
        for (int i = 0; i < numberStr.Length; i++)
        {
            int digit = numberStr[i] - '0';

            Numbers[i].sprite = NumbersSprites[digit];
            Numbers[i].gameObject.SetActive(true);
        }

        // Set 'X' at the end
        Numbers[numberStr.Length].sprite = NumbersSprites[10];
        Numbers[numberStr.Length].gameObject.SetActive(true);
    }
}
