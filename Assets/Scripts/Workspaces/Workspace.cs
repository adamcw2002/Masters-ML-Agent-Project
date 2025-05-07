using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Workspace : MonoBehaviour, IInteractable
{
    protected int maxItems = 1;
    [SerializeField] protected bool canProcessItems = true;
    [SerializeField] protected float processingTime = 0f;
    [SerializeField] protected IngredientState outputState;

    [SerializeField] protected bool canHoldPortableStorage = false;
    [SerializeField] protected bool mustRemoveWithPlate = false;
    [SerializeField] protected bool itemsVisibleOnStorage = true;

    protected List<GameObject> storedItems = new List<GameObject>();
    protected bool isProcessing = false;

    public virtual void Interact(PlayerInteract player, GameObject itemHolding)
    {
        if (isProcessing) return;

        //If portable storage is currently on the workspace
        if (canHoldPortableStorage && maxItems == 1 && storedItems.Count > 0 && storedItems[0].TryGetComponent(out PortableStorage storage))
        {
            Debug.Log("A Portable storage is currently on workspace, interact with the storage");
            storage.Interact(player, itemHolding);

            // Check if storage has changed parent (been picked up), remove from workspace if so
            if (storage.transform.parent != transform) RemoveItem(storage.gameObject);

            return;
        }
        
        //If player is holding an item
        if (itemHolding != null)
        {
            // Player is holding a plate and the workspace is not empty, try add from workspace onto plate
            if (itemHolding.TryGetComponent(out PortableStorage storageHolding) && storedItems.Count > 0)
            {
                Debug.Log("Player holding portable storage, adding from workspace onto plate");

                GameObject item = GetLastItem();
                if (item != null)
                {
                    if (item.TryGetComponent(out PortableStorage storedPortableStorage) && storedPortableStorage.StoredItems.Count > 0)
                    {
                        Debug.Log("Trying to take item from portable storage");

                        item = storedPortableStorage.GetLastItem();

                        if (player.PickupItem(item))
                            storedPortableStorage.RemoveItem(item);
                    }
                    else
                    {
                        Debug.Log("Trying to pickup item");

                        if (player.PickupItem(item))
                            RemoveItem(item);
                    }

                    return;
                }
            }

            // Add the item the player is holding to the workspace
            if (AddItem(itemHolding))
            {
                Debug.Log("Adding item from player to workspace");

                player.RemoveItem();
            }

            return;
        }

        //Player is not holding anything, try pick up item
        if (storedItems.Count > 0 && mustRemoveWithPlate == false)
        {
            Debug.Log("Player pick up item");

            GameObject item = GetLastItem();
            if (item != null)
            {
                if (player.PickupItem(item))
                    RemoveItem(item);
            }
        }
    }

    public virtual bool CanAcceptItem(GameObject item)
    {
        if (canHoldPortableStorage == false && item.TryGetComponent(out PortableStorage storage))
            return false;

        if (storedItems.Count >= maxItems)
            return false;

        return true;
    }

    public abstract bool CanProcessItem(GameObject item);

    protected abstract void UpdateVisual();

    public virtual bool AddItem(GameObject item)
    {
        if (canHoldPortableStorage && maxItems == 1 && storedItems.Count > 0 && storedItems[0].TryGetComponent(out PortableStorage portableStorage))
            return portableStorage.AddItem(item);

        // Check if it can accept the item
        if (!CanAcceptItem(item))
            return false;

        // Check whether it can process the item
        if (canProcessItems && !CanProcessItem(item))
            return false;

        storedItems.Add(item);
        item.transform.SetParent(transform);
        item.transform.localPosition = Vector3.up * 0.5f; // Position item on top

        if (itemsVisibleOnStorage == false) item.SetActive(false);

        if (canProcessItems && CanProcessItem(item)) StartProcessing();

        UpdateVisual();

        return true;
    }

    public virtual GameObject RemoveItem(GameObject item)
    {
        if (item == null) 
            return null;

        storedItems.Remove(item);

        UpdateVisual();

        return item;
    }

    private GameObject GetLastItem()
    {
        if (storedItems.Count == 0 || isProcessing)
            return null;

        return storedItems[storedItems.Count - 1];
    }

    protected virtual void StartProcessing()
    {
        if (storedItems.Count > 0 && !isProcessing)
        {
            StartCoroutine(ProcessItems());
        }
    }

    protected virtual System.Collections.IEnumerator ProcessItems()
    {
        isProcessing = true;

        UpdateVisual();

        yield return new WaitForSeconds(processingTime);

        // Process the items (specific to each workspace type)
        CompleteProcessing();

        UpdateVisual();

        isProcessing = false;
    }

    protected virtual void CompleteProcessing()
    {
        // Override in derived classes to implement specific processing
    }
}