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

            RecipeData recipeOnPlate = recipeManager.CanCombineIngredients(this);

            if (recipeOnPlate != null) CombineIngredients(recipeOnPlate);
        }

        return baseResult;
    }

    private void CombineIngredients(RecipeData recipe)
    {
        DestroyAllItems();

        IngredientData productData = recipe.finalProductData;

        GameObject recipePrefab = productData.GetPrefabFromState(recipe.finalProductState);
        GameObject recipeObject = Instantiate(recipePrefab);

        if (recipeObject.TryGetComponent(out IngredientItem ingredient) == false) ingredient = recipeObject.AddComponent<IngredientItem>();
        ingredient.SetIngredientData(productData, recipe.finalProductState);

        AddItem(recipeObject);
    }
}