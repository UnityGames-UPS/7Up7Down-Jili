using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class DiceResultmanager : MonoBehaviour
{
    private enum ImageState
    {
        NONE,
        PLAYING,
        PAUSED
    }

    internal static DiceResultmanager Instance;

    [SerializeField] private List<Sprite> textureArray;

    [SerializeField] private Image rendererDelegate;

    [SerializeField] private bool useSharedMaterial = true;

    [SerializeField] private bool doLoopAnimation = true;
    [SerializeField] private bool StartOnAwake;

    [SerializeField] private bool StartonEnable;

    private ImageState currentAnimationState;

    private int indexOfTexture;

    private float idealFrameRate = 0.0416666679f;

    private float delayBetweenAnimation;

    // When the next frame is due; late frames are made up so the roll keeps its length
    private float nextFrameAt;

    [SerializeField] private float AnimationSpeed = 5f;

    [SerializeField] private float delayBetweenLoop;
    [SerializeField] private Image DiceOneResult;
    [SerializeField] private Image DiceTwoResult;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        if (StartOnAwake)
        {
            // StartAnimation();
        }
    }



    private void OnDisable()
    {
        //rendererDelegate.sprite = textureArray[0];
        StopAnimation();
    }

    private void AnimationProcess()
    {
        SetTextureOfIndex();
        indexOfTexture++;
        if (indexOfTexture == textureArray.Count)
        {
            indexOfTexture = 0;
            if (doLoopAnimation)
            {
                ScheduleNextFrame(delayBetweenAnimation + delayBetweenLoop);
            }
            else
            {
                DiceOneResult.gameObject.SetActive(true);
                DiceTwoResult.gameObject.SetActive(true);
            }
        }
        else
        {
            ScheduleNextFrame(delayBetweenAnimation);
        }
    }

    private void ScheduleNextFrame(float delay)
    {
        nextFrameAt = Mathf.Max(nextFrameAt + delay, Time.time);
        Invoke("AnimationProcess", nextFrameAt - Time.time);
    }

    // One frame per delay, and the first frame also waits one delay
    internal float RollDuration => idealFrameRate * textureArray.Count / AnimationSpeed * textureArray.Count;

    internal void StartAnimation(Sprite firstDice, Sprite secondDice)
    {
        StopAnimation();
        DiceOneResult.gameObject.SetActive(false);
        DiceTwoResult.gameObject.SetActive(false);
        DiceOneResult.sprite = firstDice;
        DiceTwoResult.sprite = secondDice;
        indexOfTexture = 0;
        if (currentAnimationState == ImageState.NONE)
        {
            RevertToInitialState();
            delayBetweenAnimation = idealFrameRate * (float)textureArray.Count / AnimationSpeed;
            currentAnimationState = ImageState.PLAYING;
            nextFrameAt = Time.time;
            ScheduleNextFrame(delayBetweenAnimation);
        }
    }

    // Shows a result without rolling; ignored mid-roll so it cannot spoil the reveal
    internal void ShowResult(Sprite firstDice, Sprite secondDice)
    {
        if (IsInvoking("AnimationProcess")) return;
        DiceOneResult.sprite = firstDice;
        DiceTwoResult.sprite = secondDice;
        DiceOneResult.gameObject.SetActive(true);
        DiceTwoResult.gameObject.SetActive(true);
    }

    // Drops a roll still in flight so ShowResult can take over
    internal void CancelRoll()
    {
        if (IsInvoking("AnimationProcess")) StopAnimation();
    }

    internal void PauseAnimation()
    {
        if (currentAnimationState == ImageState.PLAYING)
        {
            CancelInvoke("AnimationProcess");
            currentAnimationState = ImageState.PAUSED;
        }
    }

    internal void ResumeAnimation()
    {
        if (currentAnimationState == ImageState.PAUSED && !IsInvoking("AnimationProcess"))
        {
            nextFrameAt = Time.time;
            ScheduleNextFrame(delayBetweenAnimation);
            currentAnimationState = ImageState.PLAYING;
        }
    }

    internal void StopAnimation()
    {
        if (currentAnimationState != 0)
        {
            rendererDelegate.sprite = textureArray[0];
            CancelInvoke("AnimationProcess");
            currentAnimationState = ImageState.NONE;
        }
    }

    internal void RevertToInitialState()
    {
        indexOfTexture = 0;
        SetTextureOfIndex();
    }

    private void SetTextureOfIndex()
    {
        if (useSharedMaterial)
        {
            rendererDelegate.sprite = textureArray[indexOfTexture];
        }
        else
        {
            rendererDelegate.sprite = textureArray[indexOfTexture];
        }
    }
}

