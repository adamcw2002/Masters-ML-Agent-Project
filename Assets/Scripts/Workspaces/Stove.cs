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

        Debug.Log(insidePotDefaultMaterial);
    }

    protected override void UpdateVisual()
    {
        base.UpdateVisual();

        Debug.Log("UPDATE VISUAL");

        insidePotRenderer.material = insidePotDefaultMaterial;

        if (storedItems.Count > 0)
        {
            IngredientItem ingredient = storedItems[0].GetComponent<IngredientItem>();

            Material ingredientMaterial = ingredient.IngredientData.ingredientMaterial;

            if (ingredientMaterial) insidePotRenderer.material = ingredientMaterial;
        }
    }
}
