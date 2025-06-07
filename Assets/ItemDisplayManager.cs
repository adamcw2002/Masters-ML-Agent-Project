using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using Unity.Properties;
using UnityEngine;

public class ItemDisplayManager : MonoSingleton<ItemDisplayManager>
{
    [SerializeField] private GameObject itemDisplayPrefab;
    [SerializeField] private Canvas worldSpaceCanvas;
    private Transform container = null;

    public const string poolKey = "itemDisplay";

    private void Start()
    {
        container = new GameObject().transform;
        container.transform.parent = worldSpaceCanvas.transform;
        container.name = "Item Display Container";

        ObjectPooler.Instance.CreatePool(poolKey, itemDisplayPrefab, 10);
    }

    public ItemDisplay CreateItemDisplay(Transform objectTransform, IngredientData data = null)
    {
        GameObject itemDisplay = ObjectPooler.Instance.GetFromPool(poolKey, objectTransform.position);
        itemDisplay.transform.SetParent(container);

        if (itemDisplay.TryGetComponent(out ItemDisplay controller))
        {
            controller.Init(objectTransform, data);
            return controller;
        }

        return null;
    }
}
