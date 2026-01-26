using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using System.Collections;

[System.Serializable]
public class ComboEvent : UnityEvent<int, int, int> { }

public class ComboSystem : MonoBehaviour
{
    [Header("Combo Settings")]
    [SerializeField] private float comboTimeWindow = 2f;
    [SerializeField] private int comboThreshold2 = 3;
    [SerializeField] private int comboThreshold3 = 6;
    [SerializeField] private int comboThreshold4 = 9;

    [Header("Combo Colors")]
    public Color comboColor2 = Color.yellow;
    public Color comboColor3 = Color.red;
    public Color comboColor4 = Color.magenta;

    [Header("Combo UI")]
    [SerializeField] private Image comboTimerBar;
    [SerializeField] private CanvasGroup comboUIGroup;
    [SerializeField] private float fadeDuration = 0.3f;

    [Header("Events")]
    public ComboEvent OnComboUpdated;
    public UnityEvent OnComboReset;
    public UnityEvent OnComboStarted;
    public UnityEvent OnComboEnded;

    private int currentCombo = 0;
    private float lastHitTime = 0f;
    private int currentMultiplier = 1;
    private Coroutine fadeCoroutine;
    private bool isComboActive = false;
    private bool isUIShown = false;

    private void Start()
    {
        if (comboUIGroup != null)
        {
            comboUIGroup.alpha = 0f;
            comboUIGroup.gameObject.SetActive(false);
            isUIShown = false;
        }

        if (comboTimerBar != null)
        {
            if (comboTimerBar.type != Image.Type.Filled)
            {
                comboTimerBar.type = Image.Type.Filled;
            }

            Color color = comboTimerBar.color;
            color.a = 1f;
            comboTimerBar.color = color;

            comboTimerBar.fillAmount = 0f;
        }
        else
        {
        }
    }

    private void Update()
    {
        if (isComboActive && Time.time - lastHitTime > comboTimeWindow)
        {
            ResetCombo();
            return;
        }

        if (comboTimerBar != null && isComboActive)
        {
            float timeRemaining = comboTimeWindow - (Time.time - lastHitTime);
            float fillAmount = Mathf.Clamp01(timeRemaining / comboTimeWindow);
            comboTimerBar.fillAmount = fillAmount;
        }
    }

    public void RegisterHit(int baseScore)
    {
        if (currentCombo == 0)
        {
            currentCombo++;
            lastHitTime = Time.time;
            UpdateMultiplier();
            return;
        }

        if (currentCombo == comboThreshold2 - 1 && !isComboActive)
        {
            ShowComboUI();
            isComboActive = true;
        }

        currentCombo++;
        lastHitTime = Time.time;

        if (comboTimerBar != null)
        {
            comboTimerBar.fillAmount = 1f;
        }

        UpdateMultiplier();
        OnComboUpdated?.Invoke(currentCombo, currentMultiplier, baseScore);
    }

    public void RegisterMiss()
    {
        if (isComboActive)
        {
            ResetCombo();
        }
        else if (currentCombo > 0)
        {
            currentCombo = 0;
            currentMultiplier = 1;
        }
    }

    private void UpdateMultiplier()
    {
        if (currentCombo >= comboThreshold4)
        {
            currentMultiplier = 4;
        }
        else if (currentCombo >= comboThreshold3)
        {
            currentMultiplier = 3;
        }
        else if (currentCombo >= comboThreshold2)
        {
            currentMultiplier = 2;
        }
        else
        {
            currentMultiplier = 1;
        }
    }

    private void ResetCombo()
    {
        currentCombo = 0;
        currentMultiplier = 1;
        isComboActive = false;
        OnComboReset?.Invoke();
        HideComboUI();
    }

    private void ShowComboUI()
    {
        if (comboUIGroup != null && !isUIShown)
        {
            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
            }

            comboUIGroup.gameObject.SetActive(true);

            if (comboTimerBar != null)
            {
                comboTimerBar.fillAmount = 1f;
            }

            fadeCoroutine = StartCoroutine(FadeUI(comboUIGroup, 0f, 1f, fadeDuration));
            OnComboStarted?.Invoke();
            isUIShown = true;
        }
    }

    private void HideComboUI()
    {
        if (comboUIGroup != null && isUIShown)
        {
            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
            }

            fadeCoroutine = StartCoroutine(FadeUI(comboUIGroup, 1f, 0f, fadeDuration, true));
            OnComboEnded?.Invoke();
            isUIShown = false;
        }
    }

    private IEnumerator FadeUI(CanvasGroup group, float startAlpha, float endAlpha, float duration, bool disableOnComplete = false)
    {
        float elapsed = 0f;
        group.alpha = startAlpha;

        while (elapsed < duration)
        {
            group.alpha = Mathf.Lerp(startAlpha, endAlpha, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        group.alpha = endAlpha;

        if (disableOnComplete && endAlpha <= 0f)
        {
            group.gameObject.SetActive(false);
        }
    }

    public void TimeUp()
    {
        if (isComboActive)
        {
            ResetCombo();
        }
    }


    public int GetCurrentMultiplier() { return currentMultiplier; }
    public int GetCurrentCombo() { return currentCombo; }
    public int CalculateScore(int baseScore) { return baseScore * currentMultiplier; }
}