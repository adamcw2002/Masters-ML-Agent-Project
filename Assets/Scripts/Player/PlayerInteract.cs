using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInteract : MonoBehaviour
{
    [SerializeField] private float interactRange = 1f;
    [SerializeField] private LayerMask interactableMask;
    [SerializeField] private Transform itemHoldTransform;

    private PlayerMovement movementScript;
    private PlayerAgent agent;

    private IInteractable currentInteractable;
    private GameObject currentItemHolding = null;

    public bool IsHoldingItem => currentItemHolding != null;

    void Start()
    {
        movementScript = GetComponent<PlayerMovement>();
        agent = GetComponent<PlayerAgent>();
    }

    void FixedUpdate()
    {
        DetectNearbyInteractables();
    }

    public IInteractable GetCurrentInteractable() => currentInteractable;
    public GameObject GetCurrentInteractableGameObject() => currentInteractable?.GetInteractable();

    private void Update()
    {
        if (!agent && Input.GetKeyDown(KeyCode.Space) && currentInteractable != null)
        {
            Interact();
        }
    }

    public void Interact()
    {
        currentInteractable?.Interact(this, currentItemHolding);
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
                SetNewInteractable(hit.collider?.gameObject);

                if (currentInteractable != null)
                {
                    Debug.DrawRay(origin, dir * interactRange, Color.green);
                    return;
                }
            }

            Debug.DrawRay(origin, dir * interactRange, Color.red);
        }

        SetNewInteractable(null);
    }

    void DetectLastFacingNearbyInteractables()
    {
        Vector3 direction = movementScript.lastFacingDirection;
        Vector3 origin = transform.position; // height offset if needed

        if (Physics.Raycast(origin, direction, out RaycastHit hit, interactRange, interactableMask))
        {
            SetNewInteractable(hit.collider?.gameObject);

            if (currentInteractable != null)
            {
                Debug.DrawRay(origin, direction * (interactRange + 0.5f), Color.green);
                return;
            }
        }
        else
        {
            SetNewInteractable(null);
            Debug.DrawRay(origin, direction * (interactRange + 0.5f), Color.red);
        }
    }

    private void SetNewInteractable(GameObject obj)
    {
        IInteractable newInteractable = obj?.GetComponent<IInteractable>();

        if (newInteractable == null)
        {
            MaterialHighlighter.Instance.ClearHighlight();
        }
        else if (newInteractable != currentInteractable) MaterialHighlighter.Instance.HighlightObject(obj);

        currentInteractable = newInteractable;
    }

    public float[] GetAgentInventoryObservation()
    {
        int numStates = System.Enum.GetNames(typeof(IngredientState)).Length;
        float[] observation = new float[2 + 1 + numStates]; // 2 bools + ID + one-hot state

        observation[0] = currentItemHolding != null ? 1f : 0f;
        observation[1] = (currentItemHolding != null && currentItemHolding.TryGetComponent<PortableStorage>(out _)) ? 1f : 0f;

        if (currentItemHolding != null && currentItemHolding.TryGetComponent<IngredientItem>(out var ingredient))
        {
            observation[2] = ingredient.IngredientData.uniqueIntID;

            var oneHotState = AgentObservationManager.Instance.GetOneHotIngredientState(ingredient.CurrentState);

            for (int i = 0; i < oneHotState.Length; i++)
                observation[3 + i] = oneHotState[i];
        }
        else
        {
            observation[2] = -1f;
            for (int i = 0; i < numStates; i++)
                observation[3 + i] = 0f;
        }

        return observation;
    }
}
