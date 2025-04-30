using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EmptyWorkspace : Workspace
{
    public override bool CanProcessItem(GameObject item) => false;
}
