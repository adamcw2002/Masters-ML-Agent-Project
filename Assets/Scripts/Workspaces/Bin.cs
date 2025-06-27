using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using System;

public class Bin : MonoBehaviour, IInteractable
{
    public static event EventHandler OnDishBinned;

    public void Interact(PlayerInteract player, GameObject ingredientHolding)
    {
        if (ingredientHolding == null) return;

        player.RemoveItem();

        Destroy(ingredientHolding);

        OnDishBinned?.Invoke(this, EventArgs.Empty);
    }
}

