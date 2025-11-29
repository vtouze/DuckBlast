using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

public class Timer : MonoBehaviour
{
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private Image progressBar;
    [SerializeField] private Animator readyGoAnimator;
    [SerializeField] private Animator closingTimerAnimator;
    [SerializeField] private Animator fallingSpritesAnimator;
    private int timerDuration = 60;
    [SerializeField] private TargetManager targetManager;
    [SerializeField] private PlayerController playerController;
    private float remainingTime;
    private bool isTimerRunning = false;

    private void Start()
    {
        StartCoroutine(PlayReadyGoAnimation());
    }

    private IEnumerator PlayReadyGoAnimation()
    {

        if (fallingSpritesAnimator != null)
        {
            fallingSpritesAnimator.SetTrigger("PlayOpening");
        }

        yield return new WaitForSeconds(1.5f);

        if (readyGoAnimator != null)
        {
            readyGoAnimator.SetTrigger("OpenTimer");
        }

        yield return new WaitForSeconds(3f);

        Begin(timerDuration);
        if (targetManager != null)
        {
            targetManager.enabled = true;
        }
    }

    private void Begin(int seconds)
    {
        remainingTime = seconds;
        isTimerRunning = true;
        StartCoroutine(UpdateTimer());
    }

    private IEnumerator UpdateTimer()
    {
        while (remainingTime > 0 && isTimerRunning)
        {
            timerText.text = $"{Mathf.Ceil(remainingTime):00}";
            progressBar.fillAmount = Mathf.InverseLerp(0, timerDuration, remainingTime);
            remainingTime -= Time.deltaTime;
            yield return null;
        }
        OnTimerEnd();
    }

    private void OnTimerEnd()
    {
        isTimerRunning = false;
        timerText.text = "00";
        progressBar.fillAmount = 0;
        if (closingTimerAnimator != null)
        {
            StartCoroutine(PlayClosingAnimations());
        }
    }

    private IEnumerator PlayClosingAnimations()
    {
        if (playerController != null)
        {
            playerController.enabled = false;
        }

        closingTimerAnimator.SetTrigger("CloseTimer");
        yield return new WaitForSeconds(1.5f);

        if (fallingSpritesAnimator != null)
        {
            fallingSpritesAnimator.SetTrigger("PlayClosing");
        }
        yield return new WaitForSeconds(2f);

        SceneManager.LoadScene("MainMenu");
    }
}