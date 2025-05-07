using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Stove : Workspace
{
    [Header("Cooking Pot")]
    [SerializeField] private Renderer insidePotRenderer;
    private Material insidePotDefaultMaterial;

    private void Start()
    {
        insidePotDefaultMaterial = insidePotRenderer.material;
    }

    public override bool CanProcessItem(GameObject item)
    {
        if (canProcessItems == false) return true;

        IngredientItem ingredientItem = item.GetComponent<IngredientItem>();

        //If its not an ingredient, then cannot process
        if (ingredientItem == null) return false;

        //If item is already in the new state, then cannot process
        if (ingredientItem.CurrentState == outputState) return false;

        return ingredientItem.IngredientData.CheckPossibleStates(outputState);
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
            ingredientItem.ChangeState(outputState);
        }
    }

    protected override void UpdateVisual()
    {
        if (storedItems.Count == 0)
        {
            insidePotRenderer.material = insidePotDefaultMaterial;
            return;
        }

        IngredientItem ingredient = storedItems[0].GetComponent<IngredientItem>();

        Material ingredientMaterial = ingredient.IngredientData.ingredientMaterial;

        if (ingredientMaterial) insidePotRenderer.material = ingredientMaterial;
    }
}
