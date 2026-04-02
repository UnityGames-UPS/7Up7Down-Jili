
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Chip : MonoBehaviour
{
    [SerializeField] internal Image chipImage;
    [SerializeField] internal TMP_Text Chiptext;

    internal int chipIndex;

    internal void SetData(Sprite chip, string amount, int ChipIndex)
    {
        chipImage.sprite = chip;
        Chiptext.text = amount;
        chipIndex = ChipIndex;
    }
}
