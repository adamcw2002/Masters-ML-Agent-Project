using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IngredientSpawner : MonoBehaviour, IInteractable
{
    [SerializeField] private IngredientData ingredientData;

    [SerializeField] private SpriteRenderer iconRenderer;

    public IngredientData GetSpawnedIngredient() => ingredientData;

    private void Awake()
    {
        if (ingredientData) SetIngredientData(ingredientData);
    }

    public void SetIngredientData(IngredientData data)
    {
        ingredientData = data;

        // Update visual to match the ingredient type
        // You could have a display model showing what this spawner provides

        iconRenderer.sprite = data.icon;
    }

    public void Interact(PlayerInteract player, GameObject ingredientHolding)
    {
        TrySpawnIngredient(player);
    }

    private void TrySpawnIngredient(PlayerInteract player)
    {
        if (ingredientData == null || player.CanPickupItem() == false) return;

        GameObject ingredientPrefab = ingredientData.GetPrefabFromState(ingredientData.initialState);
        if (ingredientPrefab == null) return;

        Vector3 position = transform.position + Vector3.up;

        GameObject newIngredient = new GameObject();
        newIngredient.transform.position = position;
        newIngredient.name = ingredientData.ingredientName;

        // Set the ingredient data
        IngredientItem ingredientItem = GetComponent<IngredientItem>();
        if (ingredientItem == null) ingredientItem = newIngredient.AddComponent<IngredientItem>();

        if (ingredientItem != null)
        {
            ingredientItem.SetIngredientData(ingredientData);

            if (player.PickupItem(newIngredient) == false)
            {
                Destroy(newIngredient);
            }
        }
    }
}
