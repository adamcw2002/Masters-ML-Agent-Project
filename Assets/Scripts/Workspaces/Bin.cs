using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using System;

public class Bin : MonoBehaviour, IInteractable
{
    public static event EventHandler<BinEventArgs> OnPlateBinned;
    public static event EventHandler<IngredientEventArgs> OnIngredientBinned;

    public void Interact(PlayerInteract player, GameObject ingredientHolding)
    {
        if (ingredientHolding == null) return;

        if (ingredientHolding.TryGetComponent(out Plate plate))
        {
            OnPlateBinned?.Invoke(this, new BinEventArgs(plate));

            player.RemoveItem();
            plate.RemoveAllItems();
        }
        else if (ingredientHolding.TryGetComponent(out IngredientItem ingredient))
        {
            OnIngredientBinned?.Invoke(this, new IngredientEventArgs(ingredient));

            GameObject removedItem = player.RemoveItem();
            if (removedItem) Destroy(removedItem);

        }
    }
}

