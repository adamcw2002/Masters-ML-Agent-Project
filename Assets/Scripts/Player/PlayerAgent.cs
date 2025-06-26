using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using UnityEngine;
using System;

public class PlayerAgent : Agent
{
    public static event EventHandler OnEpisodeEnd;

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
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        //Current Position - 3 Observations
        sensor.AddObservation(transform.position);

        //Current Inventory - 8 Observations
        sensor.AddObservation(interact.GetAgentInventoryObservation());

        //Current Recipe - 30 Observations
        sensor.AddObservation(AgentObservationManager.Instance.GetCurrentRecipeObservation());
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