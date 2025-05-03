using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeliveryStation : MonoBehaviour, IInteractable
{
    public void Interact(PlayerInteract player, GameObject itemHolding)
    {
        if (itemHolding == null)
        {
            Debug.Log("Player not holding an item");
            return;
        }

        Plate plate = itemHolding.GetComponent<Plate>();

        if (RecipeManager.Instance.CompleteRecipe(plate))
        {
            player.RemoveItem();
            Destroy(itemHolding);
        }
    }
}
