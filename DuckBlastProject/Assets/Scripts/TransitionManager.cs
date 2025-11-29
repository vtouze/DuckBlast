using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class TransitionManager : MonoBehaviour
{
    [SerializeField] private Animator slidingPanelAnimator;
    [SerializeField] private Animator fallingSpritesAnimator;
    [SerializeField] private float slideAnimationDuration = 1.5f;
    [SerializeField] private float fallAnimationDuration = 1.5f;
    [SerializeField] private float delayBeforeLoad = 0.25f;

    public void StartTransition(string sceneName)
    {
        StartCoroutine(PlayTransition(sceneName));
    }

    private IEnumerator PlayTransition(string sceneName)
    {
        if (slidingPanelAnimator != null)
        {
            slidingPanelAnimator.SetTrigger("PlayAnimation");
        }
        if (fallingSpritesAnimator != null)
        {
            fallingSpritesAnimator.SetTrigger("PlayClosing");
        }

        float maxLength = Mathf.Max(slideAnimationDuration, fallAnimationDuration);
        yield return new WaitForSeconds(maxLength);

        yield return new WaitForSeconds(delayBeforeLoad);
        SceneManager.LoadScene(sceneName);
    }
}