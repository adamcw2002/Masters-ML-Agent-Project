using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Appliance
{
    public string applianceName;
    public GameObject appliancePrefab;
    public bool allowsIngredientStorage;
    public int maxStorageCapacity = 1;
}