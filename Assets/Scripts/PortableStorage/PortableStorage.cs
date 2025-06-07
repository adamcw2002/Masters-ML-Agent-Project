using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class PortableStorage : MonoBehaviour, IInteractable
{
    [SerializeField] private int maxIngredients = 4;
    [SerializeField] private List<Transform> itemPositions;  // Optional positions where items should be placed
    [SerializeField] private float defaultItemHeight = 0.1f; // Height above plate for items
    [SerializeField] private bool showContainedItems = true;

    private List<GameObject> storedItems = new List<GameObject>();

    public bool IsEmpty => storedItems.Count == 0;
    public bool IsFull => storedItems.Count >= maxIngredients;
    public List<GameObject> StoredItems => storedItems;

    protected ItemDisplay itemDisplay = null;

    private void Start()
    {
        itemDisplay = ItemDisplayManager.Instance.CreateItemDisplay(transform);
    }

    private void OnDestroy()
    {
        if (itemDisplay) itemDisplay.ReturnToPool();
    }

    public void Interact(PlayerInteract player, GameObject itemHolding)
    {
        // Player has empty hands, give them the storage with all its contents
        if (itemHolding == null)
        {
            player.PickupItem(gameObject);
        }
        else if (itemHolding == gameObject)
        {
            return;
        }
        else
        {
            // Player is holding something else, try to add it to the storage
            PortableStorage heldStorage = itemHolding.GetComponent<PortableStorage>();

            if (heldStorage != null)
            {
                // Player is holding another storage - take item
                GameObject item = GetLastItem();
                if (item != null && heldStorage.AddItem(item))
                    RemoveItem(item);
                return;
            }

            if (AddItem(itemHolding))
            {
                player.RemoveItem();
            }
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

    public virtual bool AddItem(GameObject item)
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

        if (item.TryGetComponent(out IngredientItem ingredient))
        {
            ingredient.DestroyItemDisplay();
            if (ingredient.IngredientData.isProduct == false) itemDisplay.AddNewIcon(ingredient.IngredientData);
        }

        return true;
    }

    public GameObject RemoveItem(GameObject item)
    {
        if (item == null) return null;

        storedItems.Remove(item);

        item.SetActive(true);

        UpdateVisual();

        if (item.TryGetComponent(out IngredientItem ingredient))
        {
            ingredient.CreateItemDisplay();
            itemDisplay.RemoveIcon(ingredient.IngredientData);
        }

        return item;
    }

    protected void RemoveAllItems()
    {
        if (storedItems.Count == 0) return;

        foreach (GameObject item in storedItems)
        {
            Destroy(item);
        }

        storedItems.Clear();

        UpdateVisual();
    }

    public GameObject GetLastItem()
    {
        if (storedItems.Count == 0)
            return null;

        return storedItems[storedItems.Count - 1];
    }

    protected virtual void UpdateVisual() { }

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
}
