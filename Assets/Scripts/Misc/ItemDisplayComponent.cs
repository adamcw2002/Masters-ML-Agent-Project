using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemDisplayComponent : MonoBehaviour
{
    protected ItemDisplay itemDisplay = null;

    private void OnDestroy()
    {
        RemoveItemDisplay();
    }

    public void AddItemDisplay(float yOffset = 1f)
    {
        if (itemDisplay == null) itemDisplay = ItemDisplayManager.Instance.CreateItemDisplay(transform, null, yOffset);
    }

    public void RemoveItemDisplay()
    {
        itemDisplay?.ReturnToPool();
        itemDisplay = null;
    }

    public void UpdateItemDisplay(List<GameObject> storedItems) => itemDisplay?.UpdateItemDisplay(storedItems);
}

