using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Hob : Workspace
{
    [Header("Hob")]
    [SerializeField] private Renderer insidePotRenderer;
    private Material insidePotDefaultMaterial;

    private void Start()
    {
        insidePotDefaultMaterial = insidePotRenderer.material;
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
