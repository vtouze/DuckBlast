using UnityEngine;
using System.Collections;

public class CreditsController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private CanvasGroup creditsCanvasGroup;
    [SerializeField] private float fadeDuration = 1f;

    [Header("Animation")]
    [SerializeField] private Animator creditsAnimator;
    [SerializeField] private Animator backgroundSlidingAnimator;
    [SerializeField] private float animationDuration = 8f;

    private bool isCreditsActive = false;
    private Coroutine fadeCoroutine;

    private void Start()
    {
        gameObject.SetActive(false);
    }

    public void StartCredits()
    {
        gameObject.SetActive(true);
        ResetCredits();
        isCreditsActive = true;
        PlayCreditsAnimation();
    }

    private void OnEnable()
    {
        ResetCredits();
    }

    public void ResetCredits()
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }

        if (creditsCanvasGroup != null)
            creditsCanvasGroup.alpha = 1f;
    }

    private void PlayCreditsAnimation()
    {
        if (backgroundSlidingAnimator != null)
        {
            backgroundSlidingAnimator.SetTrigger("PlayAnimation");
        }

        if (creditsAnimator != null)
        {
            creditsAnimator.SetTrigger("PlayAnimation");

            StartCoroutine(ReverseAnimationAfterDelay());
        }
    }

    private IEnumerator ReverseAnimationAfterDelay()
    {
        yield return new WaitForSeconds(animationDuration);

        ReverseAnimations();
    }

    private void ReverseAnimations()
    {
        if (backgroundSlidingAnimator != null)
        {
            backgroundSlidingAnimator.SetTrigger("ReverseAnimation");
        }

        if (creditsAnimator != null)
        {
            creditsAnimator.SetTrigger("ReverseAnimation");
        }

        StartFadeOut();
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