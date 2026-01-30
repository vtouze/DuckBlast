using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;
using System;

public class ShootingManager : MonoBehaviour
{
    [SerializeField] private Button fireButton;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private GameObject impactPrefab;
    [SerializeField] private Transform crosshairTransform;
    [SerializeField] private GameObject ammoPrefab;
    [SerializeField] private Transform ammoContainer;
    [SerializeField] private ComboSystem comboSystem;
    [SerializeField] private GameObject scorePopupPrefab;
    private float ejectionForce = 3f;
    private float ejectionDuration = 0.8f;
    private float popupLifetime = 1f;
    private Vector2 popupFixedOffset = new Vector2(50f, 50f);
    private int popupSortingOrder = 10;
    [SerializeField] private Canvas scoreCanvas;
    private int score = 0;
    private Camera mainCamera;
    private bool controlsEnabled = true;

    public static event Action<bool> OnControlsEnabledChanged;

    public int GetScore()
    {
        return score;
    }

    public void SetControlsEnabled(bool enabled)
    {
        controlsEnabled = enabled;
        if (fireButton != null)
        {
            fireButton.interactable = enabled;
        }

        OnControlsEnabledChanged?.Invoke(enabled);
    }

    public bool AreControlsEnabled()
    {
        return controlsEnabled;
    }


    private void Awake()
    {
        mainCamera = Camera.main;
        if (fireButton != null)
            fireButton.onClick.AddListener(Fire);
    }

    private void Fire()
    {
        if (!controlsEnabled) return;

        Vector3 screenPosition = crosshairTransform.position;
        Vector2 worldPosition = mainCamera.ScreenToWorldPoint(screenPosition);
        Debug.DrawRay(worldPosition, Vector2.right * 0.1f, Color.red, 1f);
        RaycastHit2D hit = Physics2D.Raycast(worldPosition, Vector2.zero);
        StartCoroutine(EjectAmmo());

        if (hit.collider != null)
        {
            int baseScoreValue = 10;
            Transform targetTransform = hit.transform;

            if (hit.collider.CompareTag("Bonus"))
            {
                baseScoreValue = 20;
                Transform duckTransform = hit.transform.parent;
                if (duckTransform != null && duckTransform.CompareTag("Target"))
                {
                    targetTransform = duckTransform;
                }
            }
            else if (hit.collider.CompareTag("TargetCenter"))
            {
                baseScoreValue = 30;
                targetTransform = hit.transform.parent;
            }
            else if (hit.collider.CompareTag("TargetEdge"))
            {
                baseScoreValue = 10;
                targetTransform = hit.transform.parent;
            }
            else if (hit.collider.CompareTag("Target"))
            {
                baseScoreValue = 10;
            }

            if (targetTransform.CompareTag("Target") || hit.collider.CompareTag("Bonus") ||
                hit.collider.CompareTag("TargetCenter") || hit.collider.CompareTag("TargetEdge"))
            {
                comboSystem.RegisterHit(baseScoreValue);
                int scoreValue = comboSystem.CalculateScore(baseScoreValue);

                StartCoroutine(FallAndDestroy(targetTransform));
                SpawnImpact(hit.point, targetTransform);
                UpdateScore(scoreValue);

                SpawnScorePopup(hit.point, scoreValue, baseScoreValue);
            }
        }
        else
        {
            SpawnImpact(worldPosition);
            comboSystem.RegisterMiss();
        }
    }

    private IEnumerator EjectAmmo()
    {
        if (ammoPrefab == null || ammoContainer == null)
            yield break;
        GameObject ammo = Instantiate(ammoPrefab, ammoContainer.position, ammoContainer.rotation, ammoContainer);
        SpriteRenderer ammoRenderer = ammo.GetComponent<SpriteRenderer>();
        Rigidbody2D ammoRigidbody = ammo.GetComponent<Rigidbody2D>();
        if (ammoRenderer == null)
            yield break;
        Vector2 ejectionDirection = (Vector2.up + Vector2.right * -0.5f).normalized;
        ammoRigidbody.linearVelocity = ejectionDirection * ejectionForce;
        float elapsed = 0f;
        Quaternion startRotation = ammo.transform.rotation;
        Vector3 startScale = ammo.transform.localScale;
        while (elapsed < ejectionDuration)
        {
            float progress = elapsed / ejectionDuration;
            ammo.transform.rotation = startRotation * Quaternion.Euler(0f, 0f, progress * 360f);
            Color color = ammoRenderer.color;
            color.a = 1f - progress;
            ammoRenderer.color = color;
            elapsed += Time.deltaTime;
            yield return null;
        }
        Destroy(ammo);
    }

