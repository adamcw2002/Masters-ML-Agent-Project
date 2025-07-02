using System.Collections.Generic;
using UnityEngine;

public class LooseIngredientManager : MonoSingleton<LooseIngredientManager>
{
    private const int maxLooseItems = 20;
    private List<IngredientItem> looseItems = new List<IngredientItem>();
    private List<IngredientSpawner> ingredientSpawners = new List<IngredientSpawner>();

    private void Start()
    {
        BSPGridFloorPlanGenerator.OnFloorGenerated += BSPGridFloorPlanGenerator_OnFloorGenerated;

        IngredientSpawner.OnNewIngredientSpawner += IngredientSpawner_OnNewIngredientSpawner;

        Workspace.OnAnyItemAddedToWorkspace += Workspace_OnItemAddedToWorkspace;

        Workspace.OnAnyItemRemovedFromWorkspace += Workspace_OnItemRemovedFromWorkspace;
    }

    private void Workspace_OnItemRemovedFromWorkspace(object sender, IngredientEventArgs e)
    {
        RemoveLooseItem(e.IngredientItem);
    }

    private void Workspace_OnItemAddedToWorkspace(object sender, IngredientEventArgs e)
    {
        AddLooseItem(e.IngredientItem);
    }

    private void BSPGridFloorPlanGenerator_OnFloorGenerated()
    {
        ClearAllIngredientSpawners();
    }

    private void IngredientSpawner_OnNewIngredientSpawner(object sender, System.EventArgs e)
    {
        AddIngredientSpawner(sender as IngredientSpawner);
    }

    public int GetMaxLooseItems() => maxLooseItems;
    public bool CanAcceptLooseItem => looseItems.Count + ingredientSpawners.Count < maxLooseItems;

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

    private void AddIngredientSpawner(IngredientSpawner spawner)
    {
        if (spawner == null) return;

        ingredientSpawners.Add(spawner);
    }

    public List<IngredientItem> GetAllLooseItems()
    {
        return new List<IngredientItem>(looseItems);
    }

    public List<IngredientSpawner> GetAllIngredientSpawners() => ingredientSpawners;

    public void ClearAllLooseItems()
    {
        looseItems.Clear();
    }

    private void ClearAllIngredientSpawners()
    {
        ingredientSpawners.Clear();
    }
}
