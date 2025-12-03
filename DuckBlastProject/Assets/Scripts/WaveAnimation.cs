using UnityEngine;

public class WaterWaves : MonoBehaviour
{
    [System.Serializable]
    public class Wave
    {
        public Transform waveTransform;
        public bool isForeground;
        public float verticalHeight = 0.1f;
        public float verticalSpeed = 1f;
        public float horizontalAmount = 0.8f;
        public float horizontalSpeed = 1f;
    }

    [SerializeField] private Wave[] waves;

    private Vector3[] initialPositions;

    private void Start()
    {
        initialPositions = new Vector3[waves.Length];
        for (int i = 0; i < waves.Length; i++)
        {
            initialPositions[i] = waves[i].waveTransform.position;
        }
    }

    private void Update()
    {
        for (int i = 0; i < waves.Length; i++)
        {
            AnimateWave(waves[i], i);
        }
    }

    private void AnimateWave(Wave wave, int index)
    {
        float yOffset = Mathf.Sin(Time.time * wave.verticalSpeed) * wave.verticalHeight;

        float direction = wave.isForeground ? -1f : 1f;
        float xOffset = Mathf.Sin(Time.time * wave.horizontalSpeed) * wave.horizontalAmount * direction;

        wave.waveTransform.position = initialPositions[index] + new Vector3(xOffset, yOffset, 0f);
    }
}