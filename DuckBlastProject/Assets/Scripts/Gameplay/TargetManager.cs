using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class FloatRange
{
    public float min;
    public float max;

    public float GetRandomValue()
    {
        return Random.Range(min, max);
    }
}

[System.Serializable]
public class SpawnLine
{
    public Transform lineTransform;
    public FloatRange spawnInterval = new FloatRange { min = 1.5f, max = 2.5f };
    public FloatRange moveSpeed = new FloatRange { min = 1.5f, max = 2.5f };
    public bool moveRight = true;
    public List<GameObject> prefabs;
    [HideInInspector] public float timer;
    [HideInInspector] public float currentSpawnInterval;
    [HideInInspector] public float currentMoveSpeed;
}

public class TargetManager : MonoBehaviour
{
    public List<SpawnLine> spawnLines;
    public FloatRange floatAmplitude = new FloatRange { min = 0.1f, max = 0.3f };
    public FloatRange floatSpeed = new FloatRange { min = 0.8f, max = 1.2f };
    public FloatRange initialOffsetRange = new FloatRange { min = 2f, max = 5f };

    private bool isActive = false;

    private void OnEnable()
    {
        isActive = true;

        foreach (SpawnLine line in spawnLines)
        {
            line.currentSpawnInterval = line.spawnInterval.GetRandomValue();
            line.currentMoveSpeed = line.moveSpeed.GetRandomValue();
        }

        SpawnInitialTargets();
    }

    private void SpawnInitialTargets()
    {
        if (spawnLines.Count > 0)
        {
            float offset = 0f;
            for (int i = 0; i < 2; i++)
            {
                offset += initialOffsetRange.GetRandomValue();
                SpawnTarget(spawnLines[0], offset);
            }
        }

        if (spawnLines.Count > 1)
        {
            float offset = 0f;
            for (int i = 0; i < 2; i++)
            {
                offset += initialOffsetRange.GetRandomValue();
                SpawnTarget(spawnLines[1], offset);
            }
        }

        if (spawnLines.Count > 2)
        {
            SpawnTarget(spawnLines[2], initialOffsetRange.GetRandomValue() * 0.5f);
        }
    }

    private void Update()
    {
        if (!isActive) return;

        foreach (SpawnLine line in spawnLines)
        {
            line.timer += Time.deltaTime;
            if (line.timer >= line.currentSpawnInterval)
            {
                SpawnTarget(line);
                line.timer = 0f;

                line.currentSpawnInterval = line.spawnInterval.GetRandomValue();
                line.currentMoveSpeed = line.moveSpeed.GetRandomValue();
            }
        }
    }

    private void SpawnTarget(SpawnLine line, float xOffset = 0f)
    {
        GameObject prefab = line.prefabs[Random.Range(0, line.prefabs.Count)];

        Vector3 spawnPosition = GetSpawnPosition(line);

        if (line.moveRight)
        {
            spawnPosition.x += xOffset;
        }
        else
        {
            spawnPosition.x -= xOffset;
        }

        GameObject target = Instantiate(prefab, spawnPosition, Quaternion.identity);

        FloatAndMove floatAndMove = target.GetComponent<FloatAndMove>();
        if (floatAndMove == null)
        {
            floatAndMove = target.AddComponent<FloatAndMove>();
        }

        floatAndMove.moveSpeed = line.currentMoveSpeed;
        floatAndMove.moveRight = line.moveRight;

        floatAndMove.floatAmplitude = floatAmplitude.GetRandomValue();
        floatAndMove.floatSpeed = floatSpeed.GetRandomValue();
    }

    private Vector3 GetSpawnPosition(SpawnLine line)
    {
        float xPosition = line.moveRight ? -10f : 10f;
        float yPosition = line.lineTransform.position.y;
        return new Vector3(xPosition, yPosition, 0f);
    }
}