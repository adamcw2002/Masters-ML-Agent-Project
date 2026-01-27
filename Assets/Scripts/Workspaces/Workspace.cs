using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public abstract class Workspace : MonoBehaviour, IInteractable
{
    public static event EventHandler<IngredientEventArgs> OnAnyItemAddedToWorkspace;
    public static event EventHandler<IngredientEventArgs> OnAnyItemRemovedFromWorkspace;
    public static event EventHandler OnAnyWorkspaceProcessing;

    [SerializeField] protected int maxItems = 1;

    [SerializeField] protected bool canProcessItems = true;
    [SerializeField] protected float processingTime = 0f;
    private float processAmount;
    public bool CanProcessItems() => canProcessItems; 
    public float GetProccessAmount() => processAmount;

    [SerializeField] protected IngredientState outputState;

    [SerializeField] protected bool canHoldPortableStorage = false;
    [SerializeField] protected bool mustRemoveWithPlate = false;
    [SerializeField] protected bool itemsVisibleOnStorage = true;

    protected List<GameObject> storedItems = new List<GameObject>();
    protected bool isProcessing = false;
    private ProgressBarUI progressBarUI = null;

    private ItemDisplayComponent itemDisplay = null;

    public bool HasItems => storedItems.Count > 0;
    public GameObject GetFirstItem() => HasItems ? storedItems[0] : null;
    public virtual IngredientState? GetOutputState() => outputState;

    protected virtual void Start()
    {
        itemDisplay = GetComponent<ItemDisplayComponent>();

        foreach (GameObject item in storedItems)
        {
            if (item == null) Destroy(item);
        }
    }

    public virtual void Interact(PlayerInteract player, GameObject itemHolding)
    {
        if (isProcessing) return;

        //If portable storage is currently on the workspace
        if (canHoldPortableStorage && maxItems == 1 && storedItems.Count > 0 && storedItems[0] != null && storedItems[0]?.TryGetComponent(out PortableStorage storage) == true)
        {
            //Debug.Log("A Portable storage is currently on workspace, interact with the storage");
            storage.Interact(player, itemHolding);

            // Check if storage has changed parent (been picked up), remove from workspace if so
            if (storage.transform.parent != transform) RemoveItem(storage.gameObject);

            return;
        }

        //
        // PLAYER IS HOLDING SOMETHING
        //
        if (itemHolding != null)
        {
            //
            // PLAYER IS HOLDING STORAGE
            //
            if (itemHolding.TryGetComponent(out PortableStorage storageHolding))
            {
                //
                // TRY ADD STORAGE TO WORKSPACE
                //
                if (AddItem(itemHolding))
                {
                    player.RemoveItem();
                    return;
                }

                //
                // TRY ADD STORED ITEMS FROM WORKSPACE TO STORAGE
                //
                if (storedItems.Count > 0)
                {
                    if (storageHolding.AddItems(storedItems))
                    {
                        RemoveAllItems();
                        return;
                    }
                }

                //
                // TRY ADD ITEM FROM STORAGE TO WORKSPACE
                //
                if (storageHolding.StoredItems.Count > 0)
                {
                    if (AddItems(storageHolding))
                    {
                        storageHolding.ClearAllItems();
                        return;
                    }
                }
            }

            //
            // PLAYER IS HOLDING INGREDIENT, TRY ADD ITEM TO WORKSPACE
            //
            if (AddItem(itemHolding))
            {
                player.RemoveItem();
                return;
            }
        }

        //
        // PLAYER IS NOT HOLDING INGREDIENT, TRY TAKE ITEM FROM WORKSPACE
        //
        if (storedItems.Count > 0 && mustRemoveWithPlate == false)
        {
            GameObject item = GetLastItem();

            if (player.PickupItem(item)) RemoveItem(item);
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

    protected bool CanProcessItem(GameObject item)
    {
        IngredientItem ingredientItem = item.GetComponent<IngredientItem>();

        //If its not an ingredient, then cannot process
        if (ingredientItem == null) return false;

        //If item is already in the output state, then cannot process
        if (ingredientItem.CurrentState == outputState) return false;

        //Check if the ingredient can be the output state, and if its current state is valid to change it to the output
        return ingredientItem.IngredientData.CheckValidStates(ingredientItem.CurrentState, outputState);
    }

    protected virtual void UpdateVisual() 
    { 
        if (itemsVisibleOnStorage == false)
        {
            foreach (var item in storedItems) if (item.activeInHierarchy) item.SetActive(false);
        }
    }

    private bool CheckValidItem(GameObject item)
    {
        if (item.TryGetComponent(out PortableStorage storage) && storedItems.Count > 0)
            return false;

        if (canHoldPortableStorage && storedItems.Count > 0 && storedItems[0].TryGetComponent(out PortableStorage portableStorage))
            return portableStorage.AddItem(item);

        // Check if it can accept the item
        if (!CanAcceptItem(item))
            return false;

        // Check whether it can process the item
        if (canProcessItems && storage == false && !CanProcessItem(item))
            return false;

        return true;
    }

    public virtual bool AddItem(GameObject item, bool forceItem = false, bool startProcessing = true)
    {
        if (forceItem == false && CheckValidItem(item) == false)
        {
            return false;
        }

        storedItems.Add(item);
        item.transform.SetParent(transform);
        item.transform.localPosition = Vector3.up * 0.5f;

        UpdateVisual();

        UpdateItemDisplay();

        if (item.TryGetComponent(out IngredientItem ingredientItem))
        {
            ingredientItem.RemoveItemDisplay();
        }

        //if (startProcessing && canProcessItems && CanProcessItem(item)) StartProcessing();

        OnAnyItemAddedToWorkspace?.Invoke(this, new IngredientEventArgs(ingredientItem));

        if (startProcessing && canProcessItems && CanProcessItem(item)) StartProcessing();

        return true;
    }

    public virtual bool AddItems(PortableStorage storage)
    {
        if (storage.StoredItems.Count > maxItems - storedItems.Count) return false;

        foreach (var item in storage.StoredItems)
        {
            if (CheckValidItem(item) == false)
            {
                return false;
            }
        }

        foreach (var item in storage.StoredItems)
        {
            if (AddItem(item) == false)
            {
                Debug.LogWarning("One of the items were not accepted after passing previous checks");
            }
        }

        return true;
    }

    public virtual GameObject RemoveItem(GameObject item)
    {
        if (item == null) 
            return null;

        storedItems.Remove(item);

        UpdateVisual();

        UpdateItemDisplay();

        if (item.TryGetComponent(out IngredientItem ingredientItem))
        {
            ingredientItem.AddItemDisplay();
        }

        if (storedItems.Count == 0) itemDisplay?.RemoveItemDisplay();

        OnAnyItemRemovedFromWorkspace?.Invoke(this, new IngredientEventArgs(ingredientItem));

        return item;
    }

    public virtual void RemoveAllItems()
    {
        storedItems.Clear();
        itemDisplay?.RemoveItemDisplay();
        UpdateVisual();
    }

    public virtual void DestroyAllItems()
    {
        for (int i = 0; i < storedItems.Count; i++)
        {
            GameObject item = RemoveItem(GetLastItem());
            Destroy(item);
        }

        storedItems.Clear();
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

    protected virtual IEnumerator ProcessItems()
    {
        isProcessing = true;

        if (processingTime > 0) ShowProgressBar();

        UpdateVisual();

        float elapsed = 0f;
        while (elapsed < processingTime)
        {
            elapsed += Time.deltaTime;
            processAmount = Mathf.Clamp01(elapsed / processingTime);
            OnAnyWorkspaceProcessing?.Invoke(this, EventArgs.Empty);

            yield return null;
        }

        isProcessing = false;

        CompleteProcessing();
    }

    private void ShowProgressBar()
    {
        if (progressBarUI == null) progressBarUI = ProgressBarManager.Instance.CreateProgressBar(transform);
        progressBarUI.StartProcessing(processingTime);
    }

    protected virtual void CompleteProcessing()
    {
        if (storedItems.Count == 0)
            return;

        foreach (GameObject ingredientObj in storedItems)
        {
            if (ingredientObj?.TryGetComponent(out IngredientItem ingredient) == true)
            {
                ingredient.ChangeState(outputState);
            }
        }

        TryCombineIngredients();

        UpdateVisual();
    }

    private void TryCombineIngredients()
    {
        RecipeData recipe = RecipeManager.Instance.GetMatchingRecipe(storedItems);
        if (recipe != null)
        {
            // HAS MADE RECIPE IN APPLIANCE
            DestroyAllItems();

            IngredientData productData = recipe.finalProductData;

            GameObject recipePrefab = productData.GetPrefabFromState(recipe.finalProductState);
            GameObject recipeObject = Instantiate(recipePrefab);

            if (recipeObject.TryGetComponent(out IngredientItem ingredient) == false) ingredient = recipeObject.AddComponent<IngredientItem>();
            ingredient.SetIngredientData(productData, recipe.finalProductState);

            AddItem(recipeObject, true);
        }
    }

    private void UpdateItemDisplay()
    {
        if (itemDisplay != null)
        {
            itemDisplay.AddItemDisplay(1f);
            itemDisplay.UpdateItemDisplay(storedItems);
        }
    }

    public GameObject GetInteractable()
    {
        return gameObject;
    }
}