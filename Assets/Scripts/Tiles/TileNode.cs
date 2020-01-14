using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TileNode : IHeapItem<TileNode>
{
    public Vector3Int localPosition;    // position on the tilemap
    public Vector3 worldPosition;       // position in Unity's world space
    public TileBase tileBase;           // what type of Tile is this
    public Tilemap tilemapMember;       // what tilemap is this a part of
    public int aGridX;                  // x position in the array grid
    public int aGridY;                  
    public string name;                 // name of the node
    public bool walkable;
    public int movementPenalty;                  // 

    // for pathfinding
    public int gCost;                   // distance from the start point
    public int hCost;                   // distance from the target point (must use a heuristic)
    public TileNode parent;             // the node we was used to get to this node - allows us to retrace the path
    int heapIndex;

    public TileNode(Vector3Int _localPosition, Vector3 _worldPosition, TileBase _tileBase, Tilemap _tilemapMember, string _name, bool _walkable, int _aGridX, int _aGridY, int _penalty)
    {
        localPosition = _localPosition;
        worldPosition = _worldPosition + new Vector3(0.5f, 0.5f, 0);
        tileBase = _tileBase;
        tilemapMember = _tilemapMember;
        name = _name;
        walkable = _walkable;
        aGridX = _aGridX;
        aGridY = _aGridY;
        movementPenalty = _penalty;
    }

    // total cost to enter the node when using A*: is always gCost + hCost
    public int fCost
    {
        get
        {
            return gCost + hCost;
        }
    }

    // what is the node's index in the binary heap tree - implemented for the IHeapItem interface
    public int HeapIndex { get => heapIndex; set => heapIndex = value; }

    // compares two nodes when using A*: fCost first, if they are equal, compare hCost - implemented for the IComparable interface
    public int CompareTo(TileNode nodeToCompare)
    {
        int compare = fCost.CompareTo(nodeToCompare.fCost); // compare fCost
        if (compare == 0)
            compare = hCost.CompareTo(nodeToCompare.hCost); // compare hCost if fCost is equal

        // lower priority = -1, same priority = 0, higher priority = 1
        // lower priority is if the value is higher and higher priority is if the value is lower.
        // since we compared integers, it returns 1 if the value is higher, so we need to negate compare for the correct priority
        return -compare;
    }
}
