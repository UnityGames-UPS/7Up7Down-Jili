using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class Scaleandfade : MonoBehaviour
{
    [Header("Main Animation Image")]
    public Image image;               // the UI Image to animate
    public float firstDuration = 0.5f;
    public float waitDuration = 1f;
    public float secondDuration = 0.8f;
    public float fadeTarget = 0f;     // target alpha (0 = invisible, 1 = visible)

    [Header("Other Images")]
    public Image FirstBetImg;

    [Header("Filled Player Images")]
    public Image Filledimg_player1;
    public Image Filledimg_player2;
    public Image Filledimg_player3;
    public Image Filledimg_player4;

    [Header("Red Player Images")]
    public Image RedImagePlayer1;
    public Image RedImagePlayer2;
    public Image RedImagePlayer3;
    public Image RedImagePlayer4;

    void Start()
    {
        // Example: call DoSequence() manually for testing
        // DoSequence();
    }

    public void DoSequence()
    {
        ResetImage();
        image.gameObject.SetActive(true);

        Sequence seq = DOTween.Sequence();

        // 1️⃣ Scale down from 1.5 → 1
        seq.Append(image.rectTransform.DOScale(1f, firstDuration).SetEase(Ease.InOutSine));

        // 2️⃣ Wait
        seq.AppendInterval(waitDuration);

        // 3️⃣ Scale up from 1 → 2, and fade simultaneously
        seq.Append(image.rectTransform.DOScale(3.5f, secondDuration).SetEase(Ease.InOutSine));
        seq.Join(image.DOFade(fadeTarget, secondDuration));

        // 4️⃣ After scaling/fade completes, show FirstBetImg for 1 second
        seq.AppendCallback(() =>
        {
            FirstBetImg.gameObject.SetActive(true);
        });
        seq.AppendInterval(1f);
        seq.AppendCallback(() =>
        {
            FirstBetImg.gameObject.SetActive(false);
        });

        // 5️⃣ Animate fill amount of 4 player images (and enable red ones too)
        seq.AppendCallback(() =>
        {
            // Enable all filled and red images
            Filledimg_player1.gameObject.SetActive(true);
            Filledimg_player2.gameObject.SetActive(true);
            Filledimg_player3.gameObject.SetActive(true);
            Filledimg_player4.gameObject.SetActive(true);

            RedImagePlayer1.gameObject.SetActive(true);
            RedImagePlayer2.gameObject.SetActive(true);
            RedImagePlayer3.gameObject.SetActive(true);
            RedImagePlayer4.gameObject.SetActive(true);

            // Set starting fill to full
            Filledimg_player1.fillAmount = 1f;
            Filledimg_player2.fillAmount = 1f;
            Filledimg_player3.fillAmount = 1f;
            Filledimg_player4.fillAmount = 1f;

            // Animate fills from 1 → 0 in 3 seconds
            Filledimg_player1.DOFillAmount(0f, 3f);
            Filledimg_player2.DOFillAmount(0f, 3f);
            Filledimg_player3.DOFillAmount(0f, 3f);
            Filledimg_player4.DOFillAmount(0f, 3f);
        });

        // 6️⃣ After fill animations finish, disable all filled and red images
        seq.AppendInterval(3f);
        seq.AppendCallback(() =>
        {
            Filledimg_player1.gameObject.SetActive(false);
            Filledimg_player2.gameObject.SetActive(false);
            Filledimg_player3.gameObject.SetActive(false);
            Filledimg_player4.gameObject.SetActive(false);

            RedImagePlayer1.gameObject.SetActive(false);
            RedImagePlayer2.gameObject.SetActive(false);
            RedImagePlayer3.gameObject.SetActive(false);
            RedImagePlayer4.gameObject.SetActive(false);
            image.gameObject.SetActive(false);
        });

        seq.Play();
    }

    private void ResetImage()
    {
        image.gameObject.SetActive(false);
        image.rectTransform.localScale = Vector3.one * 1.5f;

        Color c = image.color;
        c.a = 1f;
        image.color = c;

        // Reset states of others
        FirstBetImg.gameObject.SetActive(false);
        Filledimg_player1.gameObject.SetActive(false);
        Filledimg_player2.gameObject.SetActive(false);
        Filledimg_player3.gameObject.SetActive(false);
        Filledimg_player4.gameObject.SetActive(false);

        RedImagePlayer1.gameObject.SetActive(false);
        RedImagePlayer2.gameObject.SetActive(false);
        RedImagePlayer3.gameObject.SetActive(false);
        RedImagePlayer4.gameObject.SetActive(false);
    }
}
