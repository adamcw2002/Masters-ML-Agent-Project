using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bin : MonoBehaviour, IInteractable
{
    public void Interact(PlayerInteract player, GameObject ingredientHolding)
    {
        if (ingredientHolding == null) return;

        player.RemoveItem();

        Destroy(ingredientHolding);
    }
}

