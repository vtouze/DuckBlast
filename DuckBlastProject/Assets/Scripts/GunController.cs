using UnityEngine;
using UnityEngine.InputSystem;

public class GunController : MonoBehaviour
{
    [SerializeField] private InputActionReference moveActionReference;

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float smoothTime = 0.1f;
    [SerializeField] private float deadZone = 0.1f;

    [Header("Position Settings")]
    [SerializeField] private float minX = -8f;
    [SerializeField] private float maxX = 8f;
    [SerializeField] private float yPosition = -4f;

    [Header("Floating Settings")]
    [SerializeField] private float floatAmplitude = 0.1f;
    [SerializeField] private float floatSpeed = 1f;

    private Vector3 initialPosition;
    private float floatOffset;
    private float velocityX;
    private float targetX;

    private void Start()
    {
        initialPosition = transform.position;
        targetX = initialPosition.x;
    }

    private void Update()
    {
        float horizontalInput = moveActionReference.action.ReadValue<Vector2>().x;
        horizontalInput = ApplyDeadZone(horizontalInput);

        targetX += horizontalInput * moveSpeed * Time.deltaTime;
        targetX = Mathf.Clamp(targetX, minX, maxX);

        float currentX = Mathf.SmoothDamp(
            transform.position.x,
            targetX,
            ref velocityX,
            smoothTime
        );

        floatOffset = Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;

        transform.position = new Vector3(
            currentX,
            yPosition + floatOffset,
            transform.position.z
        );
    }

    private float ApplyDeadZone(float input)
    {
        if (Mathf.Abs(input) < deadZone)
        {
            return 0f;
        }

        return Mathf.Sign(input) * ((Mathf.Abs(input) - deadZone) / (1 - deadZone));
    }
}