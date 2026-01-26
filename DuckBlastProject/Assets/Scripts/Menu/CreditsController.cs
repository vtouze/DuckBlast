using UnityEngine;
using System.Collections;

public class CreditsController : MonoBehaviour
{
    [Header("Scrolling")]
    [SerializeField] private RectTransform creditsContent;
    [SerializeField] private float scrollSpeed = 300f;

    [Header("Logo Fade")]
    [SerializeField] private RectTransform logoRect;
    [SerializeField] private CanvasGroup creditsCanvasGroup;
    [SerializeField] private float fadeDuration = 1f;

    [Header("Transition")]
    [SerializeField] private Animator backgroundSlidingAnimator;

    private Vector2 startPos;
    private Vector2 endPos;
    private bool fadeTriggered = false;
    private Coroutine fadeCoroutine;
    private bool isCreditsActive = false;

    private void Start()
    {
        startPos = creditsContent.anchoredPosition;
        endPos = new Vector2(startPos.x, startPos.y + (creditsContent.rect.height + Screen.height));

        gameObject.SetActive(false);
    }

    public void StartCredits()
    {
        gameObject.SetActive(true);
        ResetCredits();
        isCreditsActive = true;
    }

    private void OnEnable()
    {
        ResetCredits();
    }

    public void ResetCredits()
    {
        fadeTriggered = false;
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }

        creditsContent.anchoredPosition = startPos;
        if (creditsCanvasGroup != null)
            creditsCanvasGroup.alpha = 1f;
    }

    private void Update()
    {
        if (isCreditsActive && !fadeTriggered)
        {
            LauchSlidingAnimation();
            ScrollCredits();
            CheckLogoFade();
        }
    }

    private void LauchSlidingAnimation()
    {
        if (backgroundSlidingAnimator != null)
        {
            backgroundSlidingAnimator.SetTrigger("PlayAnimation");
        }
    }

    private void ScrollCredits()
    {
        creditsContent.anchoredPosition = Vector2.MoveTowards(
            creditsContent.anchoredPosition,
            endPos,
            scrollSpeed * Time.deltaTime
        );
    }

    private void CheckLogoFade()
    {
        if (fadeTriggered || logoRect == null || creditsCanvasGroup == null) return;

        Vector3[] logoCorners = new Vector3[4];
        logoRect.GetWorldCorners(logoCorners);

        Vector3 logoCenterWorld = (logoCorners[0] + logoCorners[2]) / 2f;
        Vector2 logoCenterScreen = RectTransformUtility.WorldToScreenPoint(Camera.main, logoCenterWorld);

        float screenCenterY = Screen.height / 2f;
        float distanceFromCenter = Mathf.Abs(logoCenterScreen.y - screenCenterY);

        if (distanceFromCenter < 5f)
        {
            fadeTriggered = true;
            StartFadeOut();
        }
    }

    private void StartFadeOut()
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }
        fadeCoroutine = StartCoroutine(FadeOutCredits());
    }

    private IEnumerator FadeOutCredits()
    {
        float elapsedTime = 0f;
        float startAlpha = creditsCanvasGroup.alpha;

        if (backgroundSlidingAnimator != null)
        {
            backgroundSlidingAnimator.SetTrigger("ReverseAnimation");
        }

        while (elapsedTime < fadeDuration)
        {
            creditsCanvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, elapsedTime / fadeDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        creditsCanvasGroup.alpha = 0f;

        isCreditsActive = false;
        gameObject.SetActive(false);
    }
}