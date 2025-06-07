using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlateSpawner : MonoBehaviour, IInteractable
{
    [SerializeField] private GameObject platePrefab;

    public void Interact(PlayerInteract player, GameObject itemHolding)
    {
        GameObject startItem = null;

        // Player is holding an item
        if (itemHolding != null)
        {
            // Player is holding a plate, do nothing
            if (itemHolding.TryGetComponent(out Plate plate))
            {
                return;
            }

            // Player is holding an ingredient, add it to plate
            if (itemHolding.TryGetComponent(out IngredientItem ingredient))
            {
                startItem = player.RemoveItem();
            }
        }

        SpawnPlate(player, startItem);
    }

    private void SpawnPlate(PlayerInteract player, GameObject startItem)
    {
        if (platePrefab == null) return;

        Vector3 position = transform.position + Vector3.up;

        GameObject newPlate = Instantiate(platePrefab, position, Quaternion.identity);

        // Get plate script
        Plate plateController = newPlate.GetComponent<Plate>();
        if (newPlate == null) plateController = newPlate.AddComponent<Plate>();

        if (plateController != null)
        {
            // Add start ingredient to plate
            if (startItem != null) plateController.AddItem(startItem);

            player.PickupItem(newPlate);
        }
    }
}
