using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CookingPot : PortableStorage
{
    [Header("Cooking Pot")]
    [SerializeField] private Renderer insidePotRenderer;
    private Material insidePotDefaultMaterial;

    private void Start()
    {
        insidePotDefaultMaterial = insidePotRenderer.material;
    }

    protected override void UpdateVisual()
    {
        if (StoredItems.Count == 0)
        {
            insidePotRenderer.material = insidePotDefaultMaterial;
            return;
        }

        IngredientState ingredientState = StoredItems[0].GetComponent<IngredientItem>().CurrentState;

        GameObject rawPrefab = StoredItems[0].transform.Find(ingredientState.ToString()).gameObject;

        insidePotRenderer.material = rawPrefab.GetComponent<Renderer>().material;
    }
}
