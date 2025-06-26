using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Oven : Workspace, IInteractable
{
    public override bool AddItems(PortableStorage storage)
    {
        List<RecipeData> allRecipes = RecipeManager.Instance.GetAllRecipes();

        // Make list of stored ingredient data
        List<IngredientData> storageIngredients = new List<IngredientData>();
        foreach (var item in storage.StoredItems)
        {
            if (item.TryGetComponent(out IngredientItem ingredient))
            {
                storageIngredients.Add(ingredient.IngredientData);
            }
        }

        // Compare stored ingredients with all recipes
        foreach (var recipe in allRecipes)
        {
            if (recipe.baseRequiredIngredients[0].requiredState != IngredientState.Cooked) continue;

            var requiredIngredients = recipe.baseRequiredIngredients
                                            .Select(req => req.ingredient)
                                            .ToList();

            // Must match count and contain same ingredients (ignoring order)
            if (requiredIngredients.Count == storageIngredients.Count &&
                !requiredIngredients.Except(storageIngredients).Any() &&
                !storageIngredients.Except(requiredIngredients).Any())
            {
                // Match found
                return base.AddItems(storage);
            }
        }

        // No match found
        return false;
    }
}
