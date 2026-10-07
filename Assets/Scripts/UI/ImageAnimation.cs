using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ImageAnimation : MonoBehaviour
{
	public enum ImageState
	{
		NONE,
		PLAYING,
		PAUSED
	}

	public List<Sprite> textureArray;

	public Image rendererDelegate;

	public bool useSharedMaterial = true;

	public bool doLoopAnimation = true;
	[SerializeField] private bool StartOnAwake;

	[SerializeField] private bool StartonEnable;

	[HideInInspector]
	public ImageState currentAnimationState;

	private int indexOfTexture;

	private float idealFrameRate = 0.0416666679f;

	private float delayBetweenAnimation;

	public float AnimationSpeed = 5f;

	public float delayBetweenLoop;

	private Action onComplete;
	private Action onMark;
	private int markFrame;
	private Action<int> onFrame;

	private void Awake()
	{
		if (StartOnAwake)
		{
			StartAnimation();
		}
	}

	private void OnEnable()
	{
		if (StartonEnable) StartAnimation();
	}

	private void OnDisable()
	{
		StopAnimation();
	}

	private void AnimationProcess()
	{
		SetTextureOfIndex();
		if (onFrame != null)
		{
			onFrame(indexOfTexture);
			if (currentAnimationState != ImageState.PLAYING) return;
		}
		if (onMark != null && indexOfTexture >= markFrame)
		{
			Action mark = onMark;
			onMark = null;
			mark();
			if (currentAnimationState != ImageState.PLAYING) return;
		}

		indexOfTexture++;
		if (indexOfTexture == textureArray.Count)
		{
			indexOfTexture = 0;
			// Copied first: the callback usually deactivates this object, which clears it
			Action completed = onComplete;
			if (doLoopAnimation)
			{
				Invoke("AnimationProcess", delayBetweenAnimation + delayBetweenLoop);
			}
			else
			{
				// Rests on the last frame, ready to be played again
				currentAnimationState = ImageState.NONE;
				onComplete = null;
			}
			completed?.Invoke();
		}
		else
		{
			Invoke("AnimationProcess", delayBetweenAnimation);
		}
	}

	// Restarts from the first frame; onComplete fires after the last frame (every loop if looping)
	internal void Play(Action onComplete = null) => Play(1f, null, onComplete);

	// onMark fires once, when the animation is markAt (0-1) of the way through
	internal void Play(float markAt, Action onMark, Action onComplete)
	{
		StopAnimation();
		this.onComplete = onComplete;
		this.onMark = onMark;
		markFrame = Mathf.Clamp(Mathf.FloorToInt(markAt * textureArray.Count), 0, textureArray.Count - 1);
		StartAnimation();
	}

	// onFrame fires with the index of every frame as it is shown
	internal void Play(Action<int> onFrame, Action onComplete)
	{
		Play(1f, null, onComplete);
		this.onFrame = onFrame;
	}

	public void StartAnimation()
	{
		indexOfTexture = 0;
		if (currentAnimationState == ImageState.NONE)
		{
			RevertToInitialState();
			delayBetweenAnimation = idealFrameRate * (float)textureArray.Count / AnimationSpeed;
			currentAnimationState = ImageState.PLAYING;
			Invoke("AnimationProcess", delayBetweenAnimation);
		}
	}

	public void PauseAnimation()
	{
		if (currentAnimationState == ImageState.PLAYING)
		{
			CancelInvoke("AnimationProcess");
			currentAnimationState = ImageState.PAUSED;
		}
	}

	public void ResumeAnimation()
	{
		if (currentAnimationState == ImageState.PAUSED && !IsInvoking("AnimationProcess"))
		{
			Invoke("AnimationProcess", delayBetweenAnimation);
			currentAnimationState = ImageState.PLAYING;
		}
	}

	public void StopAnimation()
	{
		onComplete = null;
		onMark = null;
		onFrame = null;
		if (currentAnimationState != 0)
		{
			rendererDelegate.sprite = textureArray[0];
			CancelInvoke("AnimationProcess");
			currentAnimationState = ImageState.NONE;
		}
	}

	public void RevertToInitialState()
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
