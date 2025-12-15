using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using System.Collections;

public class MainMenuShootingManager : MonoBehaviour
{
    [SerializeField] private Button fireButton;
    [SerializeField] private Transform crosshairTransform;
    [SerializeField] private GameObject ammoPrefab;
    [SerializeField] private Transform ammoContainer;
    [SerializeField] private GameObject impactPrefab;
    private float ejectionForce = 3f;
    private float ejectionDuration = 0.8f;

    public UnityEvent onPlay;
    public UnityEvent onLeaderboard;
    public UnityEvent onToggleSound;
    public UnityEvent onCredits;

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
        RaycastHit2D hit = Physics2D.Raycast(worldPosition, Vector2.zero);

        StartCoroutine(EjectAmmo());

        if (hit.collider != null)
        {
            SpawnImpact(hit.point, hit.transform);

            if (hit.collider.CompareTag("PlayButton"))
                onPlay.Invoke();
            else if (hit.collider.CompareTag("LeaderboardButton"))
                onLeaderboard.Invoke();
            else if (hit.collider.CompareTag("SoundButton"))
                onToggleSound.Invoke();
            else if (hit.collider.CompareTag("CreditsButton"))
                onCredits.Invoke();
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
}