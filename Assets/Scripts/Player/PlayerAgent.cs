using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using UnityEngine;
using System;
using System.ComponentModel.Design;
using System.Collections;

public class PlayerAgent : Agent
{
    public static event EventHandler OnEpisodeEnd;
    public static event EventHandler OnEpisodeStart;
    public static event EventHandler OnAgentSpawned;
    public static event Action OnAgentStep;

    [SerializeField] private PlayerMovement movement;
    [SerializeField] private PlayerInteract interact;
    [SerializeField] private bool endOnRecipeMade = true;

    private bool interactKeyPressed = false;

    private void Start()
    {
        GameTimer.OnTimeEnd += GameTimer_OnTimeEnd;
        DeliveryStation.OnRecipeDelivered += DeliveryStation_OnRecipeDelivered;
        Plate.OnAnyCombineIngredients += Plate_OnAnyCombineIngredients;

        OnAgentSpawned.Invoke(this, EventArgs.Empty);
    }

    private void OnDestroy()
    {
        GameTimer.OnTimeEnd -= GameTimer_OnTimeEnd;
        DeliveryStation.OnRecipeDelivered -= DeliveryStation_OnRecipeDelivered;
        Plate.OnAnyCombineIngredients -= Plate_OnAnyCombineIngredients;
    }

    public override void OnEpisodeBegin()
    {
        interactKeyPressed = false;

        GameObject heldItem = interact?.RemoveItem();
        if (heldItem != null) Destroy(heldItem);

        OnEpisodeStart?.Invoke(this, EventArgs.Empty);
    }
    private void DeliveryStation_OnRecipeDelivered(object sender, DeliveryEventArgs e)
    {
        if (e.isCorrectRecipe) StartCoroutine(EndEpisodeAfterFrame());
    }

    private IEnumerator EndEpisodeAfterFrame()
    {
        yield return new WaitForEndOfFrame();

        EndCurrentEpisode();
    }

    private void Plate_OnAnyCombineIngredients(object sender, IngredientEventArgs e)
    {
        if (endOnRecipeMade)
        {
            //CHECK IF PLATE HAS COMBINED ALL INGREDIENTS TO FORM THE CORRECT RECIPE
            IngredientData ingredientData = e.IngredientItem.IngredientData;
            RecipeData activeRecipeData = RecipeManager.Instance.GetActiveRecipe();

            if (ingredientData == activeRecipeData.finalProductData && e.IngredientItem.CurrentState == activeRecipeData.finalProductState)
            {
                RewardManager.Instance.AddAgentReward(GameTimer.Instance.GetNormalisedTimeRemaining(), "Time Bonus");

                EndCurrentEpisode();
            }
        }
    }

    private void GameTimer_OnTimeEnd() => EndCurrentEpisode();

    private void EndCurrentEpisode()
    {
        Debug.Log("Episode ended");

        OnEpisodeEnd?.Invoke(this, EventArgs.Empty);

        EndEpisode();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            interactKeyPressed = true;
        }

        if (Input.GetKeyDown(KeyCode.T))
        {
            var workspaces = AgentObservationManager.Instance.GetTileObservations(transform.position, 1, true);
        }

        if (Input.GetKeyDown(KeyCode.L))
        {
            AgentObservationManager.Instance.GetAllIngredientObservations(transform.position, true);
        }
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        //Current Position - 3 Observations
        sensor.AddObservation(transform.position.x);
        sensor.AddObservation(transform.position.z);

        //Current Inventory - 8 Observations
        sensor.AddObservation(interact.GetAgentInventoryObservation());

        //Current Tile to interact with - 15 Observations
        GameObject currentInteractable = interact.GetCurrentInteractableGameObject();
        sensor.AddObservation(AgentObservationManager.Instance.GetOneHotTileObservation(currentInteractable, false));

        //Current Recipe - 46 Observations
        sensor.AddObservation(AgentObservationManager.Instance.GetCurrentRecipeObservation());

        //Tile Observations - (Range + Range + 1)^2 * 17 Observations
        int tileRange = 6;
        sensor.AddObservation(AgentObservationManager.Instance.GetTileObservations(transform.position, tileRange));

        //Plate Observations - 24 * 3 = 72 Observations
        sensor.AddObservation(AgentObservationManager.Instance.GetAllPlateObservations(transform.position));

        //Loose Items Observations - 9 * Max Loose Ingredients
        sensor.AddObservation(AgentObservationManager.Instance.GetAllIngredientObservations(transform.position));

        //Delivery Station - 3
        sensor.AddObservation(AgentObservationManager.Instance.GetDeliveryStationObservation(transform.position));

        //Game Timer
        sensor.AddObservation(GameTimer.Instance.GetNormalisedTimeRemaining());
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        movement.SetMoveInput(actions.ContinuousActions[0], actions.ContinuousActions[1]);

        int interactPressed = actions.DiscreteActions[0];
        if (interactPressed == 1 && interact != null && interact.enabled)
        {
            interact.Interact();
        }

        OnAgentStep?.Invoke();
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var cont = actionsOut.ContinuousActions;
        var dis = actionsOut.DiscreteActions;

        cont[0] = Input.GetAxisRaw("Horizontal");
        cont[1] = Input.GetAxisRaw("Vertical");

        dis[0] = interactKeyPressed ? 1 : 0;

        interactKeyPressed = false;
    }
}