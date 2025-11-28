using UnityEngine;
using System.Collections.Generic;

public class TargetManager : MonoBehaviour
{
    [System.Serializable]
    public class SpawnLine
    {
        public Transform lineTransform;
        public float spawnInterval = 2f;
        public float moveSpeed = 2f;
        public bool moveRight = true;
        public List<GameObject> prefabs;
        [HideInInspector] public float timer;
    }

    public List<SpawnLine> spawnLines;
    public float floatAmplitude = 0.2f;
    public float floatSpeed = 1f;

    private void Update()
    {
        foreach (SpawnLine line in spawnLines)
        {
            line.timer += Time.deltaTime;
            if (line.timer >= line.spawnInterval)
            {
                SpawnTarget(line);
                line.timer = 0f;
            }
        }
    }

    private void SpawnTarget(SpawnLine line)
    {
        GameObject prefab = line.prefabs[Random.Range(0, line.prefabs.Count)];
        GameObject target = Instantiate(prefab, GetSpawnPosition(line), Quaternion.identity);

        FloatAndMove floatAndMove = target.AddComponent<FloatAndMove>();
        floatAndMove.moveSpeed = line.moveSpeed;
        floatAndMove.moveRight = line.moveRight;
        floatAndMove.floatAmplitude = floatAmplitude;
        floatAndMove.floatSpeed = floatSpeed;
    }


    private Vector3 GetSpawnPosition(SpawnLine line)
    {
        float xPosition = line.moveRight ? -10f : 10f;
        float yPosition = line.lineTransform.position.y;
        return new Vector3(xPosition, yPosition, 0f);
    }
}