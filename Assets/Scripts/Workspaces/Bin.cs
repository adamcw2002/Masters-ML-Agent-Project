using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using System;

public class Bin : MonoBehaviour, IInteractable
{
    public static event EventHandler OnPlateBinned;

    public void Interact(PlayerInteract player, GameObject ingredientHolding)
    {
        if (ingredientHolding == null) return;

        if (ingredientHolding.TryGetComponent(out Plate plate))
        {
            player.RemoveItem();
            plate.RemoveAllItems();
            OnPlateBinned?.Invoke(plate, EventArgs.Empty);
        }
    }
}

