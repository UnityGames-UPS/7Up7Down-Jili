
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Chip : MonoBehaviour
{
    [SerializeField] internal Image chipImage;
    [SerializeField] internal TMP_Text Chiptext;

    internal int chipIndex;
    internal string chipAmount;

    internal RectTransform Rect => (RectTransform)transform;

    internal void SetAlpha(float alpha)
    {
        Color color = chipImage.color;
        color.a = alpha;
        chipImage.color = color;
        Chiptext.alpha = alpha;
    }

    // Takes the reference chip's size and its text layout, so nothing snaps when the chip lands
    internal void MatchReference(ChipReference reference)
    {
        Rect.sizeDelta = reference.Rect.rect.size;
        if (!reference.Text) return;

        RectTransform source = reference.Text.rectTransform;
        RectTransform text = Chiptext.rectTransform;
        text.anchorMin = source.anchorMin;
        text.anchorMax = source.anchorMax;
        text.pivot = source.pivot;
        text.anchoredPosition = source.anchoredPosition;
        text.sizeDelta = source.sizeDelta;
        text.localRotation = source.localRotation;
        text.localScale = source.localScale;

        Chiptext.enableAutoSizing = reference.Text.enableAutoSizing;
        Chiptext.fontSizeMin = reference.Text.fontSizeMin;
        Chiptext.fontSizeMax = reference.Text.fontSizeMax;
        Chiptext.fontSize = reference.Text.fontSize;
    }

    internal void SetData(Sprite chip, string amount, int ChipIndex)
    {
        chipImage.sprite = chip;
        Chiptext.text = amount;
        chipIndex = ChipIndex;
        chipAmount = amount;
    }
}

// A bet option's static chip: where a flying chip lands and what it should look like
public readonly struct ChipReference
{
    public readonly RectTransform Rect;
    public readonly TMP_Text Text;

    public ChipReference(RectTransform rect, TMP_Text text)
    {
        Rect = rect;
        Text = text;
    }
}
