using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IngredientEventArgs : EventArgs
{
    public IngredientItem IngredientItem;

    public IngredientEventArgs(IngredientItem ingredientItem)
    {
        IngredientItem = ingredientItem;
    }
}

public class BinEventArgs : EventArgs
{
    public Plate plate;

    public BinEventArgs(Plate plate)
    {
        this.plate = plate;
    }
}

public class DeliveryEventArgs : EventArgs
{
    public Plate plate;
    public bool isCorrectRecipe;

    public DeliveryEventArgs(Plate plate, bool isCorrectRecipe)
    {
        this.plate = plate;
        this.isCorrectRecipe = isCorrectRecipe;
    }
}