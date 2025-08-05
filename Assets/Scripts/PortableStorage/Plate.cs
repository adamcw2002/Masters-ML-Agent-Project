using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Plate : PortableStorage
{
    public static event EventHandler<IngredientEventArgs> OnAnyCombineIngredients;

    public static event EventHandler OnAnyExtraIngredientsToActiveRecipe;

    private bool hasCombinedIngredients = false;
    public bool HasCombinedIngredients => hasCombinedIngredients;

    private bool createdFinalRecipeOnPlate = false;

    public override int GetStorageID()
    {
        return 1;
    }

    public override bool AddItem(GameObject item, bool tryCombine = true)
    {
        bool baseResult = base.AddItem(item);

        if (baseResult)
        {
            //IF FINAL RECIPE HAD ALREADY BEEN CREATED, ADDING THE EXTRA INGREDIENT RUINED IT
            if (createdFinalRecipeOnPlate) OnAnyExtraIngredientsToActiveRecipe?.Invoke(this, EventArgs.Empty);

            RecipeManager recipeManager = RecipeManager.Instance;

            RecipeData recipeOnPlate = recipeManager.CanCombineIngredients(this);

            if (recipeOnPlate != null && tryCombine)
            {
                CombineIngredients(recipeOnPlate);
            }

            if (RecipeManager.Instance.CheckCompletedRecipe(this, RecipeManager.Instance.GetActiveRecipe())) createdFinalRecipeOnPlate = true;

            hasCombinedIngredients = false;
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

        AddItem(recipeObject, false);

        hasCombinedIngredients = true;


        OnAnyCombineIngredients?.Invoke(this, new IngredientEventArgs(ingredient));
    }

    public override GameObject RemoveItem(GameObject item)
    {
        hasCombinedIngredients = false;
        createdFinalRecipeOnPlate = false;

        return base.RemoveItem(item);
    }

    public override void DestroyAllItems()
    {
        hasCombinedIngredients = false;
        createdFinalRecipeOnPlate = false;

        base.DestroyAllItems(); 
    }
}