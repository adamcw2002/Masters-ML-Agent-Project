using System.Collections.Generic;
using UnityEngine;

public class LooseIngredientManager : MonoSingleton<LooseIngredientManager>
{
    private int maxLooseItems = 10;
    private List<IngredientItem> looseItems = new List<IngredientItem>();

    public void AddLooseItem(GameObject item)
    {
        if (item.TryGetComponent(out IngredientItem ingredient)) AddLooseItem(ingredient);
    }

    public void AddLooseItem(IngredientItem item)
    {
        if (CanAcceptLooseItem && item != null && !looseItems.Contains(item))
        {
            Debug.Log("Added Loose Item");

            looseItems.Add(item);
        }
    }

    public void RemoveLooseItem(GameObject item)
    {
        if (item.TryGetComponent(out IngredientItem ingredient)) RemoveLooseItem(ingredient);
    }

    public void RemoveLooseItem(IngredientItem item)
    {
        if (item != null && looseItems.Contains(item))
        {
            looseItems.Remove(item);
        }
    }

    public bool ContainsItem(IngredientItem item)
    {
        return looseItems.Contains(item);
    }

    public List<IngredientItem> GetAllLooseItems()
    {
        return new List<IngredientItem>(looseItems);
    }

    public bool CanAcceptLooseItem => looseItems.Count < maxLooseItems;

    public void ClearAllLooseItems()
    {
        looseItems.Clear();
    }
}
