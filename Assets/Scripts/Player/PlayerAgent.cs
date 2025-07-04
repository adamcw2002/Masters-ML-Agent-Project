using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using UnityEngine;
using System;

public class PlayerAgent : Agent
{
    public static event EventHandler OnEpisodeEnd;
    public static event EventHandler OnAgentSpawned;

    private PlayerMovement movement;
    private PlayerInteract interact;

    private bool interactKeyPressed = false;

    private void Awake()
    {
        movement = GetComponent<PlayerMovement>();
        interact = GetComponent<PlayerInteract>();
    }

    private void Start()
    {
        GameTimer.OnTimeEnd += GameTimer_OnTimeEnd;

        OnAgentSpawned.Invoke(this, EventArgs.Empty);
    }

    private void GameTimer_OnTimeEnd()
    {
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
        sensor.AddObservation(transform.position);

        //Current Inventory - 8 Observations
        sensor.AddObservation(interact.GetAgentInventoryObservation());

        //Current Recipe - 46 Observations
        sensor.AddObservation(AgentObservationManager.Instance.GetCurrentRecipeObservation());

        //Tile Observations - (Range + Range + 1)^2 * 10 Observations
        //Range = 1 -> 90 Observations
        //Range = 2 -> 250 Observations
        //Range = 3 -> 490 Observations
        //Range = 4 -> 810 Observations
        //Range = 5 -> 1210 Observations
        int tileRange = 3;
        sensor.AddObservation(AgentObservationManager.Instance.GetTileObservations(transform.position, tileRange));

        //Plate Observations - 24 * 3 = 72 Observations
        sensor.AddObservation(AgentObservationManager.Instance.GetAllPlateObservations(transform.position));

        //Loose Items Observations - 9 * Max Loose Ingredients
        sensor.AddObservation(AgentObservationManager.Instance.GetAllIngredientObservations(transform.position));
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        movement.SetMoveX(actions.ContinuousActions[0]);
        movement.SetMoveZ(actions.ContinuousActions[1]);

        int interactPressed = actions.DiscreteActions[0];
        if (interactPressed == 1 && interact != null && interact.enabled)
        {
            interact.Interact();
        }
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