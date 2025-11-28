using UnityEngine;

public class FloatAndMove : MonoBehaviour
{
    public float moveSpeed = 2f;
    public bool moveRight = true;
    public float floatAmplitude = 0.2f;
    public float floatSpeed = 1f;
    private Vector3 initialPosition;
    private float floatOffset;

    private void Start()
    {
        initialPosition = transform.position;
    }

    private void Update()
    {
        float direction = moveRight ? 1f : -1f;
        transform.Translate(Vector3.right * direction * moveSpeed * Time.deltaTime);

        floatOffset = Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
        transform.position = new Vector3(
            transform.position.x,
            initialPosition.y + floatOffset,
            transform.position.z
        );

        if (Mathf.Abs(transform.position.x) > 15f)
        {
            Destroy(gameObject);
        }
    }
}