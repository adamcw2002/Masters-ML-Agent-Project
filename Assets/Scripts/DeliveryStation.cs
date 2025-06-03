using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeliveryStation : MonoBehaviour, IInteractable
{
    [SerializeField] private bool allowIncorrectRecipes = false;

    public void Interact(PlayerInteract player, GameObject itemHolding)
    {
        if (itemHolding == null || itemHolding.TryGetComponent<Plate>(out _) == false)
        {
            Debug.Log("Player not holding a plate");
            return;
        }

        Plate plate = itemHolding.GetComponent<Plate>();

        if (plate.StoredItems.Count == 0 )
        {
            Debug.Log("Cannot deliver empty plate");
            return;
        }

        bool isRecipeCorrect = RecipeManager.Instance.CompleteRecipe(plate);

        if (!isRecipeCorrect) Debug.Log("Incorrect recipe delivered");

        if (allowIncorrectRecipes || isRecipeCorrect)
        {
            player.RemoveItem();
            Destroy(itemHolding);
        }
    }
}
