using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public abstract class PortableStorage : MonoBehaviour, IInteractable
{
    public static event EventHandler<IngredientEventArgs> OnAnyIngredientAddedToPortableStorage;
    public static event EventHandler OnAnyStoragePickedUp;

    [SerializeField] private int maxIngredients = 4;
    [SerializeField] private List<Transform> itemPositions;  // Optional positions where items should be placed
    [SerializeField] private float defaultItemHeight = 0.1f; // Height above plate for items
    [SerializeField] private bool showContainedItems = true;

    private List<GameObject> storedItems = new List<GameObject>();

    public int MaxIngredients => maxIngredients;
    public bool IsEmpty => storedItems.Count == 0;
    public bool IsFull => storedItems.Count >= maxIngredients;
    public float MaxItemsAcceptable => Mathf.Max(maxIngredients - storedItems.Count, 0);
    public List<GameObject> StoredItems => storedItems;

    protected ItemDisplayComponent itemDisplay = null;

    private void Start()
    {
        itemDisplay = GetComponent<ItemDisplayComponent>();
    }

    public void Interact(PlayerInteract player, GameObject itemHolding)
    {
        //
        // PLAYER HAS NO ITEMS, PICKUP STORAGE
        //
        if (itemHolding == null)
        {
            player.PickupItem(gameObject);
            OnAnyStoragePickedUp?.Invoke(this, EventArgs.Empty);
            return;
        }

        //
        // PLAYER HAS A STORAGE IN HAND
        //
        if (itemHolding.TryGetComponent(out PortableStorage playerStorage))
        {
            //
            // ADD ALL ITEMS FROM THIS STORAGE TO THE PLAYER STORAGE
            //
            if (playerStorage.StoredItems.Count == 0 && storedItems.Count > 0)
            {
                if (playerStorage.AddItems(storedItems))
                {
                    ClearAllItems();
                    return;
                }
            }

            //
            // ADD ALL ITEMS FROM PLAYER STORAGE TO THIS STORAGE
            //
            if (playerStorage.StoredItems.Count > 0 && storedItems.Count == 0)
            {
                if (AddItems(playerStorage.StoredItems))
                {
                    playerStorage.ClearAllItems();
                    return;
                }
            }
        }

        //
        // PLAYER HAS AN INGREDIENT IN HAND
        //
        if (AddItem(itemHolding))
        {
            player.RemoveItem();
        }
    }

    public bool CanAcceptItem(GameObject item)
    {
        if (item.TryGetComponent(out Plate plate))
            return false;

        if (storedItems.Count >= maxIngredients)
            return false;

        if (storedItems.Contains(item))
            return false;

        return true;
    }

    public virtual bool AddItem(GameObject item, bool tryCombine = true)
    {
        if (!CanAcceptItem(item))
            return false;

        // Add the item to the plate
        storedItems.Add(item);
        item.transform.SetParent(transform);

        item.SetActive(showContainedItems);

        // Position the item on the plate
        PositionItem(item, storedItems.Count - 1);

        UpdateVisual();

        UpdateItemDisplay();

        if (item.TryGetComponent(out IngredientItem ingredientItem)) ingredientItem.RemoveItemDisplay();

        OnAnyIngredientAddedToPortableStorage?.Invoke(this, new IngredientEventArgs(ingredientItem));

        return true;
    }

    public virtual bool AddItems(List<GameObject> items)
    {
        if (items.Count > MaxItemsAcceptable) return false;

        List<GameObject> newItems = new List<GameObject>();

        foreach (var item in items)
        {
            if (AddItem(item) == false)
            {
                foreach (var newItem in newItems) RemoveItem(newItem);
                return false;
            }

            newItems.Add(item);
        }

        return true;
    }

    public virtual GameObject RemoveItem(GameObject item)
    {
        if (item == null) return null;

        storedItems.Remove(item);

        item.SetActive(true);

        UpdateVisual();

        UpdateItemDisplay();

        if (item.TryGetComponent(out IngredientItem ingredientItem)) ingredientItem.AddItemDisplay();

        if (storedItems.Count == 0) itemDisplay?.RemoveItemDisplay();

        return item;
    }

    public virtual void ClearAllItems()
    {
        storedItems.Clear();
        itemDisplay?.RemoveItemDisplay();
    }

    public virtual void DestroyAllItems()
    {
        if (storedItems.Count == 0) return;

        foreach (GameObject item in storedItems)
        {
            Destroy(item);
        }

        storedItems.Clear();
        itemDisplay?.RemoveItemDisplay();

        UpdateVisual();
    }

    public GameObject GetLastItem()
    {
        if (storedItems.Count == 0)
            return null;

        return storedItems[storedItems.Count - 1];
    }

    protected virtual void UpdateVisual() { }

    private void UpdateItemDisplay()
    {
        if (itemDisplay != null)
        {
            itemDisplay.AddItemDisplay();
            itemDisplay.UpdateItemDisplay(storedItems);
        }
    }

    private void PositionItem(GameObject item, int index)
    {
        // If we have pre-defined positions, use those
        if (itemPositions != null && index < itemPositions.Count)
        {
            item.transform.localPosition = itemPositions[index].localPosition;
            return;
        }

        // Otherwise, arrange in a circle
        float radius = 0.15f;
        float angleStep = 360f / Mathf.Max(1, maxIngredients);
        float angle = index * angleStep;

        Vector3 position = new Vector3(
            Mathf.Cos(angle * Mathf.Deg2Rad) * radius,
            defaultItemHeight,
            Mathf.Sin(angle * Mathf.Deg2Rad) * radius
        );

        item.transform.localPosition = position;
    }

    public abstract int GetStorageID();

    public GameObject GetInteractable()
    {
        return gameObject;
    }
}
