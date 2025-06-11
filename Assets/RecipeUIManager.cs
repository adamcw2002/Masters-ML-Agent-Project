using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RecipeUIManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI recipeTitleText;
    [SerializeField] private Image recipeIcon;
    [SerializeField] private Transform ingredientIconContainer;
    [SerializeField] private Transform cookingInstructionContainer;

    [SerializeField] private Transform iconImageTransform;

    [SerializeField] private Sprite friedSpriteIcon;
    [SerializeField] private Sprite cookedSpriteIcon;
    [SerializeField] private Sprite boiledSpriteIcon;

    private void Start()
    {
        RecipeManager.OnNewRecipeSelected += RecipeManager_OnNewRecipeSelected;
    }

    private void RecipeManager_OnNewRecipeSelected(object sender, RecipeData recipe)
    {
        SetNewRecipe(recipe);
    }

    public void SetNewRecipe(RecipeData recipe)
    {
        foreach (Transform child in ingredientIconContainer) Destroy(child.gameObject);
        foreach (Transform child in cookingInstructionContainer) Destroy(child.gameObject);

        recipeTitleText.text = recipe.recipeName;
        recipeIcon.sprite = recipe.finalProductData.icon;

        foreach (var ingredient in recipe.baseRequiredIngredients)
        {
            CreateNewIcon(ingredient);
        }
    }

    private void CreateNewIcon(RequiredRecipeIngredient ingredient)
    {
        Transform newIcon = Instantiate(iconImageTransform);
        newIcon.transform.SetParent(ingredientIconContainer, false);

        Image image = newIcon.GetChild(0).GetComponent<Image>();

        image.sprite = ingredient.ingredient.icon;

        AddCookingInstruction(ingredient.requiredState);
    }

    private void AddCookingInstruction(IngredientState requiredState)
    {
        Transform newIcon = Instantiate(iconImageTransform);
        newIcon.transform.SetParent(cookingInstructionContainer, false);

        Sprite iconSprite = null;
        switch (requiredState)
        {
            case IngredientState.Fried:
                iconSprite = friedSpriteIcon;
                break;
            case IngredientState.Boiled:
                iconSprite = boiledSpriteIcon;
                break;
            case IngredientState.Cooked:
                iconSprite = cookedSpriteIcon;
                break;
        }

        Image image = newIcon.GetChild(0).GetComponent<Image>();
        image.sprite = iconSprite;

        if (iconSprite == null)
        {
            Color hiddenColor = new Color(0, 0, 0, 0);
            image.color = hiddenColor;
            newIcon.GetComponent<Image>().color = hiddenColor;
        }

    }
}
