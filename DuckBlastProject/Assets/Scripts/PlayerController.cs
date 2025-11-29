using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private InputActionReference moveActionReference;
    private float moveSpeed = 1500;
    [SerializeField] private RectTransform canvasRectTransform;
    [SerializeField] private RectTransform crosshairRectTransform;

    private void Update()
    {
        if (!enabled) return;

        Vector2 moveDirection = moveActionReference.action.ReadValue<Vector2>();
        Vector2 currentPosition = crosshairRectTransform.anchoredPosition;
        Vector2 newPosition = currentPosition + moveDirection * moveSpeed * Time.deltaTime;
        Vector2 minPosition = canvasRectTransform.rect.min + crosshairRectTransform.sizeDelta / 2;
        Vector2 maxPosition = canvasRectTransform.rect.max - crosshairRectTransform.sizeDelta / 2;
        newPosition.x = Mathf.Clamp(newPosition.x, minPosition.x, maxPosition.x);
        newPosition.y = Mathf.Clamp(newPosition.y, minPosition.y, maxPosition.y);
        crosshairRectTransform.anchoredPosition = newPosition;
    }
}