    private IEnumerator FallAndDestroy(Transform target)
    {
        float fallDuration = 0.4f;
        float elapsed = 0f;
        Vector3 startPosition = target.position;
        Vector3 endPosition = startPosition + Vector3.down * 3f;
        Quaternion startRotation = target.rotation;
        Vector3 startScale = target.localScale;
        float maxRotation = 45f;
        float squashScale = 0.7f;

        while (elapsed < fallDuration)
        {
            float progress = elapsed / fallDuration;
            float arcHeight = Mathf.Sin(progress * Mathf.PI) * 0.5f;
            Vector3 arcPosition = startPosition + Vector3.down * progress * 3f + Vector3.up * arcHeight;
            target.position = arcPosition;
            float rotationProgress = Mathf.Sin(progress * Mathf.PI);
            float currentRotation = maxRotation * rotationProgress;
            target.rotation = startRotation * Quaternion.Euler(currentRotation, 0f, 0f);

            if (progress > 0.7f)
            {
                float squashProgress = (progress - 0.7f) / 0.3f;
                target.localScale = Vector3.Lerp(startScale, startScale * squashScale, squashProgress);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        Destroy(target.gameObject);
    }

    private void SpawnImpact(Vector3 position, Transform parent = null)
    {
        if (impactPrefab != null)
        {
            GameObject impact = Instantiate(impactPrefab, position, Quaternion.identity);
            if (parent != null)
            {
                SpriteRenderer parentRenderer = parent.GetComponent<SpriteRenderer>();
                SpriteRenderer impactRenderer = impact.GetComponent<SpriteRenderer>();
                if (parentRenderer != null && impactRenderer != null)
                {
                    impactRenderer.sortingLayerID = parentRenderer.sortingLayerID;
                    impactRenderer.sortingOrder = parentRenderer.sortingOrder + 1;
                }
                impact.transform.SetParent(parent);
            }
            Destroy(impact, 0.5f);
        }
    }

    private void SpawnScorePopup(Vector3 worldPosition, int scoreValue, int baseScoreValue)
    {
        if (scorePopupPrefab == null || scoreCanvas == null)
            return;

        Vector2 screenPosition = mainCamera.WorldToScreenPoint(worldPosition);
        Vector2 popupPosition = screenPosition + popupFixedOffset;

        GameObject popup = Instantiate(scorePopupPrefab, scoreCanvas.transform);
        RectTransform popupRect = popup.GetComponent<RectTransform>();
        popupRect.position = popupPosition;

        Canvas popupCanvas = popup.GetComponent<Canvas>();
        if (popupCanvas == null)
        {
            popupCanvas = popup.AddComponent<Canvas>();
        }
        popupCanvas.overrideSorting = true;
        popupCanvas.sortingOrder = popupSortingOrder;

        TMP_Text popupText = popup.GetComponent<TMP_Text>();
        if (popupText != null)
        {
            int currentMultiplier = comboSystem.GetCurrentMultiplier();

            if (currentMultiplier > 1)
            {
                popupText.text = $"+{baseScoreValue} x {currentMultiplier}";
                popupText.fontSize *= 1.2f;
                popupText.alignment = TextAlignmentOptions.Center;
            }
            else
            {
                popupText.text = $"+ {scoreValue}";
                popupText.alignment = TextAlignmentOptions.Left;
            }

            if (currentMultiplier == 2)
            {
                popupText.color = comboSystem.comboColor2;
                popupText.fontStyle = FontStyles.Bold;
            }
            else if (currentMultiplier == 3)
            {
                popupText.color = comboSystem.comboColor3;
                popupText.fontStyle = FontStyles.Bold | FontStyles.Italic;
            }
            else if (currentMultiplier == 4)
            {
                popupText.color = comboSystem.comboColor4;
                popupText.fontStyle = FontStyles.Bold | FontStyles.Underline;
            }
            else
            {
                popupText.color = Color.white;
                popupText.fontStyle = FontStyles.Normal;
            }

            StartCoroutine(AnimateScorePopup(popup, popupText));
        }
    }

    private IEnumerator AnimateScorePopup(GameObject popup, TMP_Text popupText)
    {
        float elapsed = 0f;
        Color startColor = popupText.color;
        RectTransform popupRect = popup.GetComponent<RectTransform>();
        Vector3 startPosition = popupRect.position;
        Vector3 endPosition = startPosition + Vector3.up * 50f;

        int currentMultiplier = comboSystem.GetCurrentMultiplier();
        bool isCombo = currentMultiplier > 1;
        float oscillationAmount = isCombo ? 10f : 0f;
        float oscillationSpeed = isCombo ? 15f : 0f;

        while (elapsed < popupLifetime)
        {
            float progress = elapsed / popupLifetime;
            Vector3 currentPosition = Vector3.Lerp(startPosition, endPosition, progress);

            if (isCombo)
            {
                currentPosition.x += Mathf.Sin(elapsed * oscillationSpeed) * oscillationAmount * (1f - progress);
            }

            popupRect.position = currentPosition;
            Color newColor = startColor;
            newColor.a = Mathf.Lerp(1f, 0f, progress);
            popupText.color = newColor;

            elapsed += Time.deltaTime;
            yield return null;
        }

        Destroy(popup);
    }

    private void UpdateScore(int points)
    {
        score += points;
        scoreText.text = score.ToString();
    }
}