using UnityEngine;
using System.Collections;

public class CreditsController : MonoBehaviour
{
    [Header("Scrolling")]
    [SerializeField] private RectTransform creditsContent;
    [SerializeField] private Animator creditsAnimator;
    [SerializeField] private float animationDuration = 5f;

    [Header("Fade")]
    [SerializeField] private CanvasGroup creditsCanvasGroup;
    [SerializeField] private float fadeDuration = 1f;

    [Header("Transition")]
    [SerializeField] private Animator backgroundSlidingAnimator;

    private bool isAnimationPlaying = false;

    private void Start()
    {
        gameObject.SetActive(false);
    }

    public void StartCredits()
    {
        if (isAnimationPlaying) return;

        gameObject.SetActive(true);
        isAnimationPlaying = true;

        creditsContent.anchoredPosition = Vector2.zero;

        if (backgroundSlidingAnimator != null)
            backgroundSlidingAnimator.SetTrigger("PlayAnimation");

        if (creditsAnimator != null)
            creditsAnimator.SetTrigger("PlayAnimation");
    }

    public void OnAnimationFinished()
    {
        isAnimationPlaying = false;
        ReverseAnimations();
    }

    private void ReverseAnimations()
    {
        if (backgroundSlidingAnimator != null)
            backgroundSlidingAnimator.SetTrigger("ReverseAnimation");

        if (creditsAnimator != null)
            creditsAnimator.SetTrigger("ReverseAnimation");

        StartCoroutine(FadeOut());
    }

    private IEnumerator FadeOut()
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
        gameObject.SetActive(false);
    }
}