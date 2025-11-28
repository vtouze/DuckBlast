using UnityEngine;
using UnityEngine.InputSystem;

public class GunController : MonoBehaviour
{
    [SerializeField] private InputActionReference moveActionReference;
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float minX = -8f;
    [SerializeField] private float maxX = 8f;
    [SerializeField] private float yPosition = -4f;
    [SerializeField] private float floatAmplitude = 0.1f;
    [SerializeField] private float floatSpeed = 1f;

    private Vector3 initialPosition;
    private float floatOffset;

    private void Start()
    {
        initialPosition = transform.position;
    }

    private void Update()
    {
        float horizontalInput = moveActionReference.action.ReadValue<Vector2>().x;
        Vector3 newPosition = transform.position;
        newPosition.x += horizontalInput * moveSpeed * Time.deltaTime;
        newPosition.x = Mathf.Clamp(newPosition.x, minX, maxX);
        newPosition.y = yPosition;

        floatOffset = Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
        newPosition.y += floatOffset;

        transform.position = newPosition;
    }
}