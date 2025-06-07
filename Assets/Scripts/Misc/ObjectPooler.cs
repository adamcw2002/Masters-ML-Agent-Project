using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class ObjectPooler : MonoSingleton<ObjectPooler>
{
    private Dictionary<string, Queue<GameObject>> poolDictionary = new Dictionary<string, Queue<GameObject>>();

    public void CreatePool(string key, GameObject prefab, int size)
    {
        if (!poolDictionary.ContainsKey(key))
        {
            poolDictionary[key] = new Queue<GameObject>();

            for (int i = 0; i < size; i++)
            {
                GameObject obj = Instantiate(prefab);

                obj.transform.SetParent(transform, false);
                obj.SetActive(false);

                poolDictionary[key].Enqueue(obj);
            }
        }
    }

    public GameObject GetFromPool(string key, Vector3 position, Transform parent = null)
    {
        if (poolDictionary.ContainsKey(key) && poolDictionary[key].Count > 0)
        {
            GameObject obj = poolDictionary[key].Dequeue();
            obj.SetActive(true);
            obj.transform.position = position;
            if (parent != null) obj.transform.parent = parent;
            return obj;
        }

        Debug.LogWarning($"NO OBJECTS LEFT IN {key} POOL");
        return null; // Optionally handle empty pool case
    }

    public List<GameObject> GetAllActive(string key)
    {
        List<GameObject> activeObjects = new List<GameObject>();

        if (poolDictionary.ContainsKey(key))
        {
            foreach (var obj in poolDictionary[key])
            {
                if (obj.activeSelf)
                {
                    activeObjects.Add(obj);
                }
            }
        }
        else
        {
            Debug.LogWarning($"Pool with key '{key}' does not exist.");
        }

        return activeObjects;
    }

    public void ReturnToPool(string key, GameObject obj)
    {
        if (obj != null && poolDictionary.ContainsKey(key))
        {
            obj.SetActive(false);
            obj.transform.SetParent(transform, false);
            poolDictionary[key].Enqueue(obj);
        }
    }
}
