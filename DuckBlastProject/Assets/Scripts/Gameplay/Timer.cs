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
    [SerializeField] private Animator scorePanelAnimator;
    [SerializeField] private Animator score_timerAnimator;
    [SerializeField] private int timerDuration = 60;

    [SerializeField] private ComboSystem comboSystem;

    [SerializeField] private TargetManager targetManager;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private ShootingManager shootingManager;
    [SerializeField] private TMP_Text finalScoreText;
    [SerializeField] private TMP_Text highScoreText;
    [SerializeField] private float scoreCountUpDuration = 2f;

    private float remainingTime;
    private bool isTimerRunning = false;
    private int highScore;
    private int currentScore;

    private void Start()
    {
        scorePanelAnimator.gameObject.SetActive(false);
        highScore = PlayerPrefs.GetInt("HighScore", 0);
        StartCoroutine(PlayReadyGoAnimation());
    }

    private IEnumerator PlayReadyGoAnimation()
    {
        SetPlayerControlsEnabled(false);
        if (fallingSpritesAnimator != null)
        {
            fallingSpritesAnimator.SetTrigger("PlayOpening");
        }
        yield return new WaitForSeconds(1.5f);
        if (score_timerAnimator != null)
        {
            score_timerAnimator.SetTrigger("ShowHUD");
        }
        if (readyGoAnimator != null)
        {
            readyGoAnimator.SetTrigger("OpenTimer");
        }
        yield return new WaitForSeconds(3f);
        SetPlayerControlsEnabled(true);
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
        StartCoroutine(EndGameSequence());
    }

    private IEnumerator EndGameSequence()
    {
        SetPlayerControlsEnabled(false);

        if (comboSystem != null)
        {
            comboSystem.TimeUp();
        }

        if (closingTimerAnimator != null)
        {
            closingTimerAnimator.SetTrigger("CloseTimer");
        }
        if (score_timerAnimator != null)
        {
            score_timerAnimator.SetTrigger("RemoveHUD");
        }
        yield return new WaitForSeconds(1.5f);
        ShowScorePanel();
    }

    private void ShowScorePanel()
    {
        scorePanelAnimator.gameObject.SetActive(true);
        isTimerRunning = false;
        timerText.text = "00";
        progressBar.fillAmount = 0;
        currentScore = shootingManager != null ? shootingManager.GetScore() : 0;
        if (currentScore > highScore)
        {
            highScore = currentScore;
            PlayerPrefs.SetInt("HighScore", highScore);
        }
        if (highScoreText != null)
        {
            highScoreText.text = highScore.ToString();
        }
        if (scorePanelAnimator != null)
        {
            scorePanelAnimator.SetTrigger("OpenScoreDisplay");
        }
        StartCoroutine(CountUpScore());
    }

    private IEnumerator CountUpScore()
    {
        yield return new WaitForSeconds(0.5f);
        int displayScore = 0;
        float elapsedTime = 0f;
        int previousDisplayScore = -1;
        while (elapsedTime < scoreCountUpDuration)
        {
            float progress = elapsedTime / scoreCountUpDuration;
            displayScore = Mathf.FloorToInt(Mathf.Lerp(0, currentScore, progress));
            if (displayScore != previousDisplayScore)
            {
                if (finalScoreText != null)
                {
                    finalScoreText.text = displayScore.ToString();
                }
                previousDisplayScore = displayScore;
            }
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        if (finalScoreText != null)
        {
            finalScoreText.text = currentScore.ToString();
        }
        yield return new WaitForSeconds(2f);
        StartCoroutine(FinalClosingAnimations());
    }

    private IEnumerator FinalClosingAnimations()
    {
        if (fallingSpritesAnimator != null)
        {
            fallingSpritesAnimator.SetTrigger("PlayClosing");
        }
        yield return new WaitForSeconds(2f);
        SceneManager.LoadScene("MainMenu");
    }

    private void SetPlayerControlsEnabled(bool enabled)
    {
        if (playerController != null)
        {
            playerController.enabled = enabled;
        }

        if (shootingManager != null)
        {
            shootingManager.SetControlsEnabled(enabled);
        }
    }
}