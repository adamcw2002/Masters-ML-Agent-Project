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

    public void AddItemDisplay()
    {
        if (itemDisplay == null) itemDisplay = ItemDisplayManager.Instance.CreateItemDisplay(transform);
    }

    public void RemoveItemDisplay()
    {
        itemDisplay?.ReturnToPool();
        itemDisplay = null;
    }

    public void AddNewIcon(IngredientData data) => itemDisplay?.AddNewIcon(data);

    public void RemoveIcon(IngredientData data) => itemDisplay.RemoveIcon(data);
    public void RemoveIcon(Sprite icon) => itemDisplay.RemoveIcon(icon);
}

