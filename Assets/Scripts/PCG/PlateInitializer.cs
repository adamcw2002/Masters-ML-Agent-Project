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

        Bin.OnPlateBinned += Bin_OnPlateBinned;
    }

    private void Bin_OnPlateBinned(object sender, BinEventArgs e)
    {
        ResetPlate(e.plate);
    }

    private void DeliveryStation_OnRecipeDelivered(object sender, DeliveryEventArgs e)
    {
        ResetPlate(e.plate);
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

        Shuffle(emptyWorkspaces);
    }

    private void SpawnPlates()
    {
        for (int i = 0; i < plates.Count; i++)
        {
            Destroy(plates[i].gameObject);
        }
        plates.Clear();

        AssignEmptyWorkspaces();

        int spawnCount = Mathf.Min(plateAmount, emptyWorkspaces.Count);

        for (int i = 0; i < spawnCount; i++)
        {
            SpawnNewPlate();
        }
    }

    private void SpawnNewPlate()
    {
        GameObject workspace = GetEmptyWorkspace();
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

    public void ResetPlate(Plate plate)
    {
        GameObject emptyWorkspace = GetEmptyWorkspace();

        if (emptyWorkspace.TryGetComponent(out Workspace workspace))
        {
            workspace.AddItem(plate.gameObject);
        }
    }

    private GameObject GetEmptyWorkspace()
    {
        AssignEmptyWorkspaces();
        return emptyWorkspaces[0];
    }

    private void Shuffle<T>(IList<T> list)
    {
        for (int i = 0; i < list.Count - 1; i++)
        {
            int j = UnityEngine.Random.Range(i, list.Count);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
