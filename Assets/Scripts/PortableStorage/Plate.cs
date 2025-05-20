using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Plate : PortableStorage
{
    public override bool AddItem(GameObject item)
    {
        bool baseResult = base.AddItem(item);

        if (baseResult)
        {
            RecipeManager recipeManager = RecipeManager.Instance;

            foreach (var recipe in recipeManager.GetAvailableRecipes())
            {
                //Debug.Log("Checking recipe: " + recipe.recipeName);

                StoredItems[0].TryGetComponent(out IngredientItem storedIngredient);

                if (StoredItems.Count == 1 && storedIngredient != null && recipe.finalProductData == storedIngredient.IngredientData)
                {
                    //Debug.Log("Recipe already completed on plate");

                    return true;
                }

                if (recipeManager.CanCombineIngredients(this, recipe))
                {
                    //Debug.Log("Combining ingredients on plate");

                    CombineIngredients(recipe);

                    return true;
                }
            }
        }

        return baseResult;
    }

    private void CombineIngredients(RecipeData recipe)
    {
        RemoveAllItems();

        IngredientData productData = recipe.finalProductData;

        GameObject recipePrefab = productData.GetPrefabFromState(recipe.finalProductState);
        GameObject recipeObject = Instantiate(recipePrefab);

        if (recipeObject.TryGetComponent(out IngredientItem ingredient) == false) ingredient = recipeObject.AddComponent<IngredientItem>();
        ingredient.SetIngredientData(productData, recipe.finalProductState);

        AddItem(recipeObject);
    }
}