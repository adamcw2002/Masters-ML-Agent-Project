using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Stove : Workspace
{
    [Header("Stove")]
    [SerializeField] private GameObject startingPot;

    private void Start()
    {
        AddItem(startingPot);
    }

    public override bool CanProcessItem(GameObject item)
    {
        IngredientItem ingredientItem = item.GetComponent<IngredientItem>();

        //If its not an ingredient, then cannot process
        if (ingredientItem == null) return false;

        //If item is already chopped, then cannot process
        if (ingredientItem.CurrentState == IngredientState.Cooked) return false;

        return ingredientItem.IngredientData.CheckPossibleStates(IngredientState.Cooked);
    }

    protected override void CompleteProcessing()
    {
        if (storedItems.Count == 0)
            return;

        // Get the ingredient
        CookingPot pot = storedItems[0].GetComponent<CookingPot>();
        GameObject ingredient = pot?.StoredItems[0];
        IngredientItem ingredientItem = ingredient?.GetComponent<IngredientItem>();

        if (ingredientItem != null)
        {
            // Change the ingredient state to chopped
            ingredientItem.ChangeState(IngredientState.Cooked);
        }
    }
}
