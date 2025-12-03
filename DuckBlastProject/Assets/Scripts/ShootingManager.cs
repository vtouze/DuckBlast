using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class ShootingManager : MonoBehaviour
{
    [SerializeField] private Button fireButton;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private GameObject impactPrefab;
    [SerializeField] private Transform crosshairTransform;
    [SerializeField] private GameObject ammoPrefab;
    [SerializeField] private Transform ammoContainer;
    private float ejectionForce = 3f;
    private float ejectionDuration = 0.8f;
    [SerializeField] private GameObject scorePopupPrefab;
    private float popupLifetime = 1f;
    private Vector2 popupFixedOffset = new Vector2(50f, 50f);
    private int popupSortingOrder = 10;
    [SerializeField] private Canvas scoreCanvas;
    private int score = 0;
    private Camera mainCamera;
    private bool controlsEnabled = true;

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
            int scoreValue = 10;
            Transform targetTransform = hit.transform;
            if (hit.collider.CompareTag("Bonus"))
            {
                scoreValue = 20;
                Transform duckTransform = hit.transform.parent;
                if (duckTransform != null && duckTransform.CompareTag("Target"))
                {
                    targetTransform = duckTransform;
                }
            }
            else if (hit.collider.CompareTag("TargetCenter"))
            {
                scoreValue = 30;
                targetTransform = hit.transform.parent;
            }
            else if (hit.collider.CompareTag("TargetEdge"))
            {
                scoreValue = 10;
                targetTransform = hit.transform.parent;
            }
            else if (hit.collider.CompareTag("Target"))
            {
                scoreValue = 10;
            }
            if (targetTransform.CompareTag("Target") || hit.collider.CompareTag("Bonus") ||
                hit.collider.CompareTag("TargetCenter") || hit.collider.CompareTag("TargetEdge"))
            {
                StartCoroutine(FallAndDestroy(targetTransform));
                SpawnImpact(hit.point, targetTransform);
                UpdateScore(scoreValue);
                SpawnScorePopup(hit.point, scoreValue);
            }
        }
        else
        {
            SpawnImpact(worldPosition);
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

    private void SpawnScorePopup(Vector3 worldPosition, int scoreValue)
    {
        if (scorePopupPrefab == null || scoreCanvas == null)
            return;
        Vector2 screenPosition = mainCamera.WorldToScreenPoint(worldPosition);
        Vector2 fixedOffset = popupFixedOffset;
        Vector2 popupPosition = screenPosition + fixedOffset;
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
            popupText.text = "+" + scoreValue.ToString();
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
        while (elapsed < popupLifetime)
        {
            float progress = elapsed / popupLifetime;
            popupRect.position = Vector3.Lerp(startPosition, endPosition, progress);
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