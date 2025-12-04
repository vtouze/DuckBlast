using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private InputActionReference moveActionReference;

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 1500f;
    [SerializeField] private float smoothTime = 0.1f;
    [SerializeField] private float deadZone = 0.1f;

    [Header("References")]
    [SerializeField] private RectTransform canvasRectTransform;
    [SerializeField] private RectTransform crosshairRectTransform;

    private Vector2 velocity = Vector2.zero;
    private Vector2 targetPosition;

    private void Update()
    {
        if (!enabled) return;

        Vector2 moveDirection = moveActionReference.action.ReadValue<Vector2>();
        moveDirection = ApplyDeadZone(moveDirection);

        targetPosition = crosshairRectTransform.anchoredPosition +
                        moveDirection * moveSpeed * Time.deltaTime;

        Vector2 minPosition = canvasRectTransform.rect.min + crosshairRectTransform.sizeDelta / 2;
        Vector2 maxPosition = canvasRectTransform.rect.max - crosshairRectTransform.sizeDelta / 2;
        targetPosition.x = Mathf.Clamp(targetPosition.x, minPosition.x, maxPosition.x);
        targetPosition.y = Mathf.Clamp(targetPosition.y, minPosition.y, maxPosition.y);

        crosshairRectTransform.anchoredPosition = Vector2.SmoothDamp(
            crosshairRectTransform.anchoredPosition,
            targetPosition,
            ref velocity,
            smoothTime
        );
    }

    private Vector2 ApplyDeadZone(Vector2 input)
    {
        if (input.magnitude < deadZone)
        {
            return Vector2.zero;
        }

        return input.normalized * ((input.magnitude - deadZone) / (1 - deadZone));
    }
}