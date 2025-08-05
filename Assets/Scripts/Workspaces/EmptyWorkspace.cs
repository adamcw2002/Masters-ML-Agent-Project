using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EmptyWorkspace : Workspace
{
    public override IngredientState? GetOutputState()
    {
        return null;
    }
}
