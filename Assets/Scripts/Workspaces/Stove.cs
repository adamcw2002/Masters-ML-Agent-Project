using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Stove : Workspace
{
    [Header("Cooking Pot")]
    [SerializeField] private Renderer insidePotRenderer;
    private Material insidePotDefaultMaterial;

    protected override void Start()
    {
        base.Start();

        insidePotDefaultMaterial = insidePotRenderer.material;
    }

    protected override void UpdateVisual()
    {
        base.UpdateVisual();

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
