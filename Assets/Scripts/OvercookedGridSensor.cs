using Unity.MLAgents.Sensors;
using UnityEngine;
using UnityEngine.Tilemaps;

public class OvercookedGridSensor : GridSensorBase
{
    public OvercookedGridSensor(
        string name,
        Vector3 cellScale,
        Vector3Int gridSize,
        string[] detectableTags,
        SensorCompressionType compression
    ) : base(name, cellScale, gridSize, detectableTags, compression) { }

    protected override int GetCellObservationSize()
    {
        /*
       tileType (e.g. 0 = floor, 1 = workspace, 2 = stove...), 

       isOccupiedByAgent (bool) 

       HasItem (bool) 

       can workspace process (bool)

       cookProgress (0-1), 

       outputState (one hot)

       itemType (normalised ID), 

       itemState [1,0,0,0,0] raw etc
       */

        return 16;
    }

    protected override bool IsDataNormalized()
    {
        return true;
    }

    protected override void GetObjectData(GameObject detectedObject, int tagIndex, float[] dataBuffer)
    {
        Vector2Int playerPos = AgentObservationManager.Instance.GetPlayerVector2IntPos();

        dataBuffer = AgentObservationManager.Instance.GetOneHotTileObservation(detectedObject, detectedObject.transform.position.Equals(playerPos));

        /*
        var tile = detectedObject.GetComponent<TileData>();
        if (tile == null)
        {
            Debug.LogWarning($"Missing TileData component on {detectedObject.name}");
            return;
        }

        dataBuffer[0] = NormalizeTileType(tile.TileType);
        dataBuffer[1] = tile.HasItem ? 1f : 0f;
        dataBuffer[2] = NormalizeItemType(tile.ItemType);
        dataBuffer[3] = NormalizeItemState(tile.ItemState);
        dataBuffer[4] = tile.CookProgress >= 0f ? Mathf.Clamp01(tile.CookProgress / 100f) : 0f;

        var output = tile.WorkspaceOutputOneHot(); // should be length 3
        dataBuffer[5] = output.Length > 0 ? output[0] : 0f;
        dataBuffer[6] = output.Length > 1 ? output[1] : 0f;
        dataBuffer[7] = output.Length > 2 ? output[2] : 0f;

        dataBuffer[8] = tile.IsOccupiedByAgent ? 1f : 0f;
        */
    }

    // Optional normalization helpers if needed
    private float NormalizeTileType(int type) => type / 10f;  // assuming max 10 types
    private float NormalizeItemType(int type) => type / 20f;  // assuming max 20 item types
    private float NormalizeItemState(int state) => state / 5f; // assuming 0–5 states
}
