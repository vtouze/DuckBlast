using UnityEngine;
using UnityEngine.Events;

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

    [Header("Events")]
    public ComboEvent OnComboUpdated;
    public UnityEvent OnComboReset;

    private int currentCombo = 0;
    private float lastHitTime = 0f;
    private int currentMultiplier = 1;

    private void Update()
    {
        if (currentCombo > 0 && Time.time - lastHitTime > comboTimeWindow)
        {
            ResetCombo();
        }
    }

    public void RegisterHit(int baseScore)
    {
        currentCombo++;
        lastHitTime = Time.time;

        UpdateMultiplier();
        OnComboUpdated?.Invoke(currentCombo, currentMultiplier, baseScore);
    }

    public void RegisterMiss()
    {
        ResetCombo();
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
        OnComboReset?.Invoke();
    }

    public int GetCurrentMultiplier()
    {
        return currentMultiplier;
    }

    public int GetCurrentCombo()
    {
        return currentCombo;
    }

    public int CalculateScore(int baseScore)
    {
        return baseScore * currentMultiplier;
    }
}