using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkyPrefabSpawner : MonoBehaviour
{
    [SerializeField] private GameObject prefab;
    [SerializeField] private Transform targetTransform;
    [SerializeField] private float spawnHeightAbove = 3f;
    [SerializeField] private float spawnInterval = 2f;

    private readonly Queue<GameObject> pool = new Queue<GameObject>();
    private readonly List<GameObject> activeItems = new List<GameObject>();

    private void Awake()
    {
        if (prefab == null)
        {
            Debug.LogError("SkyPrefabSpawner: 未指定 prefab。", this);
            enabled = false;
            return;
        }

        if (targetTransform == null)
        {
            Debug.LogError("SkyPrefabSpawner: 未指定 targetTransform。", this);
            enabled = false;
            return;
        }
    }

    private void OnEnable()
    {
        StartCoroutine(SpawnLoop());
    }

    private void OnDisable()
    {
        StopAllCoroutines();
    }

    private IEnumerator SpawnLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnInterval);
            SpawnOne();
        }
    }

    private void SpawnOne()
    {
        var obj = GetFromPool();
        if (obj == null) return;

        Vector3 spawnPos = targetTransform.position + Vector3.up * spawnHeightAbove;
        obj.transform.position = spawnPos;
        obj.transform.rotation = Quaternion.identity;
        obj.SetActive(true);

        activeItems.Add(obj);
    }

    private GameObject GetFromPool()
    {
        if (pool.Count > 0)
        {
            return pool.Dequeue();
        }

        var obj = Instantiate(prefab, transform);
        obj.SetActive(false);
        return obj;
    }

    public void ReturnToPool(GameObject obj)
    {
        if (obj == null) return;

        obj.SetActive(false);
        obj.transform.SetParent(transform);
        pool.Enqueue(obj);
    }

    public void ClearAll()
    {
        for (int i = activeItems.Count - 1; i >= 0; i--)
        {
            ReturnToPool(activeItems[i]);
        }
        activeItems.Clear();
    }
}