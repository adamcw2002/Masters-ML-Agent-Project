using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Class to represent a connection between two rooms
public class RoomConnection
{
    public Room roomA;
    public Room roomB;
    public float distance;

    public RoomConnection(Room roomA, Room roomB)
    {
        this.roomA = roomA;
        this.roomB = roomB;
        this.distance = Vector2.Distance(roomA.center, roomB.center);
    }
}