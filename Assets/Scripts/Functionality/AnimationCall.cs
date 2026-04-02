using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class AnimationCall : MonoBehaviour
{

    [SerializeField] internal AudioManager audioManager;

    [Header("Right Card")]
    [SerializeField] internal Sprite RightSprite;
    [SerializeField] internal Image RightCard;
    [SerializeField] internal Transform RightAnimStart;
    [SerializeField] internal Transform RightAnimEnd;


    [Header("Left Card")]
    [SerializeField] internal Sprite LeftSprite;
    [SerializeField] internal Image LeftCard;
    [SerializeField] internal Transform LeftAnimStart;
    [SerializeField] internal Transform LeftAnimEnd;


    [Header("Middle Card")]
    [SerializeField] internal Sprite MiddleSprite;
    [SerializeField] internal Image MiddleCard;
    [SerializeField] internal ImageAnimation MiddleEffect;
    [SerializeField] internal Transform MiddleAnimStart;
    [SerializeField] internal Transform MiddleAnimEnd;

    [Header("Movable Card")]
    [SerializeField] internal GameObject MovableCard;
    [SerializeField] private Animator CardAnimator;
    [SerializeField] internal Image MovableMiddleCard;
    [SerializeField] internal Image MovableLeftCard;
    [SerializeField] internal Image MovableRightCard;


    public void LeftHandAnim()
    {
        if (audioManager) audioManager.PlayWLAudio("cards");
        LeftCard.sprite = LeftSprite;
        LeftCard.gameObject.SetActive(true);

        LeftCard.transform.position = LeftAnimStart.position;
        LeftCard.transform.localScale = new Vector3(0f, 1f, 1f);

        float duration = 0.3f;

        Sequence seq = DOTween.Sequence();
        seq.Join(LeftCard.transform.DOMove(LeftAnimEnd.position, duration).SetEase(Ease.OutQuad));
        seq.Join(LeftCard.transform.DOScaleX(1f, duration).SetEase(Ease.OutBack));
        seq.OnComplete(() =>
        {
            // Debug.Log("Left card flip animation complete!");
        });
    }
    public void RightHandAnim()
    {
        if (audioManager) audioManager.PlayWLAudio("cards");
        RightCard.sprite = RightSprite;
        RightCard.gameObject.SetActive(true);

        RightCard.transform.position = RightAnimStart.position;
        RightCard.transform.localScale = new Vector3(0f, 1f, 1f);

        float duration = 0.3f;

        Sequence seq = DOTween.Sequence();
        seq.Join(RightCard.transform.DOMove(RightAnimEnd.position, duration).SetEase(Ease.OutQuad));
        seq.Join(RightCard.transform.DOScaleX(1f, duration).SetEase(Ease.OutBack));
        seq.OnComplete(() =>
        {
            // Debug.Log("Right card flip animation complete!");
        });
    }
    public void CenterHandAnim()
    {
        MiddleEffect.StopAnimation();
        MiddleCard.sprite = MiddleSprite;

        MiddleCard.gameObject.SetActive(true);

        MiddleCard.transform.position = MiddleAnimStart.position;
        MiddleCard.transform.localScale = new Vector3(0f, 1f, 1f);

        float duration = 0.1f;


        Sequence seq = DOTween.Sequence();


        seq.Join(MiddleCard.transform.DOMove(MiddleAnimEnd.position, duration).SetEase(Ease.OutQuad));
        seq.Join(MiddleCard.transform.DOScaleX(1f, duration).SetEase(Ease.OutBack));


        MiddleCard.sprite = MiddleSprite;

        if (audioManager) audioManager.PlayWLAudio("cards");
        seq.AppendInterval(2f);
        seq.Append(MiddleCard.transform.DOScale(1.2f, 0.25f).SetEase(Ease.OutBack));
        MiddleEffect.StartAnimation();

        seq.AppendCallback(() => MiddleEffect.StartAnimation());
    }

    public void SwipCardOne()
    {
        MovableCard.SetActive(true);
        CardAnimator.Play("New Animation");
        MovableRightCard.sprite = RightCard.sprite;
        MovableRightCard.gameObject.SetActive(true);
        RightCard.gameObject.SetActive(false);
    }
    public void SwipCardTwo()
    {
        MovableMiddleCard.sprite = MiddleCard.sprite;
        MovableMiddleCard.gameObject.SetActive(true);
        MiddleCard.gameObject.SetActive(false);
    }
    public void SwipCardThree()
    {
        MovableLeftCard.sprite = LeftCard.sprite;
        MovableLeftCard.gameObject.SetActive(true);
        LeftCard.gameObject.SetActive(false);
    }
    public void AnimComplete()
    {
        MovableCard.SetActive(false);
        MovableRightCard.gameObject.SetActive(false);
        MovableMiddleCard.gameObject.SetActive(false);
        MovableLeftCard.gameObject.SetActive(false);
    }

}
