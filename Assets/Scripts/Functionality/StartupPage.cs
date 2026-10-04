using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class StartupPage : MonoBehaviour
{
    [SerializeField] GameManager gameManager;
    [SerializeField] SocketIOManager socketManager;
    [SerializeField] AudioManager audioManager;

    [Header("UI")]
    [FormerlySerializedAs("HomePage")]
    [SerializeField] internal GameObject Page;
    [SerializeField] internal GameObject ContinueObj;
    [SerializeField] internal GameObject LoadingObj;
    [Header("Continue")]
    [SerializeField] internal Button ContinueBtn;
    [SerializeField] internal Button DontShowAgainBtn;
    [SerializeField] internal Image DontShowAgainTick;
    [SerializeField] private TMP_Text LoadingPercentageText;

    [Header("Loading")]
    [SerializeField] private Image LoadingFillImage;
    [Header("SplashImages")]
    [SerializeField] internal Button LeftBtn;
    [SerializeField] internal Button RightBtn;
    [SerializeField] internal List<GameObject> SplashImage;

    internal bool canLoadFull = false;

    private bool isDontShowAgain = false;
    // Old key kept so saved choices survive the rename
    private const string DONT_SHOW_AGAIN_KEY = "DontShowAgainHomepage";

    [Header("Splash Animation")]
    [SerializeField] private float slideDuration = 0.5f;
    [SerializeField] private float autoScrollInterval = 5f;
    private int currentSplashIndex = 0;
    private int autoDirection = 1;
    private float idleTime = 0f;
    private bool isAnimating = false;

    private void Awake()
    {
        // The page is often left disabled in the scene while editing
        Page.SetActive(true);

        // The game loop runs behind this page; keep it silent until dismissed
        audioManager.SetStartupSilence(true);
    }

    private void Start()
    {
        isDontShowAgain = PlayerPrefs.GetInt(DONT_SHOW_AGAIN_KEY, 0) == 1;

        ContinueBtn.onClick.RemoveAllListeners();
        ContinueBtn.onClick.AddListener(Dismiss);

        DontShowAgainBtn.onClick.RemoveAllListeners();
        DontShowAgainBtn.onClick.AddListener(OnDontShowAgainClick);
        DontShowAgainTick.gameObject.SetActive(isDontShowAgain);

        if (LeftBtn != null)
        {
            LeftBtn.onClick.RemoveAllListeners();
            LeftBtn.onClick.AddListener(() => OnArrowClick(-1));
        }

        if (RightBtn != null)
        {
            RightBtn.onClick.RemoveAllListeners();
            RightBtn.onClick.AddListener(() => OnArrowClick(1));
        }

        StartCoroutine(StartLoading());
        InitializeSplashImages();
    }

    #region Splash animation

    private void InitializeSplashImages()
    {
        if (SplashImage == null || SplashImage.Count == 0) return;

        for (int i = 0; i < SplashImage.Count; i++)
        {
            SplashImage[i].SetActive(i == 0);
        }

        currentSplashIndex = 0;
        UpdateButtonStates();

        if (SplashImage.Count > 1) StartCoroutine(AutoScroll());
    }

    private IEnumerator AutoScroll()
    {
        while (true)
        {
            yield return null;
            if (isAnimating) continue;

            idleTime += Time.deltaTime;
            if (idleTime < autoScrollInterval) continue;

            // Reverse at either end
            int next = currentSplashIndex + autoDirection;
            if (next < 0 || next >= SplashImage.Count) autoDirection = -autoDirection;

            SlideTo(currentSplashIndex + autoDirection);
        }
    }

    private void OnArrowClick(int direction)
    {
        // Auto-scroll resumes a full interval after the last click
        idleTime = 0f;

        int target = currentSplashIndex + direction;
        if (isAnimating || target < 0 || target >= SplashImage.Count) return;

        SlideTo(target);
    }

    private void SlideTo(int toIndex)
    {
        idleTime = 0f;
        StartCoroutine(SlideAnimation(currentSplashIndex, toIndex));
    }

    private IEnumerator SlideAnimation(int fromIndex, int toIndex)
    {
        isAnimating = true;

        RectTransform fromRect = SplashImage[fromIndex].GetComponent<RectTransform>();
        RectTransform toRect = SplashImage[toIndex].GetComponent<RectTransform>();

        // Moving forward, the new slide enters from the right and the old one leaves to the left
        float width = ((RectTransform)toRect.parent).rect.width;
        float enterSide = toIndex > fromIndex ? 1f : -1f;
        Vector2 toStartPos = new Vector2(width * enterSide, 0);
        Vector2 fromEndPos = new Vector2(-width * enterSide, 0);

        toRect.anchoredPosition = toStartPos;
        fromRect.anchoredPosition = Vector2.zero;
        toRect.gameObject.SetActive(true);

        float elapsed = 0f;
        while (elapsed < slideDuration)
        {
            elapsed += Time.deltaTime;
            float easedT = Mathf.SmoothStep(0, 1, elapsed / slideDuration);

            fromRect.anchoredPosition = Vector2.Lerp(Vector2.zero, fromEndPos, easedT);
            toRect.anchoredPosition = Vector2.Lerp(toStartPos, Vector2.zero, easedT);

            yield return null;
        }

        fromRect.gameObject.SetActive(false);
        fromRect.anchoredPosition = Vector2.zero;
        toRect.anchoredPosition = Vector2.zero;

        currentSplashIndex = toIndex;
        isAnimating = false;
        UpdateButtonStates();
    }

    private void UpdateButtonStates()
    {
        if (LeftBtn != null)
        {
            LeftBtn.gameObject.SetActive(currentSplashIndex > 0);
        }

        if (RightBtn != null)
        {
            RightBtn.gameObject.SetActive(currentSplashIndex < SplashImage.Count - 1);
        }
    }

    #endregion

    private IEnumerator StartLoading()
    {
        LoadingObj.SetActive(true);
        ContinueObj.SetActive(false);

        float progress = 0f;
        float targetProgress = 0.9f;

        while (progress < targetProgress)
        {
            progress += Time.deltaTime * 0.5f;
            if (progress > targetProgress) progress = targetProgress;

            UpdateLoadingUI(progress);
            yield return null;
        }

        // Hold at 90% until the first level has been joined
        while (!canLoadFull)
        {
            yield return null;
        }

        while (progress < 1f)
        {
            progress += Time.deltaTime * 0.5f;
            if (progress > 1f) progress = 1f;

            UpdateLoadingUI(progress);
            yield return null;
        }

        yield return new WaitForSeconds(0.2f);

        LoadingObj.SetActive(false);
        if (isDontShowAgain)
        {
            Dismiss();
        }
        else
        {
            ContinueObj.SetActive(true);
        }
    }

    private void UpdateLoadingUI(float progress)
    {
        if (LoadingFillImage != null)
        {
            LoadingFillImage.fillAmount = progress;
        }

        if (LoadingPercentageText != null)
        {
            LoadingPercentageText.text = "Loading..." + Mathf.RoundToInt(progress * 100) + "%";
        }
    }

    private void Dismiss()
    {
        StopAllCoroutines();
        Page.SetActive(false);
        audioManager.SetStartupSilence(false);
    }

    private void OnDontShowAgainClick()
    {
        isDontShowAgain = !isDontShowAgain;
        DontShowAgainTick.gameObject.SetActive(isDontShowAgain);

        PlayerPrefs.SetInt(DONT_SHOW_AGAIN_KEY, isDontShowAgain ? 1 : 0);
        PlayerPrefs.Save();
    }

    internal void SetCanLoadFull(bool value)
    {
        canLoadFull = value;
    }
}
