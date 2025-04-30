using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Room
{
    public Vector2Int position;  // Bottom-left corner (x, y)
    public Vector2Int size;      // Width and height (x, y)
    public bool isSplit = false;
    public Vector2 center;       // Center point for connection calculations

    public Room(Vector2Int position, Vector2Int size)
    {
        this.position = position;
        this.size = size;
        this.center = new Vector2(position.x + size.x / 2f, position.y + size.y / 2f);
    }

    public bool IsSplittable(int minSize, int gapSize)
    {
        return (size.x >= (minSize * 2 + gapSize)) || (size.y >= (minSize * 2 + gapSize));
    }
}