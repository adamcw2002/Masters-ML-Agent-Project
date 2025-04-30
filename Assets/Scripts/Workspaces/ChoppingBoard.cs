using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChoppingBoard : Workspace, IInteractable
{
    public override bool CanProcessItem(GameObject item)
    {
        IngredientItem ingredientItem = item.GetComponent<IngredientItem>();

        //If its not an ingredient, then cannot process
        if (ingredientItem == null) return false;

        //If item is already chopped, then cannot process
        if (ingredientItem.CurrentState == IngredientState.Chopped) return false;

        return ingredientItem.IngredientData.CheckPossibleStates(IngredientState.Chopped);
    }

    protected override void CompleteProcessing()
    {
        if (storedItems.Count == 0)
            return;

        // Get the ingredient on the chopping board
        GameObject ingredient = storedItems[0];
        IngredientItem ingredientItem = ingredient.GetComponent<IngredientItem>();

        if (ingredientItem != null)
        {
            // Change the ingredient state to chopped
            ingredientItem.ChangeState(IngredientState.Chopped);
        }
    }
}