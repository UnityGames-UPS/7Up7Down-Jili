using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class Homepage : MonoBehaviour
{
    [SerializeField] GameManager gameManager;
    [SerializeField] SocketIOManager socketManager;
    [SerializeField] AudioManager audioManager;

    [Header("UI")]
    [SerializeField] internal GameObject HomePage;
    [SerializeField] internal GameObject ContinueObj;
    [SerializeField] internal GameObject LoadingObj;
    [Header("Continue")]
    [SerializeField] internal Button ContinueBtn;
    [SerializeField] internal Button DontShowAgainBtn;
    [SerializeField] internal Image DontShowAgainTick;
    [SerializeField] private TMP_Text LoadingPercentageText;

    [Header("Loading")]
    [SerializeField] internal TMP_Text PlayerBalance;
    [SerializeField] private Image LoadingFillImage;
    [Header("SplashImages")]
    [SerializeField] internal Button LeftBtn;
    [SerializeField] internal Button RightBtn;
    [SerializeField] internal List<GameObject> SplashImage;

    internal bool canLoadFull = false;

    private bool isDontShowAgain = false;
    private const string DONT_SHOW_AGAIN_KEY = "DontShowAgainHomepage";

    private int currentSplashIndex = 0;
    private Coroutine autoLoopCoroutine;
    private float autoLoopInterval = 3f;
    private bool isUserInteracted = false;

    [Header("Splash Animation")]
    [SerializeField] private float slideDuration = 0.5f;
    [SerializeField] private float autoScrollInterval = 3f;
    private bool isAnimating = false;
    private Queue<System.Action> animationQueue = new Queue<System.Action>();
    private RectTransform currentSplashRect;
    private Coroutine slideCoroutine;
    private bool isAutoScrolling = true;

    private void Start()
    {
        // Load saved preference
        isDontShowAgain = PlayerPrefs.GetInt(DONT_SHOW_AGAIN_KEY, 0) == 1;

        // Setup buttons
        ContinueBtn.onClick.RemoveAllListeners();
        ContinueBtn.onClick.AddListener(OnContinueClick);

        DontShowAgainBtn.onClick.RemoveAllListeners();
        DontShowAgainBtn.onClick.AddListener(OnDontShowAgainClick);

        // Set initial tick state
        DontShowAgainTick.gameObject.SetActive(isDontShowAgain);
        if (LeftBtn != null)
        {
            LeftBtn.onClick.RemoveAllListeners();
            LeftBtn.onClick.AddListener(OnLeftClick);
        }

        if (RightBtn != null)
        {
            RightBtn.onClick.RemoveAllListeners();
            RightBtn.onClick.AddListener(OnRightClick);
        }
        // Start loading
        StartCoroutine(StartLoading());
        InitializeSplashImages();
    }

    #region Splash animation

    private void InitializeSplashImages()
    {
        if (SplashImage == null || SplashImage.Count == 0) return;

        // Show first image, hide others
        for (int i = 0; i < SplashImage.Count; i++)
        {
            if (SplashImage[i] != null)
            {
                SplashImage[i].SetActive(i == 0);
                // Enable/disable buttons based on position
                UpdateButtonStates();
            }
        }

        currentSplashIndex = 0;
        isAutoScrolling = true;

        // Start auto loop
        StartAutoLoop();
    }

    private void StartAutoLoop()
    {
        if (autoLoopCoroutine != null)
        {
            StopCoroutine(autoLoopCoroutine);
        }
        autoLoopCoroutine = StartCoroutine(AutoLoopSplash());
    }

    private IEnumerator AutoLoopSplash()
    {
        // Auto scroll from first to last, then stop
        while (isAutoScrolling && currentSplashIndex < SplashImage.Count - 1)
        {
            yield return new WaitForSeconds(autoScrollInterval);
            if (!isAnimating && isAutoScrolling)
            {
                ShowNextSplashWithAnimation();
            }
        }

        // Auto scroll finished
        isAutoScrolling = false;
        autoLoopCoroutine = null;
    }

    private void OnLeftClick()
    {
        // Stop auto scrolling when user interacts
        StopAutoScroll();

        if (!isAnimating && currentSplashIndex > 0)
        {
            EnqueueSlide(() => ShowPreviousSplashWithAnimation());
        }
    }

    private void OnRightClick()
    {
        // Stop auto scrolling when user interacts
        StopAutoScroll();

        if (!isAnimating && currentSplashIndex < SplashImage.Count - 1)
        {
            EnqueueSlide(() => ShowNextSplashWithAnimation());
        }
    }

    private void StopAutoScroll()
    {
        if (isAutoScrolling)
        {
            isAutoScrolling = false;
            if (autoLoopCoroutine != null)
            {
                StopCoroutine(autoLoopCoroutine);
                autoLoopCoroutine = null;
            }
        }
    }

    private void EnqueueSlide(System.Action slideAction)
    {
        animationQueue.Enqueue(slideAction);
        if (!isAnimating)
        {
            ProcessQueue();
        }
    }

    private void ProcessQueue()
    {
        if (animationQueue.Count > 0 && !isAnimating)
        {
            var nextSlide = animationQueue.Dequeue();
            nextSlide?.Invoke();
        }
    }

    private void ShowNextSplashWithAnimation()
    {
        if (SplashImage == null || SplashImage.Count == 0) return;
        if (currentSplashIndex >= SplashImage.Count - 1) return;

        int nextIndex = currentSplashIndex + 1;
        StartSlideAnimation(currentSplashIndex, nextIndex, false);
    }

    private void ShowPreviousSplashWithAnimation()
    {
        if (SplashImage == null || SplashImage.Count == 0) return;
        if (currentSplashIndex <= 0) return;

        int prevIndex = currentSplashIndex - 1;
        StartSlideAnimation(currentSplashIndex, prevIndex, true);
    }

    private void StartSlideAnimation(int fromIndex, int toIndex, bool isLeft)
    {
        if (slideCoroutine != null)
            StopCoroutine(slideCoroutine);

        slideCoroutine = StartCoroutine(SlideAnimation(fromIndex, toIndex, isLeft));
    }

    private IEnumerator SlideAnimation(int fromIndex, int toIndex, bool isLeft)
    {
        isAnimating = true;

        GameObject fromSlide = SplashImage[fromIndex];
        GameObject toSlide = SplashImage[toIndex];

        if (fromSlide == null || toSlide == null)
        {
            isAnimating = false;
            ProcessQueue();
            yield break;
        }

        RectTransform fromRect = fromSlide.GetComponent<RectTransform>();
        RectTransform toRect = toSlide.GetComponent<RectTransform>();

        if (fromRect == null || toRect == null)
        {
            isAnimating = false;
            ProcessQueue();
            yield break;
        }

        // Set starting positions
        toSlide.SetActive(true);
        toRect.anchoredPosition = isLeft ? new Vector2(Screen.width, 0) : new Vector2(-Screen.width, 0);
        fromRect.anchoredPosition = Vector2.zero;

        float elapsed = 0f;
        Vector2 fromStartPos = Vector2.zero;
        Vector2 fromEndPos = isLeft ? new Vector2(-Screen.width, 0) : new Vector2(Screen.width, 0);
        Vector2 toStartPos = isLeft ? new Vector2(Screen.width, 0) : new Vector2(-Screen.width, 0);
        Vector2 toEndPos = Vector2.zero;

        while (elapsed < slideDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / slideDuration;
            float easedT = Mathf.SmoothStep(0, 1, t);

            fromRect.anchoredPosition = Vector2.Lerp(fromStartPos, fromEndPos, easedT);
            toRect.anchoredPosition = Vector2.Lerp(toStartPos, toEndPos, easedT);

            yield return null;
        }

        // Final positions
        fromRect.anchoredPosition = fromEndPos;
        toRect.anchoredPosition = toEndPos;

        // Hide old slide
        fromSlide.SetActive(false);

        // Reset positions
        fromRect.anchoredPosition = Vector2.zero;
        toRect.anchoredPosition = Vector2.zero;

        currentSplashIndex = toIndex;
        slideCoroutine = null;
        isAnimating = false;

        // Update button states (disable at ends)
        UpdateButtonStates();

        // Process next in queue
        ProcessQueue();
    }

    private void UpdateButtonStates()
    {
        // Disable left button at first image
        if (LeftBtn != null)
        {
            LeftBtn.gameObject.SetActive(currentSplashIndex > 0);
        }

        // Disable right button at last image
        if (RightBtn != null)
        {
            RightBtn.gameObject.SetActive(currentSplashIndex < SplashImage.Count - 1);
        }
    }

    private void HideAllSplashImages()
    {
        StopAutoScroll();

        foreach (var splash in SplashImage)
        {
            if (splash != null)
            {
                splash.SetActive(false);
            }
        }
    }

    #endregion






    private IEnumerator StartLoading()
    {
        LoadingObj.SetActive(true);
        ContinueObj.SetActive(false);

        float progress = 0f;
        float targetProgress = 0.9f; // 90%

        // Load until 90%
        while (progress < targetProgress)
        {
            progress += Time.deltaTime * 0.5f; // Adjust speed as needed
            if (progress > targetProgress) progress = targetProgress;

            UpdateLoadingUI(progress);
            yield return null;
        }

        // Wait until canLoadFull becomes true
        while (!canLoadFull)
        {
            yield return null;
        }

        // Load to 100%
        float startProgress = progress;
        while (progress < 1f)
        {
            progress += Time.deltaTime * 0.5f;
            if (progress > 1f) progress = 1f;

            UpdateLoadingUI(progress);
            yield return null;
        }

        // Loading complete
        yield return new WaitForSeconds(0.2f);

        // Check if "Don't show again" is enabled
        if (isDontShowAgain)
        {
            // Skip continue screen, directly start game
            LoadingObj.SetActive(false);
            gameManager.currentRoom = socketManager.initialData.levels[0];
            socketManager.SendRoomSelection(socketManager.initialData.levels[0]);
            OnLoadingComplete();
        }
        else
        {
            // Show continue screen
            LoadingObj.SetActive(false);
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

    private void OnContinueClick()
    {
        gameManager.currentRoom = socketManager.initialData.levels[0];
        socketManager.SendRoomSelection(socketManager.initialData.levels[0]);
        ContinueBtn.interactable = false;
        // OnLoadingComplete();
    }

    private void OnDontShowAgainClick()
    {
        // Toggle the tick
        isDontShowAgain = !isDontShowAgain;
        DontShowAgainTick.gameObject.SetActive(isDontShowAgain);

        // Save preference
        PlayerPrefs.SetInt(DONT_SHOW_AGAIN_KEY, isDontShowAgain ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void OnLoadingComplete()
    {
        // Call any methods that need to run after loading
        if (gameManager != null)
        {
            // Example: gameManager.StartGame();
        }

        Debug.Log("Homepage loading complete");
    }

    internal void SetPlayerBalance(double balance)
    {
        if (PlayerBalance != null)
        {
            PlayerBalance.text = "Rs " + balance.ToString();
        }
    }

    internal void SetCanLoadFull(bool value)
    {
        canLoadFull = value;
    }

    internal void ResetLoading()
    {
        canLoadFull = false;
        if (LoadingFillImage != null)
        {
            LoadingFillImage.fillAmount = 0f;
        }
        if (LoadingPercentageText != null)
        {
            LoadingPercentageText.text = "0%";
        }
    }
}