using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlateInitializer : MonoSingleton<PlateInitializer>
{
    [SerializeField] private int plateAmount;
    [SerializeField] private GameObject platePrefab;

    private List<Plate> plates = new List<Plate>();
    List<GameObject> emptyWorkspaces = new List<GameObject>();

    public List<Plate> GetPlates() => plates;

    private void OnEnable()
    {
        WorkspaceGenerator.OnWorkspacesGenerated += WorkspaceGenerator_OnWorkspacesGenerated;

        DeliveryStation.OnRecipeDelivered += DeliveryStation_OnRecipeDelivered;

        Bin.OnDishBinned += Bin_OnDishBinned;
    }

    private void Bin_OnDishBinned(object sender, System.EventArgs e)
    {
        SpawnNewPlate();
    }

    private void DeliveryStation_OnRecipeDelivered(object sender, System.EventArgs e)
    {
        SpawnNewPlate();
    }

    private void WorkspaceGenerator_OnWorkspacesGenerated(object sender, System.EventArgs e)
    {
        SpawnPlates();
    }

    private void AssignEmptyWorkspaces()
    {
        emptyWorkspaces.Clear();

        Dictionary<Vector2Int, GameObject> workspaces = WorkspaceGenerator.Instance.GetAllWorkspaces();

        emptyWorkspaces = workspaces.Values
            .Where(ws => ws != null && ws.TryGetComponent<EmptyWorkspace>(out EmptyWorkspace workspace) && workspace.HasItems == false)
            .ToList();

        // Shuffle the list
        System.Random rng = new System.Random();
        emptyWorkspaces = emptyWorkspaces.OrderBy(_ => rng.Next()).ToList();
    }

    private void SpawnPlates()
    {
        AssignEmptyWorkspaces();

        int spawnCount = Mathf.Min(plateAmount, emptyWorkspaces.Count);

        for (int i = 0; i < spawnCount; i++)
        {
            SpawnNewPlate();
        }
    }

    private void SpawnNewPlate()
    {
        AssignEmptyWorkspaces();

        Debug.Log("Spawn plate");

        GameObject workspace = emptyWorkspaces[0];
        if (workspace.TryGetComponent(out Workspace ws))
        {
            GameObject plate = Instantiate(platePrefab);
            ws.AddItem(plate);

            if (plate.TryGetComponent(out Plate plateComponent))
            {
                plates.Add(plateComponent);
            }
        }
    }
}
