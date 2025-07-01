using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RewardManager : MonoSingleton<RewardManager>
{
    private PlayerAgent agent;

    private void Start()
    {
        PlayerAgent.OnAgentSpawned += PlayerAgent_OnAgentSpawned;

        IngredientSpawner.OnPlayerGrabIngredient += IngredientSpawner_OnPlayerGrabIngredient;
    }

    private void IngredientSpawner_OnPlayerGrabIngredient(object sender, System.EventArgs e)
    {
        
    }

    private void OnDisable()
    {
        PlayerAgent.OnAgentSpawned -= PlayerAgent_OnAgentSpawned;
    }

    private void PlayerAgent_OnAgentSpawned(object sender, System.EventArgs e)
    {
        agent = sender as PlayerAgent;
    }

    
}
