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

    private int score = 0;
    private Camera mainCamera;

    private void Awake()
    {
        mainCamera = Camera.main;
        if (fireButton != null)
            fireButton.onClick.AddListener(Fire);
    }

    private void Fire()
    {
        Vector3 screenPosition = crosshairTransform.position;
        Vector2 worldPosition = mainCamera.ScreenToWorldPoint(screenPosition);
        Debug.DrawRay(worldPosition, Vector2.right * 0.1f, Color.red, 1f);
        RaycastHit2D hit = Physics2D.Raycast(worldPosition, Vector2.zero);

        if (hit.collider != null && hit.collider.CompareTag("Target"))
        {
            StartCoroutine(FallAndDestroy(hit.transform));
            SpawnImpact(hit.point, hit.transform);
            UpdateScore(10);
        }
        else
        {
            SpawnImpact(worldPosition);
        }
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

    private void UpdateScore(int points)
    {
        score += points;
        scoreText.text = "Score" + score.ToString();
    }
}