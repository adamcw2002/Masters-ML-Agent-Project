using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AgentObservationManager : MonoSingleton<AgentObservationManager>
{
    public float[] GetCurrentRecipeObservation()
    {
        RecipeData activeRecipe = RecipeManager.Instance.GetActiveRecipe();
        int maxIngredients = 4;
        int stateCount = System.Enum.GetValues(typeof(IngredientState)).Length;

        // Total: 1 ID + n state one-hot = 1 + 5 = 6 per entry
        float[] observation = new float[1 + stateCount + maxIngredients * (1 + stateCount)];
        int index = 0;

        // --- Final Product ---
        observation[index++] = activeRecipe.finalProductData.uniqueIntID;

        float[] finalStateOneHot = GetOneHotIngredientState(activeRecipe.finalProductState);
        foreach (float val in finalStateOneHot)
            observation[index++] = val;

        // --- Ingredients (baseRequiredIngredients only) ---
        for (int i = 0; i < maxIngredients; i++)
        {
            if (i < activeRecipe.baseRequiredIngredients.Count)
            {
                var req = activeRecipe.baseRequiredIngredients[i];
                observation[index++] = req.ingredient.uniqueIntID;

                float[] oneHot = GetOneHotIngredientState(req.requiredState);
                foreach (float val in oneHot)
                    observation[index++] = val;
            }
            else
            {
                // Pad
                observation[index++] = -1f;
                for (int s = 0; s < stateCount; s++)
                    observation[index++] = 0f;
            }
        }

        return observation;
    }

    public float[] GetOneHotIngredientState(IngredientState state)
    {
        int numStates = System.Enum.GetValues(typeof(IngredientState)).Length;
        float[] oneHot = new float[numStates];

        oneHot[(int)state] = 1f;
        return oneHot;
    }
}
