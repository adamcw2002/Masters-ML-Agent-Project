using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChoppingBoard : Workspace, IInteractable
{
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
            ingredientItem.ChangeState(outputState);
        }
    }

    protected override void UpdateVisual() { }
}