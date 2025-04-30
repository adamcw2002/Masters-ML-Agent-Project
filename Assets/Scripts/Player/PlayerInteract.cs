using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInteract : MonoBehaviour
{
    [SerializeField] private float interactRange = 1f;
    [SerializeField] private LayerMask interactableMask;
    [SerializeField] private Transform itemHoldTransform;

    private PlayerMovement movementScript;

    private IInteractable currentInteractable;
    private GameObject currentItemHolding = null;

    public bool IsHoldingItem => currentItemHolding != null;

    void Start()
    {
        movementScript = GetComponent<PlayerMovement>();
    }

    void FixedUpdate()
    {
        DetectNearbyInteractables();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) && currentInteractable != null)
        {
            currentInteractable.Interact(this, currentItemHolding);
        }
    }

    public bool CanPickupItem()
    {
        //If holding item, check if it is a plate
        if (IsHoldingItem)
        {
            if (currentItemHolding.TryGetComponent(out PortableStorage storage) == false) return false;

            return !storage.IsFull;
        }

        return true;
    }

    public bool PickupItem(GameObject item)
    {
        // Player is holding item
        if (currentItemHolding != null)
        {
            // Player has a portable storage
            if (currentItemHolding.TryGetComponent(out PortableStorage storage))
            {
                return storage.AddItem(item);
            }

            return false;
        }

        // Ingredient requires plate and player not holding plate
        if (item.TryGetComponent(out IngredientItem ingredient))
        {
            if (ingredient.RequiresPlate) return false;
        }

        currentItemHolding = item;

        item.transform.position = itemHoldTransform.position;
        item.transform.parent = itemHoldTransform;

        return true;
    }

    public GameObject RemoveItem()
    {
        GameObject tempItem = currentItemHolding;
        currentItemHolding = null;
        return tempItem;
    }

    void DetectNearbyInteractables()
    {
        DetectLastFacingNearbyInteractables();
        if (currentInteractable != null) return;

        Vector3 origin = transform.position;
        Vector3[] directions = new Vector3[]
        {
            transform.forward,
            -transform.forward,
            -transform.right,
            transform.right
        };

        foreach (Vector3 dir in directions)
        {
            if (Physics.Raycast(origin, dir, out RaycastHit hit, interactRange, interactableMask))
            {
                currentInteractable = hit.collider.GetComponent<IInteractable>();
                if (currentInteractable != null)
                {
                    Debug.DrawRay(origin, dir * interactRange, Color.green);
                    return;
                }
            }

            Debug.DrawRay(origin, dir * interactRange, Color.red);
        }

        currentInteractable = null;
    }

    void DetectLastFacingNearbyInteractables()
    {
        Vector3 direction = movementScript.lastFacingDirection;
        Vector3 origin = transform.position; // height offset if needed

        if (Physics.Raycast(origin, direction, out RaycastHit hit, interactRange, interactableMask))
        {
            currentInteractable = hit.collider.GetComponent<IInteractable>();
            if (currentInteractable != null)
            {
                Debug.DrawRay(origin, direction * (interactRange + 0.5f), Color.green);
                return;
            }
        }
        else
        {
            currentInteractable = null;
            Debug.DrawRay(origin, direction * (interactRange + 0.5f), Color.red);
        }
    }
}
